using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Xml;
using Telligent.Evolution.Components;
using Telligent.Evolution.Configuration;
using Telligent.Evolution.Extensibility.Storage.Providers.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;

namespace FourRoads.TelligentCommunity.ThemeHelper.Plugins
{
    public class FileSystemUtility: ISingletonPlugin
    {
        #region Fields

        private static readonly Dictionary<string, Action> _pathActions = new Dictionary<string, Action>();
        private static readonly Dictionary<string, FileSystemWatcher> _pathWatchers =
            new Dictionary<string, FileSystemWatcher>();

        private string _siteThemeName;

        #endregion

        #region Internal classes

        private static class WatcherEvent
        {
            private static DateTime Time { get; set; }
            private static WatcherChangeTypes ChangeType { get; set; }
            private static string Path { get; set; }

            static WatcherEvent()
            {
                Time = DateTime.MinValue;
            }

            public static bool Matches(DateTime dateTime)
            {
                return ((dateTime - Time).TotalSeconds < 1.0);
            }

            public static bool Matches(FileSystemEventArgs e)
            {
                return e.FullPath.Equals(Path) && e.ChangeType.Equals(ChangeType);
            }

            public static void Update(FileSystemEventArgs args, DateTime dateTime)
            {
                Path = args.FullPath;
                ChangeType = args.ChangeType;
                Time = dateTime;
            }
        }

        #endregion

        #region IPlugin Members

        public string Description
        {
            get
            {
                return
                    "Monitors local CFS folders and triggers theme reversion for immediate visibility of changes during development";
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Initialize()
        {
            ThemeUtility themeUtility = PluginManager.GetSingleton<ThemeUtility>();
            IThemeUtilities utilites = PluginManager.GetSingleton<IThemeUtilities>();

            if (themeUtility == null || utilites == null || !utilites.EnableFileSystemWatcher)
                return;

            _siteThemeName = CSContext.Current.SiteTheme;

            InitialiseFileSystemWatcher(Constants.DefaultWidgets, themeUtility.ResetCache);
            InitialiseFileSystemWatcher(Constants.FactoryDefaultConfigurations,
                () => themeUtility.RevertTheme(ReversionType.Configuration), _siteThemeName);
            InitialiseFileSystemWatcher(Constants.FactoryDefaultPages,
                () => themeUtility.RevertTheme(ReversionType.Layouts), _siteThemeName);
            InitialiseFileSystemWatcher(Constants.ThemeFiles, () => themeUtility.RevertTheme(ReversionType.Files),
                string.Format(@"s\fd\{0}", _siteThemeName));
        }

        public string Name
        {
            get { return "4 Roads - Filesystem utility"; }
        }

        #endregion

        #region File system monitoring methods

        /// <summary>
        /// Initialises a new file system watcher.
        /// </summary>
        /// <param name="fileStoreKey">The CFS file store key.</param>
        /// <param name="changeAction">The change action.</param>
        /// <param name="path">The file store sub-path.</param>
        void InitialiseFileSystemWatcher(string fileStoreKey, Action changeAction, string path = null)
        {
            ICentralizedFileStorageProvider fileStore = CentralizedFileStorage.GetFileStore(fileStoreKey);

            if (fileStore is FileSystemFileStorageProvider)
            {
                string basePath = GetBasePath(fileStoreKey);
                string fileStorePath = Path.Combine(Globals.CalculateFileSystemStorageLocation(basePath), fileStoreKey);

                if (!String.IsNullOrEmpty(path))
                {
                    fileStorePath = Path.Combine(fileStorePath, path);
                }

                if (!String.IsNullOrEmpty(fileStorePath) && Directory.Exists(fileStorePath))
                {
                    // Create a new EnableFileSystemWatcher and set it to watch for changes in LastAccess and LastWrite times.
                    FileSystemWatcher watcher = new FileSystemWatcher
                    {
                        IncludeSubdirectories = true,
                        Path = fileStorePath,
                        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                    };

                    // Add event handlers.
                    watcher.Changed += OnChanged;
                    watcher.EnableRaisingEvents = true;

                    _pathWatchers.Add(fileStoreKey, watcher);
                    _pathActions.Add(fileStorePath, changeAction);
                }
            }
        }

        #endregion

        #region Event handlers

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = source as FileSystemWatcher;
            DateTime writeTime = File.GetLastWriteTime(e.FullPath);

            // Avoid duplicate events
            if (watcher == null || (WatcherEvent.Matches(writeTime) && WatcherEvent.Matches(e)))
                return;

            // Execute the appropriate action
            if (_pathActions.ContainsKey(watcher.Path))
                _pathActions[watcher.Path]();

            WatcherEvent.Update(e, writeTime);
        }

        #endregion

        private string GetBasePath(string fileStoreKey)
        {
            XmlNode configSection = CSConfiguration.GetConfig().GetConfigSection("CommunityServer/CentralizedFileStorage");
            string basePath = null;

            if (configSection != null)
            {
                foreach (XmlNode xmlNode in configSection.ChildNodes.Cast<XmlNode>().Where(xmlNode => xmlNode.NodeType != XmlNodeType.Comment))
                {
                    if (xmlNode.Name == "fileStoreGroup")
                    {
                        if (xmlNode.Attributes != null &&
                            xmlNode.Attributes["default"] != null &&
                            xmlNode.Attributes["default"].Value == "true" &&
                            xmlNode.Attributes["basePath"] != null &&
                            !String.IsNullOrEmpty(xmlNode.Attributes["basePath"].Value) &&
                            basePath == null)
                        {
                            basePath = xmlNode.Attributes["basePath"].Value;
                        }

                        foreach (XmlNode xmlNode2 in xmlNode.ChildNodes)
                        {
                            if (xmlNode2.Name == "fileStore" &&
                                xmlNode2.Attributes != null &&
                                xmlNode2.Attributes["name"] != null &&
                                xmlNode2.Attributes["name"].Value == fileStoreKey &&
                                xmlNode2.Attributes["basePath"] != null &&
                                !String.IsNullOrEmpty(xmlNode2.Attributes["basePath"].Value))
                            {
                                basePath = xmlNode2.Attributes["basePath"].Value;
                                break;
                            }
                        }

                        continue;
                    }

                    if (xmlNode.Name == "fileStore" &&
                        xmlNode.Attributes != null &&
                        xmlNode.Attributes["name"] != null &&
                        xmlNode.Attributes["name"].Value == fileStoreKey &&
                        xmlNode.Attributes["basePath"] != null &&
                        !String.IsNullOrEmpty(xmlNode.Attributes["basePath"].Value))
                    {
                        basePath = xmlNode.Attributes["basePath"].Value;
                        break;
                    }
                }
            }

            return basePath;
        }
    }
}