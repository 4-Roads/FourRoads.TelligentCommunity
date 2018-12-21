using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using Telligent.Common;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.ScriptedContentFragments.Services;
using ThemeFiles = Telligent.Evolution.Components.ThemeFiles;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public abstract class FactoryDefaultWidgetProviderInstallerV2 : IScriptedContentFragmentFactoryDefaultProvider, IInstallablePlugin, IConfigurablePlugin
    {
        private bool _enableFilewatcher;
        private bool _installOnNextLoad;
        private IPluginConfiguration _configuration;
        private FileSystemWatcher _fileSystemWatcher;
        private static object _resourceLock = new object();

        public abstract Guid ScriptedContentFragmentFactoryDefaultIdentifier { get; }
        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }

        #region IPlugin Members

        public string Name => ProjectName + " - Widgets";

        public string Description => "Defines the default widget set for " + ProjectName + ".";

        public void Initialize()
        {
            if (_enableFilewatcher)
            {
                InitializeFilewatcher();
            }

            if (_installOnNextLoad)
            {
                InstallNow();

                if (_configuration != null)
                {
                    _configuration.SetBool("installNextLoad", false);
                    _configuration.Commit();
                }
            }

            ThemeVersionHelper.LocalVersionCheck($"widgets-{ProjectName}", Version, Install);
        }

        #endregion

        #region IInstallablePlugin Members

        public virtual void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version)
            {
                InstallNow();
            }
        }
        
        public void InstallNow(){
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
                        this,
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
                            this,
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
                    FactoryDefaultScriptedContentFragmentProviderFiles.DeleteAllFiles(this);
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

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
            if (IsDebugBuild)
            {
                _enableFilewatcher = configuration.GetBool("filewatcher");
            }
            _installOnNextLoad = configuration.GetBool("installNextLoad");
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup propertyGroup = new PropertyGroup("options", "Options", 0);

                propertyGroup.Properties.Add(new Property("installNextLoad", "Install on next load", PropertyType.Bool, 0, bool.TrueString));

                if (IsDebugBuild)
                {
                    propertyGroup.Properties.Add(new Property("filewatcher", "Resource Watcher for Development", PropertyType.Bool, 0, bool.TrueString));
                }
                return new[] { propertyGroup };
            }
        }

        private static void ClearCacheNotApiSafe()
        {
            Services.Get<IFactoryDefaultScriptedContentFragmentService>().ExpireCache();
            Services.Get<IScriptedContentFragmentService>().ExpireCache();
            Services.Get<IContentFragmentPageService>().RemoveAllFromCache();
            Services.Get<IContentFragmentService>().RefreshContentFragments();
            // the calls below are not reliable when called during init phase
            ThemeFiles.RequestHostVersionedThemeFileRegeneration();
            SystemFileStore.RequestHostVersionedThemeFileRegeneration();
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
                catch (IOException e)
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
        private void BuildWidget(string pathToWidget)
        {

            // WaitForFile(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            var widgetXml = BytesToText(ReadFileBytes(pathToWidget + "/widget.xml"));

            // Get the widget ID:
            Guid instanceId;
            string cssClass;
            Guid providerId;

            if (!GetInstanceIdFromWidgetXml(widgetXml, out instanceId, out cssClass, out providerId))
            {
                return;
            }

            // If this widget's provider ID is not the one we're installing, then ignore it:
            if (providerId != ScriptedContentFragmentFactoryDefaultIdentifier)
            {
                return;
            }

            // Update the widget's XML:
            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(
                this,
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
                    this,
                    instanceId,
                    fileName,
                    PreprocessWidgetFile(ReadFileBytes(supFile), fileName, cssClass)
                );
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

                // Clear the cache:
                ClearCacheNotApiSafe();
            }

        }

        /// <summary>
        /// Builds all widgets that belong to this installer.
        /// </summary>
        public void BuildAllWidgets(string dirPath)
        {
            // For each widget directory..
            foreach (var dir in Directory.EnumerateDirectories(dirPath))
            {
                // Build the widget:
                BuildWidget(dir);
            }
        }

        protected abstract ICallerPathVistor CallerPath();

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