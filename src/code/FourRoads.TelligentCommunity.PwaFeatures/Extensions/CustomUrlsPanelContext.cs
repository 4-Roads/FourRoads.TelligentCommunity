using System.Collections.Specialized;
using FourRoads.TelligentCommunity.PwaFeatures.Plugins;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.PwaFeatures.Extensions
{
    public class CustomUrlsPanelContext : IContextualScriptedContentFragmentExtension
    {
        public string ExtensionName => "context";

        public object GetExtension(NameValueCollection context)
        {
            return new CustomUrlsPanelExtension(context["UserId"], context["Page"], context["FirebaseSenderId"],  context["FirebaseConfig"]);
        }
    }
}