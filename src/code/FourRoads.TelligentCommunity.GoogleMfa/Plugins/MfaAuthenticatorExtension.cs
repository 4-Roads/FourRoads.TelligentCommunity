using FourRoads.TelligentCommunity.GoogleMfa.Plugins.WidgetApi;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Plugins
{
    public class MfaAuthenticatorExtension : IScriptedContentFragmentExtension
    {
        public void Initialize()
        {
            
        }

        public string Name => "4 Roads - Google Authenticator Extension";
        public string Description => "Used for MFA authentication";
        public string ExtensionName => "frcommon_v1_googleMfa";
        public object Extension => new MfaScriptedFragment();
    }
}
