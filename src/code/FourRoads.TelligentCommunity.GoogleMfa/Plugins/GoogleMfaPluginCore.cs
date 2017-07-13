using System;
using System.Collections.Generic;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.GoogleMfa.Extensions;
using FourRoads.TelligentCommunity.GoogleMfa.Interfaces;
using FourRoads.TelligentCommunity.GoogleMfa.Logic;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Plugins
{
    public class GoogleMfaPluginCore : IPluginGroup , IBindingsLoader, INavigable
    {
        private IMfaLogic _mfaLogic;

        public void Initialize()
        {

        }

        public string Name => "Google MFA Plugin";

        public string Description => "Plugin for adding MFA using the google authenticator";

        public void RegisterUrls(IUrlController controller)
        {
            Injector.Get<IMfaLogic>().RegisterUrls(controller);
        }

        public IEnumerable<Type> Plugins => new[]
        {
            typeof (GoogleAuthenticatorExtension),
            typeof (FactoryDefaultWidgetProviderInstaller),
        };

        public void LoadBindings(IContainer container)
        {
            container.Register<IMfaLogic, MfaLogic>(Reuse.Singleton);
        }

        public int LoadOrder => 0;
    }
}
