namespace FourRoads.TelligentCommunity.PwaFeatures.Plugins
{
    public class CustomUrlsPanelExtension
    {
        public CustomUrlsPanelExtension(string userId , string page, string firebaseSenderId, string firebaseConfig)
        {
            UserId = int.Parse(userId);
            Page = page;
            FirebaseSenderId = firebaseSenderId;
            FirebaseConfig = firebaseConfig;
        }

        public int UserId { get; private set; }
        public string Page { get; private set; }
        public string FirebaseSenderId { get; private set; }
        public string FirebaseConfig { get; private set; }
        
    }
}