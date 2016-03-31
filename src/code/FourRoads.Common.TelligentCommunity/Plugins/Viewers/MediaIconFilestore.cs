using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Viewers
{
    public class MediaIconFilestore : ICentralizedFileStore
    {
        public static string FILESTORE_KEY = "fourroads.mediaviewers";

        public void Initialize()
        {
 
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
