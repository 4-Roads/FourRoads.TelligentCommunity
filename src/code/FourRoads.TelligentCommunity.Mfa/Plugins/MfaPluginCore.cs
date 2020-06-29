using System;
using System.Collections.Generic;
using System.Linq;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Mfa.DataProvider;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Logic;
using FourRoads.TelligentCommunity.Mfa.Resources;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class MfaPluginCore : IPluginGroup , IBindingsLoader, INavigable, IConfigurablePlugin, ITranslatablePlugin
    {
        private IPluginConfiguration _configuration;
        private ITranslatablePluginController _translations;

        public void Initialize()
        {
            Injector.Get<IMfaLogic>().Initialize(_configuration.GetBool("emailVerification").Value , PluginManager.Get<VerifyEmailPlugin>().FirstOrDefault() , PluginManager.Get<EmailVerifiedSocketMessage>().FirstOrDefault());
        }

        public string Name => "4 Roads - MFA Plugin";

        public string Description => "Plugin for adding MFA using the google authenticator";

        public void RegisterUrls(IUrlController controller)
        {
            Injector.Get<IMfaLogic>().RegisterUrls(controller);
        }

        public IEnumerable<Type> Plugins => new[]
        {
            typeof (MfaSqlScriptsInstaller),
            typeof (FactoryDefaultWidgetProviderInstaller),
            typeof (MfaAuthenticatorExtension),
            typeof (VerifyEmailPlugin),
            typeof (VerifyEmailTokens),
            typeof (EmailVerifiedSocketMessage)
        };

        public void LoadBindings(IContainer container)
        {
            container.Register<IMfaLogic, MfaLogic>(Reuse.Singleton);
            container.Register<IMfaDataProvider, MfaDataProvider>(Reuse.Singleton);
        }

        public int LoadOrder => 0;
        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup group = new PropertyGroup(){LabelResourceName = "GroupOptions" , Id="options"};

                group.Properties.Add(new Property(){Id="emailVerification" , LabelResourceName = "EmailVerification" , DataType = "bool" , DefaultValue = "true" });

                return new[] {group};
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translations = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation tr = new Translation("en-us");

                tr.Set("EmailVerification", "Enable Email Verification");
                tr.Set("GroupOptions", "Options");

                return new[] {tr};
            }
        }
    }
}
