using System;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;
using System.Web;
using Telligent.Evolution.Extensibility;

using Telligent.Evolution.Extensibility.UI.Version3;
using TelligentConfiguration = Telligent.Evolution.Extensibility.Configuration.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Navigation
{
    public class LogonNavigationExtensions : ISiteCustomNavigationPlugin , ITranslatablePlugin
    {
        private ITranslatablePluginController _translatablePluginController;

        public string Description => "Logon Navigation link";

        public string Name => "4 Roads - Navigation Logon";

        public string NavigationTypeName => "Logon";

        public TelligentConfiguration.PropertyGroup[] GetConfigurationProperties()
        {
            return new TelligentConfiguration.PropertyGroup[ 0 ];
        }

        CustomNavigationItem ICustomNavigationPlugin.GetNavigationItem(Guid id, ICustomNavigationItemConfiguration configuration)
        {
            return new CustomNavigationItem() {
                DefaultLabel = () => _translatablePluginController.GetLanguageResourceValue("Logon"),
                Url = Apis.Get<ICoreUrls>().LogIn(new CoreUrlLoginOptions { ReturnToCurrentUrl = true}),
                CssClass = "login",
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

        public TelligentConfiguration.PropertyGroup[] GetConfigurationProperties()
        {
            return new TelligentConfiguration.PropertyGroup[ 0 ];
        }

        CustomNavigationItem ICustomNavigationPlugin.GetNavigationItem(Guid id, ICustomNavigationItemConfiguration configuration)
        {
            return new CustomNavigationItem()
            {
                DefaultLabel = () => _translatablePluginController.GetLanguageResourceValue("Register"),
                Url = Apis.Get<ICoreUrls>().Register(HttpContext.Current.Request.Url.LocalPath),
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
