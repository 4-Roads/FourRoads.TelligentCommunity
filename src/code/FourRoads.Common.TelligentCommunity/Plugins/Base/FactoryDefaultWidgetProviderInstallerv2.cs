using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.ScriptedContentFragments.Services;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public abstract class FactoryDefaultWidgetProviderInstallerv2 : IScriptedContentFragmentFactoryDefaultProvider, IInstallablePlugin, IConfigurablePlugin
    {
        private bool _enableFilewatcher = false;
        private bool _installOnNextLoad = false;
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
                Install(Version);

                if (_configuration != null)
                {
                    _configuration.SetBool("installNextLoad", false);
                    _configuration.Commit();
                }
            }
        }

        #endregion

        #region IInstallablePlugin Members

        public virtual void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version || IsDebugBuild)
            {
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
                        var widgetXml = EmbeddedResources.GetString(resourceName);

                        if (!GetInstanceIdFromWidgetXml(widgetXml, out instanceId))
                            return;

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

                            using (var stream = EmbeddedResources.GetStream(supplementPath))
                            {
                                FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(this, instanceId, supplementName, stream);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        new TCException($"Couldn't load widget from '{resourceName}' embedded resource.", exception).Log();
                    }
                });
            }
        }

        private bool GetInstanceIdFromWidgetXml(string widhgetXml, out Guid instanceId)
        {
            instanceId = Guid.Empty;
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
                _installOnNextLoad = configuration.GetBool("installNextLoad");
            }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                if (IsDebugBuild)
                {
                    PropertyGroup propertyGroup = new PropertyGroup("options", "Options", 0);

                    propertyGroup.Properties.Add(new Property("installNextLoad", "Install on next load", PropertyType.Bool, 0, bool.TrueString));

                    propertyGroup.Properties.Add(new Property("filewatcher", "Resource Watcher for Development", PropertyType.Bool, 0, bool.TrueString));

                    return new[] { propertyGroup };
                }

                return new PropertyGroup[0];
            }
        }

        private static void ClearCacheNotApiSafe()
        {
            Telligent.Common.Services.Get<IFactoryDefaultScriptedContentFragmentService>().ExpireCache();
            Telligent.Common.Services.Get<IScriptedContentFragmentService>().ExpireCache();
            Telligent.Common.Services.Get<IContentFragmentPageService>().RemoveAllFromCache();
            Telligent.Common.Services.Get<IContentFragmentService>().RefreshContentFragments();
            Telligent.Evolution.Components.ThemeFiles.RequestHostVersionedThemeFileRegeneration();
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

        public Stream FileAsStream(string path)
        {
            // Avoiding locking
            var bytes = File.ReadAllBytes(path);
            var stream = new MemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Builds a single widget held in the given file path.
        /// </summary>
        private void BuildWidget(string pathToWidget)
        {

            // WaitForFile(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            var widgetXml = File.ReadAllText(pathToWidget + "/widget.xml");

            // Get the widget ID:
            Guid instanceId;

            if (!GetInstanceIdFromWidgetXml(widgetXml, out instanceId))
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
                    FileAsStream(supFile)
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

        private void InitializeFilewatcher([CallerFilePath] string path = "")
        {
            _fileSystemWatcher?.Dispose();

            if (!string.IsNullOrWhiteSpace(path))
            {
                path = Path.GetDirectoryName(path);

                //TODO: Assumption is that this class is in the root of the project for now
                path = Path.Combine(path, "Resources");
                path = Path.Combine(path, "Widgets");

                if (Directory.Exists(path))
                {
                    _fileSystemWatcher = new FileSystemWatcher();
                    _fileSystemWatcher.Path = path;
                    _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    _fileSystemWatcher.Filter = "*.*";
                    _fileSystemWatcher.IncludeSubdirectories = true;
                    _fileSystemWatcher.EnableRaisingEvents = true;

                    _fileSystemWatcher.Changed += new FileSystemEventHandler(OnChanged);
                    _fileSystemWatcher.Created += new FileSystemEventHandler(OnChanged);
                    _fileSystemWatcher.Deleted += new FileSystemEventHandler(OnChanged);
                }
            }

        }
    }
}