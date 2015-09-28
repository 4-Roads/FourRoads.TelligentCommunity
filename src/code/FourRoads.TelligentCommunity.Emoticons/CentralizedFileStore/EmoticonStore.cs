using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.Emoticons.CentralizedFileStore
{
    public class EmoticonsStore : ICentralizedFileStore
    {
        public static string FILESTORE_KEY = "fourroads.emoticons";

        public void Initialize()
        {
            CentralizedFileStorage.GetFileStore(EmoticonsStore.FILESTORE_KEY).Delete("css");
        }

        public string Name
        {
            get { return "Filestore"; }
        }

        public string Description
        {
            get { return "Handles the filestorage for emoticons"; }
        }

        public string FileStoreKey
        {
            get { return FILESTORE_KEY; }
        }
    }
}