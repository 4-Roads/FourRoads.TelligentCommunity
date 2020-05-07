using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Controls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components.Jobs;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Jobs;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;
using File = System.IO.File;
using TelligentServices = Telligent.Common.Services;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{

    /// <summary>
    /// NOTE: this class no longer inherits from IScriptedContentFragmentFactoryDefaultProvider this is to allow for support of the scripted panels
    /// </summary>
    public abstract class FactoryDefaultWidgetProviderInstallerV3<TScriptedContentFragmentFactoryDefaultProvider> : IInstallablePlugin, IConfigurablePlugin, IEvolutionJob where TScriptedContentFragmentFactoryDefaultProvider: class, IScriptedContentFragmentFactoryDefaultProvider
    {
        public abstract Guid ScriptedContentFragmentFactoryDefaultIdentifier { get; }
        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }
        private TScriptedContentFragmentFactoryDefaultProvider _sourceScriptedFragment;

        #region IPlugin Members

        public string Name => ProjectName + " - Widgets";

        public string Description => "Defines the default widget set for " + ProjectName + ".";

        public void Initialize()
        {
            _sourceScriptedFragment= PluginManager.Get<TScriptedContentFragmentFactoryDefaultProvider>().FirstOrDefault();

            if (IsDebugBuild)
            {
                if (_enableFilewatcher)
                {
                    InitializeFilewatcher();
                }

                ScheduleInstall();
            }
        }

        #endregion
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
                    TelligentServices.Get<IJobCoreService>().LocalJobStore.Peek(out jobs, 0, 500, filter);
                }
                else
                {
                    TelligentServices.Get<IJobCoreService>().RemoteJobStore.Peek(out jobs, 0, 500, filter);
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

        protected void InstallNow()
        {
            if(_sourceScriptedFragment == null)
            {
                new TCException($"Couldn't load DefaultProvider plugin \"{typeof(TScriptedContentFragmentFactoryDefaultProvider).Name}\". Widgets installation aborted").Log();
            }

            Uninstall();

            string basePath = BaseResourcePath + "Widgets.";

            EmbeddedResources.EnumerateReosurces(basePath, "widget.xml", resourceName =>
            {
                try
                {
                    // Resource path to all files relating to this widget:
                    string widgetPath = resourceName.Replace(".widget.xml", ".");

                    // The widget's nice name:
                    // string widgetName = widgetPath.Substring(basePath.Length);

                    Guid instanceId;
                    string cssClass;
                    Guid providerId;
                    var widgetXml = EmbeddedResources.GetString(resourceName);

                    if (!GetInstanceIdFromWidgetXml(widgetXml, out instanceId, out cssClass, out providerId))
                        return;

                    Apis.Get<IEventLog>().Write($"Installting widget '{resourceName}'", new EventLogEntryWriteOptions() { Category = "4 Roads" });

                    // If this widget's provider ID is not the one we're installing, then ignore it:
                    if (providerId != ScriptedContentFragmentFactoryDefaultIdentifier)
                    {
                        return;
                    }

                    FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(
                        _sourceScriptedFragment,
                        instanceId.ToString("N").ToLower() + ".xml",
                        TextAsStream(widgetXml)
                    );

                    IEnumerable<string> supplementaryResources = GetType().Assembly.GetManifestResourceNames()
                                                .Where(r => r.StartsWith(widgetPath) && !r.EndsWith(".widget.xml")).ToArray();

                    if (!supplementaryResources.Any())
                        return;

                    foreach (string supplementPath in supplementaryResources)
                    {
                        string supplementName = supplementPath.Substring(widgetPath.Length);
                        var stream = EmbeddedResources.GetStream(supplementPath);
                        FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                            _sourceScriptedFragment,
                            instanceId,
                            supplementName,
                            PreprocessWidgetFile(ReadStream(stream), supplementName, cssClass)
                        );
                    }
                }
                catch (Exception exception)
                {
                    new TCException($"Couldn't load widget from '{resourceName}' embedded resource.", exception).Log();
                }
            });
        }

        private bool GetInstanceIdFromWidgetXml(string widhgetXml, out Guid instanceId, out string cssClass, out Guid providerId)
        {
            instanceId = Guid.Empty;
            providerId = Guid.Empty;
            cssClass = "";
            // GetInstanceIdFromWidgetXml widget identifier
            XDocument xdoc = XDocument.Parse(widhgetXml);
            XElement root = xdoc.Root;

            if (root == null)
                return false;

            XElement element = root.Element("scriptedContentFragment");

            if (element == null)
                return false;

            XAttribute attribute = element.Attribute("instanceIdentifier");

            if (attribute == null)
                return false;

            instanceId = new Guid(attribute.Value);

            XAttribute providerAttr = element.Attribute("provider");

            if (providerAttr == null)
                return false;

            providerId = new Guid(providerAttr.Value);

            var cssClassAttr = element.Attribute("cssClass");
            cssClass = (cssClassAttr != null) ? cssClassAttr.Value : instanceId.ToString();

            return true;
        }

        public virtual void Uninstall()
        {
            if (!IsDebugBuild)
            {
                //Only in release do we want to uninstall widgets, when in development we don't want this to happen
                try
                {
                    FactoryDefaultScriptedContentFragmentProviderFiles.DeleteAllFiles(_sourceScriptedFragment);
                }
                catch (Exception exception)
                {
                    new TCException($"Couldn't delete factory default widgets from provider ID: '{ScriptedContentFragmentFactoryDefaultIdentifier}'.", exception).Log();
                }
            }
        }

        private bool IsDebugBuild => Diagnostics.IsDebug(GetType().Assembly);

        public Version Version => GetType().Assembly.GetName().Version;

        #endregion

        internal class InstallButtonPropertyControl : PluginButtonPropertyControl
        {
            protected override void OnClick()
            {
                FactoryDefaultWidgetProviderInstallerV3<TScriptedContentFragmentFactoryDefaultProvider> plugin = (PluginManager.Get<IInstallablePlugin>().First(o => o.GetType().AssemblyQualifiedName == CallingType)) as FactoryDefaultWidgetProviderInstallerV3<TScriptedContentFragmentFactoryDefaultProvider>;

                plugin?.ScheduleInstall();
            }

            public override string Text => "Install Widgets";

            public string CallingType => this.ConfigurationProperty.Attributes["CallingType"];
        }

        public void Update(IPluginConfiguration configuration)
        {
            if (IsDebugBuild)
            {
                _enableFilewatcher = configuration.GetBool("filewatcher");
            }
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

        public Stream TextAsStream(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
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

        public static byte[] ReadStream(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private string BytesToText(byte[] data)
        {
            // UTF8 file without BOM
            return Encoding.UTF8.GetString(data).Trim('\uFEFF', '\u200B'); ;
        }

        /// <summary>
        /// Processes the file content for a particular widget file, enabling automated formatting from pretty -> Telligent.
        /// </summary>
        /// <param name="fileContent"></param>
        /// <param name="name"></param>
        /// <param name="cssClass"></param>
        /// <returns></returns>
        protected virtual Stream PreprocessWidgetFile(byte[] fileContent, string name, string cssClass)
        {
            if (name == "script.js")
            {
                fileContent = BuildJavascript(fileContent, cssClass);
            }
            return new MemoryStream(fileContent);
        }

        /// <summary>
        /// Wraps widget.js with automated scope management to keep everything consistent and tidy.
        /// </summary>
        /// <param name="jsFile">The content of widget.js</param>
        /// <param name="cssClass">The CSS class name from the widget</param>
        protected virtual byte[] BuildJavascript(byte[] jsFile, string cssClass)
        {
            var str = @"(function($){
                if (!$.customWidgets) { $.customWidgets = { }; }
                $.customWidgets['" + cssClass.Replace("-", "").Replace("_", "") + @"'] = {register : function(context, __fragId){
                    var widget = {root: $(__fragId ? '#' + __fragId : '." + cssClass + @"'), id: __fragId};
                    " + BytesToText(jsFile) + @"
                }};
            })(jQuery);";

            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// Builds a single widget held in the given file path.
        /// </summary>
        private Guid BuildWidget(string pathToWidget)
        {

            // WaitForFile(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            var widgetXml = BytesToText(ReadFileBytes(pathToWidget + "/widget.xml"));

            // Get the widget ID:
            Guid instanceId;
            string cssClass;
            Guid providerId;

            if (!GetInstanceIdFromWidgetXml(widgetXml, out instanceId, out cssClass, out providerId))
            {
                return Guid.Empty;
            }

            // If this widget's provider ID is not the one we're installing, then ignore it:
            if (providerId != ScriptedContentFragmentFactoryDefaultIdentifier)
            {
                return Guid.Empty;
            }

            // Update the widget's XML:
            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(
                _sourceScriptedFragment,
                instanceId.ToString("N").ToLower() + ".xml",
                TextAsStream(widgetXml)
            );

            // Copy in any files which are siblings of widget.xml:
            foreach (var supFile in Directory.EnumerateFiles(pathToWidget))
            {
                var fileName = Path.GetFileName(supFile);

                if (fileName == "widget.xml")
                {
                    continue;
                }

                FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                    _sourceScriptedFragment,
                    instanceId,
                    fileName,
                    PreprocessWidgetFile(ReadFileBytes(supFile), fileName, cssClass)
                );
            }

            return instanceId;
        }

        /// <summary>
        /// Builds all widgets that belong to this installer.
        /// </summary>

        protected abstract ICallerPathVistor CallerPath();

        //Becuase this is ont API safe and also relies on file paths this should never go into a release build
        private bool _enableFilewatcher;
        private FileSystemWatcher _fileSystemWatcher;

        public void BuildAllWidgets(string dirPath)
        {
            // For each widget directory..
            foreach (var dir in Directory.EnumerateDirectories(dirPath))
            {
                // Build the widget:
                BuildWidget(dir);
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // 1. Which widget is this file for?
            //    Can be identified from the file called 'widget.xml' alongside the file that changed.
            var widgetPath = Path.GetDirectoryName(e.FullPath);

            if (File.Exists(widgetPath + "/widget.xml"))
            {
                // Build just this widget.
                BuildWidget(widgetPath);
            }
        }

        private void InitializeFilewatcher()
        {
            _fileSystemWatcher?.Dispose();
            string path = CallerPath().GetPath();

            if (!string.IsNullOrWhiteSpace(path))
            {
                path = Path.GetDirectoryName(path).Replace("\\", "/");
                var directoryParts = path.Split('/').ToList();
                var pathToFind = "/Resources/Widgets";

                // Go up the directory tree and check for a nearby Resources/Widgets dir.
                for (var i = 0; i < directoryParts.Count; i++)
                {
                    var widgetsPath = string.Join("/", directoryParts) + pathToFind;

                    if (Directory.Exists(widgetsPath))
                    {
                        _fileSystemWatcher = new FileSystemWatcher();
                        _fileSystemWatcher.Path = widgetsPath;
                        _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
                        _fileSystemWatcher.Filter = "*.*";
                        _fileSystemWatcher.IncludeSubdirectories = true;
                        _fileSystemWatcher.EnableRaisingEvents = true;

                        _fileSystemWatcher.Changed += OnChanged;
                        _fileSystemWatcher.Created += OnChanged;
                        _fileSystemWatcher.Deleted += OnChanged;
                        return;
                    }

                    // Pop the last one and go around again:
                    directoryParts.RemoveAt(directoryParts.Count - 1);
                }
            }
        }
    }
}