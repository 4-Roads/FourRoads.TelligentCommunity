using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Mfa.Plugins.WidgetApi;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class MfaAuthenticatorExtension : IScriptedContentFragmentExtension
    {
        public void Initialize()
        {
            
        }

        public string Name => "4 Roads - MFA Extension";
        public string Description => "Used for MFA authentication";
        public string ExtensionName => "frcommon_v1_Mfa";
        public object Extension => Injector.Get<MfaScriptedFragment>();
    }
}
