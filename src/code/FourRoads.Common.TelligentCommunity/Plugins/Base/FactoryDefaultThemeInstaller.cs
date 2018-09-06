using System;
using System.Xml;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Theme = Telligent.Evolution.Extensibility.UI.Version1.Theme;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public abstract class FactoryDefaultThemeInstaller : IInstallablePlugin
    {
        #region IPlugin Members

        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }

        public string Name => ProjectName + " - Theme";

        public string Description => "Installs the default theme for " + ProjectName + ".";

        public void Initialize()
        {
            ThemeVersionHelper.LocalVersionCheck($"theme-{ProjectName}", Version, Install);
        }

        #endregion

        #region IInstallablePlugin Members

        public void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version)
            {
                Uninstall();

                #region Install custom theme

                string basePath = BaseResourcePath + "Themes.";

                EmbeddedResources.EnumerateReosurces(basePath, ".xml", resourceName =>
                {
                    // Get widget identifier
                    XmlDocument xmlDocument = new XmlDocument();

                    try
                    {
                        xmlDocument.LoadXml(EmbeddedResources.GetString(resourceName));
                        XmlNode node = xmlDocument.SelectSingleNode("/theme/themeImplementation");

                        if (node != null)
                            ThemeConfigurations.DeserializeTheme(node, true, false);
                    }
                    catch (Exception exception)
                    {
                        new TCException(
                            string.Format("Couldn't load theme from '{0}' embedded resource.", resourceName), exception).Log();
                    }
                });


                #endregion

                #region Install custom pages into theme (and revert any configured defaults or contextual versions of these pages)

                basePath = BaseResourcePath + "Pages.";

                EmbeddedResources.EnumerateReosurces(basePath, ".xml", resourceName =>
                {
                    XmlDocument xml = new XmlDocument();

                    try
                    {
                        xml.LoadXml(EmbeddedResources.GetString(resourceName));
                        XmlNode node = xml.SelectSingleNode("/theme/contentFragmentPages/contentFragmentPage");

                        if (node == null || node.Attributes == null)
                            return;

                        string pageName = node.Attributes["pageName"].Value;
                        string themeType = node.Attributes["themeType"].Value;

                        if (string.IsNullOrEmpty(pageName) || string.IsNullOrEmpty(themeType))
                            return;

                        foreach (Theme theme in Themes.List(Guid.Parse(themeType)))
                        {
                            if (theme != null)
                            {
                                if (theme.IsConfigurationBased)
                                {
                                    ThemePages.AddUpdateFactoryDefault(theme, node);
                                    ThemePages.DeleteDefault(theme, pageName, true);
                                }
                                else
                                {
                                    ThemePages.AddUpdateDefault(theme, node);
                                }

                                ThemePages.Delete(theme, pageName, true);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        new TCException(string.Format("Couldn't load page from '{0}' embedded resource.", resourceName), exception).Log();
                    }
                });

                #endregion
            }
        }

        public void Uninstall()
        {
            if (!Diagnostics.IsDebug(GetType().Assembly))
            {
                #region Delete custom pages from theme (factory defaults, configured defaults, and contextual pages)

                string basePath = BaseResourcePath + "Themes.";

                EmbeddedResources.EnumerateReosurces(basePath, ".xml", resourceName =>
                {
                    XmlDocument xml = new XmlDocument();

                    try
                    {
                        xml.LoadXml(EmbeddedResources.GetString(resourceName));
                        XmlNode node = xml.SelectSingleNode("/theme/contentFragmentPages/contentFragmentPage");

                        if (node == null || node.Attributes == null)
                            return;

                        string pageName = node.Attributes["pageName"].Value;
                        string themeType = node.Attributes["themeType"].Value;

                        if (string.IsNullOrEmpty(pageName) || string.IsNullOrEmpty(themeType))
                            return;

                        foreach (Theme theme in Themes.List(Guid.Parse(themeType)))
                        {
                            if (theme != null)
                            {
                                if (theme.IsConfigurationBased)
                                {
                                    ThemePages.DeleteFactoryDefault(theme, pageName, true);
                                }

                                ThemePages.DeleteDefault(theme, pageName, true);
                                ThemePages.Delete(theme, pageName, true);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        new TCException(
                            string.Format("Couldn't delete page from '{0}' embedded resource.", resourceName), exception)
                            .Log();
                    }
                });

                #endregion
            }
        }

        public Version Version
        {
            get { return GetType().Assembly.GetName().Version; }
        }

        #endregion
    }
}