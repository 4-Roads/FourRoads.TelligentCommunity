using System.Web;

namespace FourRoads.TelligentCommunity.PwaFeatures.Extensions
{
    public class UtilityExtension
    {
        public UtilityExtension(string firebaseConfig)
        {
            FirebaseConfig = firebaseConfig;
        }
        public void RegisterPwaMeta(string additionalHeader)
        {
            HttpContext.Current.Items.Add("pwa_manifest_path", additionalHeader);
        }

        public string FirebaseConfig
        {
            get;
            private set;
        }
    }
}