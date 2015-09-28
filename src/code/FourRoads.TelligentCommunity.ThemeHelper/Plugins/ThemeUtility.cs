using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Common;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.ScriptedContentFragments.Services;

namespace FourRoads.TelligentCommunity.DeveloperTools.Plugins
{
    public class ThemeUtility: ISingletonPlugin
    {
        #region Fields

        private static IList<string> _themeFileFolders;

        private Guid _userThemeTypeId;
        private Guid _siteThemeTypeId;
        private Guid _groupThemeTypeId;
        private Guid _blogThemeTypeId;
        private Guid _themeContextId;
        private string _siteThemeName;
        private List<ThemeFileInfo> _themeFileInfos;
        private ThemeConfigurationData _themeConfigurationData;
        private IThemeUtilities _utilities;

        protected ISecurityService SecurityService;
        protected IContentFragmentPageService ContentFragmentPageService;
        protected IContentFragmentScopedPropertyService ContentFragmentScopedPropertyService;
        protected IThemeTypeService ThemeTypeService;

        #endregion

        #region Internal classes

        private class ThemeFileInfo
        {
            public ThemeFile ThemeFile { get; set; }
            public bool HasDeletedConfiguredVersion { get; set; }
            public bool HasConfiguredVersion { get; set; }
        }

        #endregion

        #region Properties

        private IEnumerable<ThemeFileInfo> ThemeFileInfos
        {
            get
            {
                if (_themeFileInfos == null)
                {
                    _themeFileInfos = new List<ThemeFileInfo>();

                    foreach (string folder in _themeFileFolders)
                    {
                        _themeFileInfos.AddRange(
                            ThemeFiles.GetFiles(_siteThemeTypeId, _themeContextId, _siteThemeName, folder)
                                .Select(f =>
                                {
                                    List<ThemeFile> themeFiles = (_themeConfigurationData != null)
                                        ? ThemeFiles.DeserializeThemeFiles(_siteThemeTypeId,
                                            _themeContextId, _siteThemeName,
                                            folder,
                                            _themeConfigurationData.GetStringValue(folder, ""))
                                            .ToList()
                                        : null;
                                    return new ThemeFileInfo
                                    {
                                        ThemeFile = f,
                                        HasConfiguredVersion =
                                            ThemeFiles.GetConfiguredFile(_siteThemeTypeId, _themeContextId,
                                                _siteThemeName,
                                                folder, f.FileName) != null,
                                        HasDeletedConfiguredVersion =
                                            (themeFiles != null && !themeFiles.Exists(
                                                t => t.FileName == f.FileName && t.PropertyName == f.PropertyName))
                                    };
                                }));
                    }
                }

                return _themeFileInfos;
            }
        }

        #endregion

        #region IPlugin Members

        public string Description
        {
            get { return "Provides methods to selectively revert theme"; }
        }

        public void Initialize()
        {
            _utilities =
                Telligent.Evolution.Extensibility.Version1.PluginManager.GetSingleton<IThemeUtilities>();

            if (_utilities == null || !_utilities.EnableThemePageControls)
                return;

            ContentFragmentPageService = Services.Get<IContentFragmentPageService>();
            ContentFragmentScopedPropertyService = Services.Get<IContentFragmentScopedPropertyService>();
            ThemeTypeService = Services.Get<IThemeTypeService>();
            _userThemeTypeId = CSContext.Current.SiteSettings.UserThemeTypeID;
            _siteThemeTypeId = SiteThemeContext.Instance().ThemeTypeID;
            _groupThemeTypeId = CSContext.Current.SiteSettings.GroupThemeTypeID;
            _blogThemeTypeId = CSContext.Current.SiteSettings.BlogThemeTypeID;
            _themeContextId = Guid.Empty;
            _siteThemeName = CSContext.Current.SiteTheme;
            _themeConfigurationData = ThemeConfigurationDatas.GetThemeConfigurationData(_siteThemeTypeId,
                _themeContextId,
                _siteThemeName, false);
            _themeFileFolders = new List<string>
            {
                Constants.ThemeStylesheetFiles,
                Constants.ThemePrintStylesheetFiles,
                Constants.ThemeJavascriptFiles,
                Constants.ThemeGeneralFiles
            };
        }

        public string Name
        {
            get { return "4 Roads - Theme utility"; }
        }

        #endregion

        #region Reversion methods

        internal void ResetCache()
        {
            Services.Get<IFactoryDefaultScriptedContentFragmentService>().ExpireCache();
            Services.Get<IScriptedContentFragmentService>().ExpireCache();
            Services.Get<IContentFragmentPageService>().RemoveAllFromCache();
            Services.Get<IContentFragmentService>().RefreshContentFragments();
            ThemeFiles.RequestHostVersionedThemeFileRegeneration();
            SystemFileStore.RequestHostVersionedThemeFileRegeneration();
        }

        internal void RevertTheme()
        {
            RevertTheme(ReversionType.All);
        }

