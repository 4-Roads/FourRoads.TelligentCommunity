using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Routing;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Interfaces;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Resources;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class CommunityCallerPath : ICallerPathVistor
    {
        public string GetPath()
        {
            return InternalGetPath();
        }

        protected string InternalGetPath([CallerFilePath] string path = null)
        {
            return path;
        }
    }

    public class DefaultWidgetInstaller : FactoryDefaultWidgetProviderInstaller<DefaultWidgetInstaller>,
        IScriptedContentFragmentFactoryDefaultProvider, INavigable
    {
        private const string _leaderAssemblyName =
            "Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::{0}";

        private static readonly string _mfaPageName = "mfa";
        private static readonly string _pageVerifyEmailName = "verify-email";
        private static readonly string _pageRequireMfaName = "manage-mfa";

        //{295391e2b78d4b7e8056868ae4fe8fb3}
        private static readonly string _defaultMfaPageLayout =
            $"<contentFragmentPage pageName=\"{_mfaPageName}\" isCustom=\"false\" layout=\"Content\" title=\"4 Roads - MFA Entry\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::295391e2b78d4b7e8056868ae4fe8fb3\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        private static readonly string _defaultVerifyEmailPageLayout =
            $"<contentFragmentPage pageName=\"{_pageVerifyEmailName}\" isCustom=\"false\" layout=\"Content\"  title=\"4 Roads - Verify Email\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::a8b6e56eac3246169d1727c84c17fd66\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        private static readonly string _defaultRequireMfaPageLayout =
            $"<contentFragmentPage pageName=\"{_pageRequireMfaName}\" isCustom=\"false\" layout=\"Content\" title=\"4 Roads - Manage MFA\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::6d9264a6f6c4434c9d9954b87a865e57\" showHeader=\"True\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        private IMfaLogic _mfaLogic;

        protected override string ProjectName => "MFA Widget Installer";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.Mfa.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();

        private IMfaLogic MfaLogic
        {
            get
            {
                if (_mfaLogic == null) _mfaLogic = Injector.Get<IMfaLogic>();

                return _mfaLogic;
            }
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_mfaPageName, "mfa", new SiteRootRouteConstraint(), null, _mfaPageName,
                new PageDefinitionOptions
                {
                    DefaultPageXml = _defaultMfaPageLayout,
                    HasApplicationContext = false,
                    Validate = MfaLogic.ValidateNonAnonymous
                });

            controller.AddPage(_pageVerifyEmailName, "verifyemail", new SiteRootRouteConstraint(), null,
                _pageVerifyEmailName, new PageDefinitionOptions
                {
                    DefaultPageXml = _defaultVerifyEmailPageLayout,
                    HasApplicationContext = false,
                    Validate = MfaLogic.ValidateEmailRequest
                });

            controller.AddPage(_pageRequireMfaName, "manage_mfa", new SiteRootRouteConstraint(), null,
                _pageRequireMfaName, new PageDefinitionOptions
                {
                    DefaultPageXml = _defaultRequireMfaPageLayout,
                    HasApplicationContext = false,
                    Validate = MfaLogic.ValidateNonAnonymous
                });
        }

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier { get; } =
            new Guid("{3BD55B78-2E62-4B77-B6E7-30F6BCE17DE2}");

        void IPlugin.Initialize()
        {
            base.Initialize();

            _mfaLogic = null;
        }

        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }

        public override void Install(Version lastInstalledVersion)
        {
            //Install the widgets
            base.Install(lastInstalledVersion);

            InstallPages();
        }

        protected string GetScriptedFragmentName(string id)
        {
            return string.Format(_leaderAssemblyName, id);
        }

        public void InstallPages()
        {
            //Install the pages and also add the settings widget to the settings page

            void InstallPage(string xmlString)
            {
                try
                {
                    var xml = new XmlDocument();
                    xml.LoadXml(xmlString);
                    var node = xml.SelectSingleNode("//contentFragmentPage");

                    if (node == null || node.Attributes == null)
                        return;

                    var pageName = node.Attributes["pageName"].Value;
                    var themeType = Guid.Parse("0c647246-6735-42f9-875d-c8b991fe739b");

                    if (string.IsNullOrEmpty(pageName))
                        return;

                    foreach (var theme in Themes.List(themeType))
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
                catch (Exception exception)
                {
                    new Exception($"Couldn't load page from {xmlString}.", exception);
                }
            }

            InstallPage(_defaultMfaPageLayout);
            InstallPage(_defaultVerifyEmailPageLayout);
            InstallPage(_defaultRequireMfaPageLayout);

            //Update the settings page to include 
            foreach (var theme in Themes.List(ThemeTypes.Site))
                if (ThemePages.FactoryDefaultExists(theme, "user-edituser", false))
                {
                    var fragments = ThemePageContentFragments.ListFactoryDefault(theme, "user-edituser", false);

                    var findFrag = GetScriptedFragmentName("3317de4f74eb434da129a95a41aebc5b");

                    if (fragments.All(f => f.ContentFragmentType != findFrag))
                        ThemePageContentFragments.InsertInFactoryDefault(theme, "user-edituser", false,
                            GetScriptedFragmentName("af9000b6cc8c4f658c2046ffdc09c5db"), ContentFragmentPlacement.After,
                            findFrag,
                            "fragmentHeader=%24%7Bresource%3AContentDiscuss_Header%7D&amp;textAreaSelector=.blog-post%20.post-content.user-defined-markup",
                            "full-border with-header");
                }
        }
    }
}