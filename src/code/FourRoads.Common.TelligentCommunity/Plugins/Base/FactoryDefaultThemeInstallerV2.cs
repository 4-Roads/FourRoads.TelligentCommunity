using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Telligent.Common;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Components.Jobs;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.ScriptedContentFragments.Services;
using Telligent.Jobs;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;
using Theme = Telligent.Evolution.Extensibility.UI.Version1.Theme;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    /// <summary>
    /// - How this works-
    /// Theme source files are located in Themes/{ThemeName}/
    /// When a theme is built by editing those files, it'll appear in Resources/Themes/{ThemeName}-{ThemeSegment}-Theme.xml
    /// The build+update process takes around 1 minute, so *it's important that you simply wait for it to happen*.
    /// Clearing the caches etc during that time slows it down considerably.
    /// Included files can have configuration - this configuration goes in the new theme meta file, Themes/{ThemeName}/theme.json
    /// This is so it doesn't depend on the output XML file at all - i.e. the XML files can be fully "built" from the source files.
    /// Previously that caused two problems - sometimes the XML file got corrupted breaking the ability to update it.
    /// Secondly file ordering wasn't preserved and file config could dissappear resulting in subtle bits of broken style.
    /// </summary>

    public abstract class FactoryDefaultThemeInstallerV2 : IInstallablePlugin, IConfigurablePlugin, IEvolutionJob
    {
        #region IPlugin Members

        private bool _enableFilewatcher = false;
        private FileSystemWatcher _fileSystemWatcher;
        private IPluginConfiguration _configuration;
        private static readonly object _updateLocker = new object();
        private static readonly object _pageLocker = new object();

        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }
        protected abstract ICallerPathVistor CallerPath();

        public string Name => ProjectName + " - Theme";

        public string Description => "Installs the default theme for " + ProjectName + ".";

        public void Initialize()
        {
            if (IsDebugBuild && _enableFilewatcher)
            {
                InitializeFilewatcher();
            }
        }

        #endregion

        private void EnumerateResourceFolder(string basePath, string extension, Action<string> handleResource)
        {
            basePath = BaseResourcePath + basePath;

            EmbeddedResources.EnumerateReosurces(
                basePath,
                extension,
                resourceName =>
                {
                    Apis.Get<IUsers>()
                        .RunAsUser(
                            Apis.Get<IUsers>().ServiceUserName,
                            () =>
                            {
                                handleResource(resourceName);
                            });
                });
        }

        /// <summary>
        /// Set this to false to prevent the installer from installing when version numbers are lower
        /// </summary>
        protected virtual bool SupportAutoInstall => true;

        #region IInstallablePlugin Members

        public virtual void Install(Version lastInstalledVersion)
        {
            if (SupportAutoInstall)
            {
                if (lastInstalledVersion < Version)
                {
                    ScheduleInstall();
                }
            }
        }

        public void Execute(JobData jobData)
        {
            InstallNow();
        }

        protected void ScheduleInstall()
        {
            JobInfo[] jobs = null;
            try
            {
                var runAllJobsLocally = ConfigurationManager.AppSettings["RunJobsInternally"] != null && ConfigurationManager.AppSettings["RunJobsInternally"].ToLower() == "true";

                PeekFilter filter = new PeekFilter()
                {
                    JobNameTypeFilter = GetType().FullName,
                    SortBy = JobSortBy.StateAndName,
                    SortOrder = Telligent.Jobs.SortOrder.Ascending
                };

                if (runAllJobsLocally)
                {
                    Services.Get<IJobCoreService>().LocalJobStore.Peek(out jobs, 0, 500, filter);
                }
                else
                {
                    Services.Get<IJobCoreService>().RemoteJobStore.Peek(out jobs, 0, 500, filter);
                }
            }
            catch (Exception)
            {
                // failed to determine if we have an existing job queued
            }

            if (jobs == null || jobs.Count(j => j.State == JobState.Ready) == 0)
            {
                Apis.Get<IJobService>().Schedule(GetType(), DateTime.UtcNow.AddSeconds(15));
            }
        }

        public void InstallNow()
        {
            Uninstall();

            #region Install custom theme

            EnumerateResourceFolder(
                "Themes.",
                ".xml",
                resourceName =>
                {
                    XmlDocument xmlDocument = new XmlDocument();

                    try
                    {
                        xmlDocument.LoadXml(EmbeddedResources.GetString(resourceName));

                        UpdateTheme(xmlDocument);
                    }
                    catch (Exception exception)
                    {
                        new TCException(
                            string.Format("Couldn't load theme from '{0}' embedded resource.", resourceName),
                            exception).Log();
                    }

                });

            #endregion

            #region Install custom pages into theme (and revert any configured defaults or contextual versions of these pages)

            EnumerateResourceFolder(
                "Pages.",
                ".xml",
                resourceName =>
                {
                    XmlDocument xml = new XmlDocument();

                    try
                    {
                        xml.LoadXml(EmbeddedResources.GetString(resourceName));

                        UpdatePageLayouts(xml);
                    }
                    catch (Exception exception)
                    {
                        new TCException(string.Format("Couldn't load page from '{0}' embedded resource.", resourceName), exception).Log();
                    }

                }
            );

            #endregion

        }

        private static void UpdateTheme(XmlDocument xmlDocument)
        {
            lock (_updateLocker)
            {

                XmlNode node = xmlDocument.SelectSingleNode("/theme/themeImplementation");

                if (node != null)
                {
                    Telligent.Evolution.Components.Theme theme = ThemeConfigurations.DeserializeTheme(node, true, false);

                    Guid name = new Guid(xmlDocument.SelectSingleNode("/theme/@name").Value);
                    Guid themeTypeid = new Guid(xmlDocument.SelectSingleNode("/theme/themeImplementation/themeInformation/@themeTypeId").Value);

                    ThemeConfigurationData data = new ThemeConfigurationData(themeTypeid, CSContext.Current.SiteThemeData.ThemeContextID, name.ToString("N"));

                    ThemeConfigurationDataImporter importer = new ThemeConfigurationDataImporter(data, CSContext.Current);

                    importer.Import(node.SelectSingleNode("themeInformation/factoryDefaultConfiguration"));

                    ThemeConfigurations.SaveFactoryDefaults(theme, data);

                    Telligent.Evolution.Components.ThemeConfigurationDatas.Update(data);

                    ClearCacheNotApiSafe(theme.ThemeID, theme.ThemeTypeID);
                }
            }
        }

        private static void ClearCacheNotApiSafe(Guid id, Guid ThemeTypeId)
        {
            Telligent.Common.Services.Get<IFactoryDefaultScriptedContentFragmentService>().ExpireCache();
            Telligent.Common.Services.Get<IScriptedContentFragmentService>().ExpireCache();
            Telligent.Common.Services.Get<IContentFragmentPageService>().RemoveAllFromCache();
            Telligent.Common.Services.Get<IContentFragmentService>().RefreshContentFragments();


            // in theory this should refresh the cache >= 10.2.2.4296
            //var theme = Themes.List(ThemeTypeId).FirstOrDefault(t => t.Id == id);
            //if (theme != null)
            //{
            //    byte[] byteArray = Encoding.ASCII.GetBytes(".dummy.4roads {padding: 21px;}");
            //    MemoryStream stream = new MemoryStream(byteArray);

            //    // add an remove a file to trigger a cache clear and reload
            //    Telligent.Evolution.Extensibility.UI.Version1.ThemeFiles.AddUpdateFactoryDefault(theme, ThemeProperties.StyleSheetFiles, "Test.less", stream, (int)stream.Length , new CssThemeFileOptions() { ApplyToModals = true, ApplyToNonModals = true });
            //    Telligent.Evolution.Extensibility.UI.Version1.ThemeFiles.RemoveFactoryDefault(theme, ThemeProperties.StyleSheetFiles, "Test.less");
            //}

            // the calls below are not reliable when called during init phase
            //Telligent.Evolution.Components.ThemeFiles.RequestHostVersionedThemeFileRegeneration();
            //SystemFileStore.RequestHostVersionedThemeFileRegeneration();
        }

        private static void InteractiveClearCacheNotApiSafe()
        {
            // the calls below are not reliable when called during init phase
            Telligent.Evolution.Components.ThemeFiles.RequestHostVersionedThemeFileRegeneration();
            SystemFileStore.RequestHostVersionedThemeFileRegeneration();
        }


        public void Uninstall()
        {

        }

        private void UpdatePageLayouts(XmlDocument xmlDocument)
        {
            // Lots of database spam ensues here, taking around 30 seconds to complete
            // (stacks with other database traffic from the .Import call above).
            // Could only run this when files in "PageLayouts" change to make big 50%+ time saves.

            var pages = xmlDocument.SelectSingleNode("/theme/themeImplementation/themeInformation/factoryDefaultPageLayouts/defaultFragmentPages");
            var headers = xmlDocument.SelectSingleNode("/theme/themeImplementation/themeInformation/factoryDefaultPageLayouts/defaultHeaders");
            var footers = xmlDocument.SelectSingleNode("/theme/themeImplementation/themeInformation/factoryDefaultPageLayouts/defaultFooters");

            Guid themeTypeid = new Guid(xmlDocument.SelectSingleNode("/theme/themeImplementation/themeInformation/@themeTypeId").Value);
            Guid name = new Guid(xmlDocument.SelectSingleNode("/theme/@name").Value);

            foreach (Theme theme in Themes.List(themeTypeid))
            {
                if (theme != null)
                {
                    if (theme.Id == name)
                    {
                        foreach (XmlElement page in pages)
                        {
                            ThemePages.AddUpdateFactoryDefault(theme, page);
                            ThemePages.DeleteDefault(theme, page.Name, true);
                            ThemePages.Delete(theme, page.Name, true);
                        }

                        foreach (XmlElement header in headers)
                        {
                            ThemeHeaders.AddUpdateFactoryDefault(theme, header);
                            ThemePages.DeleteDefault(theme, header.Name, true);
                            ThemePages.Delete(theme, header.Name, true);
                        }

                        foreach (XmlElement footer in footers)
                        {
                            ThemeFooters.AddUpdateFactoryDefault(theme, footer);
                            ThemePages.DeleteDefault(theme, footer.Name, true);
                            ThemePages.Delete(theme, footer.Name, true);
                        }
                    }
                }
            }
        }
        private bool IsDebugBuild => Diagnostics.IsDebug(GetType().Assembly);

        public Version Version => GetType().Assembly.GetName().Version;

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
            if (IsDebugBuild)
            {
                _enableFilewatcher = configuration.GetBool("filewatcher");
            }
        }

        internal class InstallButtonPropertyControl : PluginButtonPropertyControl
        {
            protected override void OnClick()
            {
                FactoryDefaultThemeInstallerV2 plugin = (PluginManager.Get<IInstallablePlugin>().First(o => o.GetType().AssemblyQualifiedName == CallingType)) as FactoryDefaultThemeInstallerV2;

                plugin?.ScheduleInstall();
            }

            public override string Text => "Install Theme";

            public string CallingType => this.ConfigurationProperty.Attributes["CallingType"];
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup propertyGroup = new PropertyGroup("options", "Options", 0);

                Property button = new Property("installNextLoad", "Install (via job)", PropertyType.Custom, 0, "");
                button.ControlType = typeof(InstallButtonPropertyControl);
                button.Attributes.Add("Text", "Install");
                button.Attributes.Add("CallingType", GetType().AssemblyQualifiedName);
                propertyGroup.Properties.Add(button);

                if (IsDebugBuild)
                {
                    propertyGroup.Properties.Add(new Property("filewatcher", "Resource Watcher for Development", PropertyType.Bool, 0, bool.TrueString));
                }

                return new[] { propertyGroup };
            }
        }

        public Newtonsoft.Json.Linq.JObject LoadThemeMeta(string themePath)
        {
            var metaFilePath = Path.Combine(themePath, "theme.json");

            if (!File.Exists(metaFilePath))
            {
                return null;
            }

            return JsonConvert.DeserializeObject(File.ReadAllText(metaFilePath)) as Newtonsoft.Json.Linq.JObject;
        }

        /// <summary>Telligents type IDs for the various theme segments.</summary>
        private Dictionary<string, Guid> themeSegmentIds = new Dictionary<string, Guid>(){
            {"Group", ThemeTypes.Group},
            {"Blog", ThemeTypes.Weblog},
            {"Site", ThemeTypes.Site},
            {"User", ThemeTypes.User}
        };

        private void InitializeFilewatcher()
        {
            _fileSystemWatcher?.Dispose();
            string path = CallerPath().GetPath();

            //Rebase the path
            string assemblyName = GetType().Assembly.GetName().Name;

            path = path.Substring(0, path.IndexOf(assemblyName, StringComparison.Ordinal) + assemblyName.Length + 1);

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            path = Path.GetDirectoryName(path);
            string resourcePath = path;

            //RTODO: Assumption is that this class is in the root of the project for now
            path = Path.Combine(path, "Themes");

            //string destinationPath = Path.GetFullPath(Path.Combine(AssemblyDirectory, "..\\filestorage"));

            // Built XML output goes in the Resources folder:
            resourcePath = Path.Combine(resourcePath, "Resources");
            resourcePath = Path.Combine(resourcePath, "Themes");

            if (!Directory.Exists(path))
            {
                return;
            }

            _fileSystemWatcher = new FileSystemWatcher();
            _fileSystemWatcher.Path = path;
            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _fileSystemWatcher.Filter = "*.*";
            _fileSystemWatcher.IncludeSubdirectories = true;
            _fileSystemWatcher.EnableRaisingEvents = true;

            _fileSystemWatcher.Changed += (sender, e) => OnChanged(sender, e, path, resourcePath);
        }

        internal void OnChanged(object source, FileSystemEventArgs e, string path, string resourcePath)
        {
            // Needs to run as the service user otherwise it runs as 
            // any random online user and can then get "file upload" errors..
            var _userService = Apis.Get<IUsers>();

            _userService.RunAsUser(_userService.ServiceUserName, () =>
            {

                try
                {
                    lock (_pageLocker)
                    {
                        // Get the relative path:
                        var themeRelativePath = e.FullPath.Substring(path.Length + 1);

                        // First directory is the theme.
                        var themePathParts = themeRelativePath.Replace("\\", "/").Split('/');

                        // Theme root is..
                        var themeDirName = themePathParts[0];
                        var themePath = Path.Combine(path, themeDirName);

                        // So therefore we can get the config file for the theme (theme.json):
                        var themeMetadata = LoadThemeMeta(themePath);

                        // For each theme segment..
                        foreach (var segment in themeSegmentIds)
                        {
                            // Page layouts are in..
                            var pageLayoutFile = Path.Combine(themePath, "PageLayouts", segment.Key + ".xml");

                            // The target XML file is..
                            var targetXmlFile = Path.Combine(resourcePath, themeDirName + "-" + segment.Key + "-Theme.xml");

                            if (!File.Exists(targetXmlFile))
                            {
                                if (segment.Key == "Site")
                                {
                                    throw new Exception("Theme file missing - needed " + targetXmlFile + " to exist to write into it.");
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            // Load up the target XML doc (using .LoadXml to avoid locking the file):
                            XmlDocument xmlDocument = new XmlDocument();
                            xmlDocument.LoadXml(BytesToText(ReadFileBytes(targetXmlFile)));

                            // Delete widget node. Widgets are installed separately and widgets in the theme can both massively slow down the cycle
                            // as well as greatly impact its stability.
                            var widgets = xmlDocument.SelectSingleNode("/theme/themeImplementation/themeInformation/factoryDefaultPageLayouts/defaultContentFragments");

                            if (widgets != null)
                            {
                                widgets.ParentNode.RemoveChild(widgets);
                            }

                            // Load the page layouts:
                            XmlDocument pageDocument = new XmlDocument();
                            pageDocument.LoadXml(BytesToText(ReadFileBytes(pageLayoutFile)));

                            // Push those page layouts into the theme:
                            ProcessPageLayoutNodes(xmlDocument, pageDocument, "defaultHeaders");
                            ProcessPageLayoutNodes(xmlDocument, pageDocument, "defaultFooters");
                            ProcessPageLayoutNodes(xmlDocument, pageDocument, "defaultFragmentPages");

                            // If it's the site segment, we should also update the files too:
                            if (segment.Value == ThemeTypes.Site)
                            {
                                var customFileConfigs = themeMetadata.GetValue("fileConfigurations") as JObject;

                                // Rebuild all the files for the stylesheets etc.
                                UpdateFileDataNodes(
                                    xmlDocument,
                                    themePath,
                                    customFileConfigs,
                                    "StyleSheetFiles",
                                    new string[] {
                                        "screen.less",
                                        "modal.less",
                                        "print.css",
                                        "oauth.css",
                                        "handheld.less",
                                        "tablet.less",
                                        "modalhandheld.less"
                                    }
                                );
                                UpdateFileDataNodes(xmlDocument, themePath, customFileConfigs, "JsFiles");
                                UpdateFileDataNodes(xmlDocument, themePath, customFileConfigs, "Files");
                            }

                            // Save the theme file and update the running instance:
                            try
                            {
                                if (e.FullPath.Contains("PageLayouts"))
                                {
                                    UpdatePageLayouts(xmlDocument);
                                }
                            }
                            catch (Exception pe)
                            {
                                Apis.Get<IEventLog>().Write(pe.ToString(), new EventLogEntryWriteOptions() { });
                            }

                            UpdateTheme(xmlDocument);
                            WriteXmlFile(targetXmlFile, xmlDocument);
                        }

                        InteractiveClearCacheNotApiSafe();
                    }
                }
                catch (Exception pe)
                {
                    Apis.Get<IEventLog>().Write(pe.ToString(), new EventLogEntryWriteOptions() { });
                }
            });
        }

        private byte[] ReadFileBytes(string path)
        {
            byte[] result = null;

            while (result == null)
            {
                try
                {
                    result = File.ReadAllBytes(path);
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }

            return result;
        }

        private string BytesToText(byte[] data)
        {
            // UTF8 file without BOM
            return Encoding.UTF8.GetString(data).Trim('\uFEFF', '\u200B'); ;
        }

        private void WriteXmlFile(string path, XmlDocument xmlDocument)
        {
            StringWriter stringWriter = new StringWriter();

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            var xmlTextWriter = XmlWriter.Create(stringWriter, settings);
            xmlDocument.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            var data = stringWriter.ToString();

            while (true)
            {
                try
                {
                    File.WriteAllText(path, data);
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }

        private string LowerCaseFirstLetter(string text)
        {
            return Char.ToLowerInvariant(text[0]) + text.Substring(1);
        }

        /// <summary>
        /// Updates the files node in the given xml document for the given category of files.
        /// </summary>
        /// <param name="xmlDocument">The parsed theme XML document</param>
        /// <param name="themePath">The theme's directory.</param>
        /// <param name="name">The theme ID</param>
        /// <param name="folder">The category of files, e.g. StyleSheetFiles</param>
        /// <param name="preserveOrder">An optional list of file names. 
        /// These files will always appear at the start of the files set in the exact provided order. 
        /// This is particularly important for the stylesheetFiles where order matters.</param>
        private void UpdateFileDataNodes(XmlDocument xmlDocument, string themePath, JObject customFileConfigs, string folder, string[] preserveOrder = null)
        {
            var camelCaseFolderName = LowerCaseFirstLetter(folder);

            var parentFilesNode = xmlDocument.SelectSingleNode($"/theme/themeImplementation/themeInformation/factoryDefaultConfiguration/properties/property[@id='{camelCaseFolderName}']");

            if (parentFilesNode == null)
            {
                return;
            }

            JObject fileConfigsForNode = null;

            if (customFileConfigs != null)
            {
                fileConfigsForNode = customFileConfigs.GetValue(camelCaseFolderName) as JObject;
            }

            // The files are located in..
            string folderPath = Path.Combine(themePath, folder);

            // Get the list of files and sort alphabetically:
            var listOfFiles = Directory.GetFiles(folderPath);

            for (var i = 0; i < listOfFiles.Length; i++)
            {
                listOfFiles[i] = Path.GetFileName(listOfFiles[i]);
            }

            Array.Sort(listOfFiles);

            var results = preserveOrder == null ? listOfFiles.ToList() : preserveOrder.ToList();

            if (preserveOrder != null)
            {
                foreach (var fileName in listOfFiles)
                {
                    // Check if this file is in the set of files to preserve:
                    // - The list of files to preserve is short, so a linear scan is faster than a dictionary here
                    var preserveThisFile = false;

                    foreach (var preservedFile in preserveOrder)
                    {
                        if (fileName == preservedFile)
                        {
                            // Yep - preserve the order of this file; it's already in the results set.
                            preserveThisFile = true;
                            break;
                        }
                    }

                    if (!preserveThisFile)
                    {
                        // Not in the results set yet - add it now:
                        results.Add(fileName);
                    }
                }
            }

            parentFilesNode.RemoveChild(parentFilesNode.SelectSingleNode("files"));
            var filesNode = xmlDocument.CreateElement("files");

            foreach (var fileName in results)
            {
                // Create the <file> node and append it to the <files> node:
                var fileNode = xmlDocument.CreateElement("file");
                filesNode.AppendChild(fileNode);

                // Setup its name and config attributes:
                fileNode.SetAttribute("name", fileName);

                if (fileConfigsForNode != null)
                {
                    var fileConfig = fileConfigsForNode.GetValue(fileName);

                    if (fileConfig != null)
                    {
                        fileNode.SetAttribute("configuration", fileConfig.ToString());
                    }
                }

                // Create the <content> node and append it to the <file> node:
                var contentNode = xmlDocument.CreateElement("content");
                fileNode.AppendChild(contentNode);

                // Set the innerText (will typically come out as CDATA):
                var cdataChild = xmlDocument.CreateCDataSection(GetBase64FileData(Path.Combine(folderPath, fileName)));
                contentNode.AppendChild(cdataChild);
            }

            parentFilesNode.AppendChild(filesNode);
        }

        private string GetBase64FileData(string filePath)
        {
            string updatedb64Data;
            using (var sourceFile = File.OpenRead(filePath))
            {
                byte[] sourceData = new byte[sourceFile.Length];

                sourceFile.Read(sourceData, 0, (int)sourceFile.Length);

                updatedb64Data = Convert.ToBase64String(sourceData);
            }
            return updatedb64Data;
        }

        private static void ProcessPageLayoutNodes(XmlDocument xmlDocument, XmlDocument pageDocument, string section)
        {
            var importFromNode = pageDocument.SelectSingleNode($"/theme/{section}");

            if (importFromNode != null)
            {
                var sectionNode = xmlDocument.SelectSingleNode($"/theme/themeImplementation/themeInformation/factoryDefaultPageLayouts/{section}");

                if (sectionNode == null)
                {
                    //Add the node
                    var importedNode = xmlDocument.ImportNode(importFromNode, true);
                    xmlDocument.SelectSingleNode("/theme/themeImplementation/themeInformation/factoryDefaultPageLayouts").AppendChild(importedNode);
                }
                else
                {
                    sectionNode.InnerXml = importFromNode.InnerXml;
                }
            }
        }
        #endregion
    }
}
