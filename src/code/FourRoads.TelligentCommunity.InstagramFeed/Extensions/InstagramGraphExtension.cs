using FourRoads.TelligentCommunity.InstagramFeed.ScriptedContentFragments;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.InstagramFeed.Extensions
{
    public class InstagramGraphExtension : IScriptedContentFragmentExtension
    {
        public void Initialize()
        {

        }

        public string Name => "4 Roads - Instagram Feed Extension";
        public string Description => "Instagram Graph API wrapper";
        public string ExtensionName => "frcommon_v1_instagramFeed";
        public object Extension => new InstagramGraphScriptedFragment();
    }
}
