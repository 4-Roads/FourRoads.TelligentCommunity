using System.Collections.Specialized;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.PwaFeatures
{
    public class ServiceWorkerPanelContext : IContextualScriptedContentFragmentExtension
    {
        public ServiceWorkerPanelContext()
        {

        }

        public string ExtensionName => "context";

        public object GetExtension(NameValueCollection context)
        {
            return new ServiceWorkerPanelExtension(context["UserId"], context["Page"], context["FirebaseSenderId"],  context["FirebaseConfig"]);
        }
    }
}