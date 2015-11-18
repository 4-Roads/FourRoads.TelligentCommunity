using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ThemeHelper.Plugins
{
    public class SourceMapUtility: ISingletonPlugin
    {
        #region Fields

        private static readonly object _lock = new object();
        private Guid _siteThemeTypeId;
        private Guid _themeContextId;
        private string _siteThemeName;

        #endregion

        #region IPlugin Members

        public string Description
        {
            get { return "Configures source maps for compiled stylesheets"; }
        }

        public void Initialize()
        {
            IThemeUtilities utilites =
                Telligent.Evolution.Extensibility.Version1.PluginManager.GetSingleton<IThemeUtilities>();

            if (utilites == null || !utilites.EnableSourceMap)
                return;

            _siteThemeTypeId = SiteThemeContext.Instance().ThemeTypeID;
            _themeContextId = Guid.Empty;
            _siteThemeName = CSContext.Current.SiteTheme;

            CentralizedFileStorage.Events.AfterCreate += AfterCreateStylesheet;
            CentralizedFileStorage.Events.AfterUpdate += AfterUpdateStylesheet;
        }

        public string Name
        {
            get { return "4 Roads - Source maps utility"; }
        }

        #endregion

        #region Event handlers

        void AfterCreateStylesheet(CentralizedFileAfterCreateEventArgs e)
        {
            UpsertSourceMap(e.FileName, e.FileStoreKey, e.Path);
        }

        void AfterUpdateStylesheet(CentralizedFileAfterUpdateEventArgs e)
        {
            UpsertSourceMap(e.FileName, e.FileStoreKey, e.Path);
        }

        #endregion

        internal void GenerateHostVersionedSourceMaps()
        {
            ICentralizedFileStorageProvider fileStore = CentralizedFileStorage.GetFileStore(Constants.ThemeFiles);
            string stylesheetPath = string.Format(@"s.fd.{0}.{1}", _siteThemeName, Constants.ThemeStylesheetFiles);
            var files = fileStore.GetFiles(stylesheetPath, PathSearchOption.TopLevelPathOnly);

            // Update source maps
            foreach (ICentralizedFile file in files.Where(f => f.FileName.EndsWith(".css")))
            {
                UpsertSourceMap(file.FileName, file.FileStoreKey, file.Path);
            }
        }

        private void UpsertSourceMap(string fileName, string fileStoreKey, string path)
        {
            const string propertyName = Constants.ThemeStylesheetFiles;

            // Test if modified file is a stylesheet
            if (fileName.EndsWith(".css") && fileStoreKey == Constants.ThemeFiles && path.EndsWith(propertyName))
            {
                ICentralizedFileStorageProvider fileStore = CentralizedFileStorage.GetFileStore(fileStoreKey);
                string mapName = string.Concat(fileName, ".map");

                if (fileStore != null)
                {
                    ICentralizedFile mapFile = fileStore.GetFile(path, mapName);

                    // Check if source map exists
                    if (mapFile != null)
                    {
                        string writePath = GetSanitisedPath(_siteThemeTypeId, _themeContextId, _siteThemeName,
                            propertyName, new Uri(Globals.FullPath("~/")));

                        if (CentralizedFileStorage.GetFileStore(Constants.ThemeFiles).GetFile(writePath, mapName) == null)
                        {
                            lock (_lock)
                            {
                                if (CentralizedFileStorage.GetFileStore(Constants.ThemeFiles).GetFile(writePath, mapName) ==
                                    null)
                                {
                                    using (DistributedMonitor distributedMonitor = new DistributedMonitor())
                                    {
                                        const int limit = 5;

                                        for (int i = 0; i < limit; i++)
                                        {
                                            using (
                                                DistributedLock distributedLock =
                                                    distributedMonitor.Enter(Constants.DistributedMonitorKey))
                                            {
                                                if (distributedLock.IsOwner)
                                                {
                                                    if (
                                                        CentralizedFileStorage.GetFileStore(Constants.ThemeFiles)
                                                            .GetFile(writePath, mapName) ==
                                                        null)
                                                    {

                                                        byte[] array;

                                                        using (Stream stream = mapFile.OpenReadStream())
                                                        {
                                                            array = new byte[stream.Length];
                                                            stream.Read(array, 0, array.Length);
                                                            stream.Close();
                                                        }

                                                        string text = Encoding.UTF8.GetString(array);

                                                        // Modify paths
                                                        text = text.Replace("../",
                                                            string.Format("{0}/cfs-file/__key/themefiles/s-fd-{1}-",
                                                                CSContext.Current.ApplicationPath, _siteThemeName));
                                                        array = Encoding.UTF8.GetBytes(text);
                                                        CentralizedFileStorage.GetFileStore(Constants.ThemeFiles)
                                                            .AddUpdateFile(writePath, mapName, new MemoryStream(array));
                                                    }
                                                    break;
                                                }
                                                distributedMonitor.Wait(distributedLock, 5);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string GetCurrentHostVersionKey()
        {
            const string propertyName = "CurrentHostVersionKey";
            Type objType = typeof(ThemeFiles);
            PropertyInfo propInfo = objType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Static);

            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                  string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));

            return propInfo.GetValue(null) as string;
        }

        private static string GetSanitisedPath(Guid themeTypeId, Guid themeContextId, string themeName, string propertyName, Uri uri)
        {
            return CentralizedFileStorage.MakePath(new[]
            {
                GetHostVersionPath(themeTypeId, themeContextId, themeName, propertyName),
                uri.Scheme.ToLower(),
                uri.Host.ToLower().Replace(".", ""),
                uri.Port.ToString("0")
            });
        }

        private static string GetHostVersionPath(Guid themeTypeId, Guid themeContextId, string themeName, string propertyName)
        {
            return CentralizedFileStorage.MakePath(new[]
            {
                "h",
                GetCurrentHostVersionKey(),
                ThemeFiles.MakePath(themeTypeId, themeContextId, themeName, propertyName)
            });
        }
    }
}