        internal void RevertTheme(ReversionType reversionType)
        {
            if((reversionType & ReversionType.Layouts) > 0)
                RevertLayouts();
            
            if ((reversionType & ReversionType.HeadersAndFooters) > 0)
                RevertHeadersAndFooters();
            
            if ((reversionType & ReversionType.Configuration) > 0)
                RevertConfiguration();
            
            if ((reversionType & ReversionType.Files) > 0)
                RevertFiles();
            
            if ((reversionType & ReversionType.ScopedProperties) > 0)
                RevertScopedProperties();

            if (reversionType != ReversionType.None)
                ResetCache();

            if ((reversionType & ReversionType.Files) > 0)
            {
                SourceMapUtility sourceMapUtility =
                    Telligent.Evolution.Extensibility.Version1.PluginManager.GetSingleton<SourceMapUtility>();

                sourceMapUtility.GenerateHostVersionedSourceMaps();
            }
        }

        internal void RevertLayouts()
        {
            if (_utilities.EnableFileSystemWatcher)
            {
                ContentFragmentPageService.DeleteConfiguredAndDefault(_userThemeTypeId, _siteThemeName, true);
                ContentFragmentPageService.DeleteConfiguredAndDefault(_siteThemeTypeId, _siteThemeName, true);
                ContentFragmentPageService.DeleteConfiguredAndDefault(_groupThemeTypeId, _siteThemeName, true);
                ContentFragmentPageService.DeleteConfiguredAndDefault(_blogThemeTypeId, _siteThemeName, true);
            }
            else
            {
                ContentFragmentPageService.DeleteConfigured(_userThemeTypeId, _siteThemeName, true);
                ContentFragmentPageService.DeleteConfigured(_siteThemeTypeId, _siteThemeName, true);
                ContentFragmentPageService.DeleteConfigured(_groupThemeTypeId, _siteThemeName, true);
                ContentFragmentPageService.DeleteConfigured(_blogThemeTypeId, _siteThemeName, true);
            }
        }

        internal void RevertHeadersAndFooters()
        {
            if (_utilities.EnableFileSystemWatcher)
            {
                ContentFragmentPageService.DeleteConfiguredAndDefaultHeaders(_siteThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredAndDefaultHeaders(_groupThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredAndDefaultHeaders(_blogThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredAndDefaultFooters(_siteThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredAndDefaultFooters(_groupThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredAndDefaultFooters(_blogThemeTypeId, _siteThemeName);
            }
            else
            {
                ContentFragmentPageService.DeleteConfiguredHeaders(_siteThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredHeaders(_groupThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredHeaders(_blogThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredFooters(_siteThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredFooters(_groupThemeTypeId, _siteThemeName);
                ContentFragmentPageService.DeleteConfiguredFooters(_blogThemeTypeId, _siteThemeName);
            }
        }

        internal void RevertConfiguration()
        {
            ThemeConfigurationData themeConfigurationData = ThemeConfigurationDatas.GetThemeConfigurationData(_siteThemeTypeId,
                _themeContextId, _siteThemeName, false);

            if (themeConfigurationData == null)
                return;

            PropertyGroup[] propertyGroups = themeConfigurationData.PropertyGroups;

            foreach (
                Property current in
                    propertyGroups.SelectMany(
                        propertyGroup =>
                            propertyGroup.GetAllProperties()
                                .Where(current => !_themeFileFolders.Contains(current.ID))))
            {
                themeConfigurationData.ClearValue(current);
            }

            themeConfigurationData.Commit();
        }

        internal void RevertFiles()
        {
            List<ThemeFileInfo> themeFileInfoList = ThemeFileInfos.ToList();
            ThemeConfigurationData configuration =
                ThemeConfigurationDatas.GetFactoryDefaultThemeConfigurationData(_siteThemeTypeId, _siteThemeName);

            themeFileInfoList.ForEach(info =>
            {
                if (info.HasConfiguredVersion)
                {
                    ThemeFiles.DeleteFile(info.ThemeFile.ThemeTypeID,
                        info.ThemeFile.ThemeContextID, info.ThemeFile.ThemeName,
                        info.ThemeFile.PropertyName, info.ThemeFile.FileName);

                    return;
                }

                if (info.HasDeletedConfiguredVersion)
                {
                    IEnumerable<ThemeFile> themeFiles = ThemeFiles.DeserializeThemeFiles(configuration,
                        info.ThemeFile.PropertyName,
                        configuration.GetCustomValue(info.ThemeFile.PropertyName, String.Empty));
                    ThemeFile themeFile =
                        themeFiles.FirstOrDefault(
                            t =>
                                String.Compare(t.FileName, info.ThemeFile.FileName,
                                    StringComparison.OrdinalIgnoreCase) == 0);

                    if (themeFile != null)
                    {
                        IList<ThemeFile> list = ThemeFiles.DeserializeThemeFiles(_siteThemeTypeId,
                            _themeContextId, _siteThemeName, info.ThemeFile.PropertyName,
                            _themeConfigurationData.GetCustomValue(
                                info.ThemeFile.PropertyName, String.Empty));

                        list.Add(themeFile);

                        string value = ThemeFiles.SerializeThemeFiles(list);

                        _themeConfigurationData.SetCustomValue(
                            info.ThemeFile.PropertyName, value);

                        ThemeConfigurationDatas.Update(_themeConfigurationData);
                    }
                }
            });
        }

        internal void RevertScopedProperties()
        {
            ContentFragmentScopedPropertyService.RevertScopedProperties(_siteThemeName, _siteThemeTypeId);
            ContentFragmentScopedPropertyService.RevertScopedProperties(_siteThemeName, _groupThemeTypeId);
            ContentFragmentScopedPropertyService.RevertScopedProperties(_siteThemeName, _blogThemeTypeId);
        }

        #endregion
    }
}
