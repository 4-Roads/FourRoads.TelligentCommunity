using System;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Version1;
using System.Web;

namespace FourRoads.Common.TelligentCommunity.Plugins.Navigation
{
    public class LogonNavigationExtensions : ISiteCustomNavigationPlugin , ITranslatablePlugin
    {
        private ITranslatablePluginController _translatablePluginController;

        public string Description => "Logon Navigation link";

        public string Name => "4 Roads - Navigation Logon";

        public string NavigationTypeName => "Logon";

        public PropertyGroup[] GetConfigurationProperties()
        {
            return new PropertyGroup[ 0 ];
        }

        public ICustomNavigationItem GetNavigationItem(Guid id, ICustomNavigationItemConfiguration configuration)
        {
            return new CustomNavigationItem(id,
                        () => _translatablePluginController.GetLanguageResourceValue("Logon"),
                        () => PublicApi.CoreUrls.LogIn(new CoreUrlLoginOptions { ReturnToCurrentUrl = true}), i => PublicApi.Users.AccessingUser.Username == PublicApi.Users.AnonymousUserName,
                        () =>
                        {
                            if (PublicApi.Url.CurrentContext != null &&
                                !string.IsNullOrWhiteSpace(PublicApi.Url.CurrentContext.PageName))
                                return PublicApi.Url.CurrentContext.PageName == "common-login";
                            return false;
                        })
            {
                Plugin = this,
                Children = new ICustomNavigationItem[ 0 ],
                Configuration = configuration,
                CssClass = "login"
            };
        }

        public void Initialize()
        {
           
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translatablePluginController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                var translation = new Translation("en-us");

                translation.Set("Logon", "Log In");

                return new Translation[]
                {
                    translation
                };
            }
        }
    }

    public class RegisterNavigationExtensions : ISiteCustomNavigationPlugin, ITranslatablePlugin
    {
        private ITranslatablePluginController _translatablePluginController;

        public string Description => "Register Navigation link";

        public string Name => "4 Roads - Navigation Register";

        public string NavigationTypeName => "Register";

        public PropertyGroup[] GetConfigurationProperties()
        {
            return new PropertyGroup[ 0 ];
        }

        public ICustomNavigationItem GetNavigationItem(Guid id, ICustomNavigationItemConfiguration configuration)
        {
            return new CustomNavigationItem(id,
                        () => _translatablePluginController.GetLanguageResourceValue("Register"),
                        () => PublicApi.CoreUrls.Register(HttpContext.Current.Request.Url.LocalPath), i => PublicApi.Users.AccessingUser.Username == PublicApi.Users.AnonymousUserName,
                        () => PublicApi.Url.CurrentContext.PageName == "user-createuser")
            {
                Plugin = this,
                Children = new ICustomNavigationItem[ 0 ],
                Configuration = configuration,
                CssClass = "register"
            };
        }

        public void Initialize()
        {

        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translatablePluginController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                var translation = new Translation("en-us");

                translation.Set("Register", "Register");

                return new Translation[]
                {
                    translation
                };
            }
        }
    }

}
