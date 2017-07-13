using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.GoogleMfa.ScriptedFragments;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Extensions
{
    public class GoogleAuthenticatorExtension : IScriptedContentFragmentExtension
    {
        public void Initialize()
        {
            
        }

        public string Name => "Google Authenticator Extension";
        public string Description => "Used for MFA authentication";
        public string ExtensionName => "frcommon_v1_googleMfa";
        public object Extension => new GoogleAuthenticatorScriptedFragment();
    }
}
