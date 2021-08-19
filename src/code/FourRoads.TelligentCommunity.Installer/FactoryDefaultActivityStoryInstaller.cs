using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using FourRoads.TelligentCommunity.Installer.Components.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using File = System.IO.File;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using FourRoads.TelligentCommunity.Installer.Components.Utility;

namespace FourRoads.TelligentCommunity.Installer
{
    public abstract class FactoryDefaultActivityStoryInstaller : IHttpCallback, IInstallablePlugin, IConfigurablePlugin, IEvolutionJob
    {
        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }

        private IHttpCallbackController _callbackController;

        #region IPlugin Members

        public string Name => ProjectName + " - Activity Story Widgets";

        public string Description => "Defines the default activity story widget set for " + ProjectName + ".";

        public void Initialize()
        {
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
            Apis.Get<IJobService>().Schedule(GetType(), DateTime.UtcNow.AddSeconds(5));
        }

        protected void InstallNow()
        {

            Uninstall();

            string basePath = BaseResourcePath + "ActivityStories.";

            EmbeddedResources.EnumerateResources(basePath, "activitystory.xml", resourceName =>
            {
                try
                {
                    // Resource path to all files relating to this widget:
                    string widgetPath = resourceName.Replace(".activitystory.xml", ".");

                    Guid instanceId;
                    Guid providerId;
                    var widgetXml = EmbeddedResources.GetString(resourceName);

                    if (!GetInstanceIdFromWidgetXml(widgetXml, out instanceId, out providerId))
                        return;

                    Apis.Get<IEventLog>().Write($"Installing activity story '{resourceName}'", new EventLogEntryWriteOptions() { Category = "Installer" });

                    //no extensible installer available so need to copy it manually
                    var defaultwidgets = CentralizedFileStorage.GetFileStore("defaultwidgets");

                    defaultwidgets.AddFile(CentralizedFileStorage.MakePath(providerId.ToString("N")), $"{instanceId:N}.xml", EmbeddedResources.GetStream(resourceName), false);
                }
                catch (Exception exception)
                {
                    Apis.Get<IExceptions>().Log(new Exception($"Couldn't load activity story widget from '{resourceName}' embedded resource.", exception));
                }
            });

            Caching.ExpireUICaches();
            Caching.ExpireAllCaches();
        }

        private bool GetInstanceIdFromWidgetXml(string widgetXml, out Guid instanceId, out Guid providerId)
        {
            instanceId = Guid.Empty;
            providerId = Guid.Empty;
            // GetInstanceIdFromWidgetXml widget identifier
            XDocument xdoc = XDocument.Parse(widgetXml);
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

            return true;
        }

        public virtual void Uninstall()
        {
            if (!IsDebugBuild)
            {
                //Only in release do we want to uninstall widgets, when in development we don't want this to happen
                try
                {
                    //todo -- FactoryDefaultScriptedContentFragmentProviderFiles.DeleteAllFiles(_sourceScriptedFragment);
                }
                catch (Exception exception)
                {
                    Apis.Get<IExceptions>().Log(new Exception("Couldn't delete factory default activity story widgets from provider ID: '{ScriptedContentFragmentFactoryDefaultIdentifier}'.", exception));
                }
            }
        }
        private static bool IsDebug()
        {
            object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(DebuggableAttribute), false);
            if ((customAttributes != null) && (customAttributes.Length == 1))
            {
                DebuggableAttribute attribute = customAttributes[0] as DebuggableAttribute;
                return (attribute.IsJITOptimizerDisabled && attribute.IsJITTrackingEnabled);
            }
            return false;
        }

        private bool IsDebugBuild => IsDebug();

        public Version Version => GetType().Assembly.GetName().Version;

        #endregion

        public void Update(IPluginConfiguration configuration)
        {
            if (IsDebugBuild)
            {
                _enableFilewatcher = configuration.GetBool("filewatcher").HasValue ? configuration.GetBool("filewatcher").Value : false;
            }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup pg = new PropertyGroup() { Id = "options", LabelText = "Options" };

                var updateStatusProp = new Property()
                {
                    Id = "widgetRefresh",
                    LabelText = "Update Custom Activity Story Widgets",
                    DescriptionResourceName = "Request a background job to refresh the custom activity story widgets",
                    DataType = "custom",
                    Template = "installer_triggerAction",
                    DefaultValue = ""
                };
                if (_callbackController != null)
                {
                    updateStatusProp.Options.Add("callback", _callbackController.GetUrl());
                }
                updateStatusProp.Options.Add("resturl", "");
                updateStatusProp.Options.Add("data", "refresh:true");
                updateStatusProp.Options.Add("label", "Trigger Refresh");
                pg.Properties.Add(updateStatusProp);

                if (IsDebugBuild)
                {
                    var statusMappingProp = new Property()
                    {
                        Id = "filewatcher",
                        LabelText = "Resource Watcher for Development",
                        DescriptionText = "During development automatically refresh custom activity story widgets when they are edited",
                        DataType = "Bool",
                        Template = "bool",
                        DefaultValue = bool.TrueString
                    };
                    pg.Properties.Add(statusMappingProp);
                }

                return new[] { pg };
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

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        /// <summary>
        /// Builds a single widget held in the given file path.
        /// </summary>
        private Guid BuildWidget(string pathToWidget)
        {
            // WaitForFile(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            var widgetXml = BytesToText(ReadFileBytes(pathToWidget + "/activitystory.xml"));

            // Get the widget ID:
            Guid instanceId;
            Guid providerId;

            if (!GetInstanceIdFromWidgetXml(widgetXml, out instanceId, out providerId))
            {
                return Guid.Empty;
            }

            var defaultwidgets = CentralizedFileStorage.GetFileStore("defaultwidgets");
            defaultwidgets.AddFile(CentralizedFileStorage.MakePath(providerId.ToString("N")), $"{instanceId:N}.xml", GenerateStreamFromString(widgetXml), false);

            Caching.ExpireUICaches();
            Caching.ExpireAllCaches();

            return instanceId;
        }

        /// <summary>
        /// Builds all widgets that belong to this installer.
        /// </summary>

        protected abstract ICallerPathVistor CallerPath();

        //Becuase this is ont API safe and also relies on file paths this should never go into a release build
        private bool _enableFilewatcher;
        private FileSystemWatcher _fileSystemWatcher;


        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // 1. Which widget is this file for?
            //    Can be identified from the file called 'widget.xml' alongside the file that changed.
            var widgetPath = Path.GetDirectoryName(e.FullPath);

            if (File.Exists(widgetPath + "/activitystory.xml"))
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
                var pathToFind = "/Resources/ActivityStories";

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

        public void ProcessRequest(HttpContextBase httpContext)
        {
            ScheduleInstall();
        }

        public void SetController(IHttpCallbackController controller)
        {
            this._callbackController = controller;
        }
    }
}
