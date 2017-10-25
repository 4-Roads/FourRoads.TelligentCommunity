using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.Splash.Plugins
{
    public class Filestore : ISecuredCentralizedFileStore
    {
        public void Initialize()
        {

        }

        public string Name {
            get { return "Filestore"; } 
        }

        public string Description {
            get { return "Secure storage for CSV file that contains a list of users that have registered and interest in the site"; }
        }

        public string FileStoreKey
        {
            get { return Constants.FILESTOREKEY; }
        }

        public bool UserHasAccess(int userId, string path, string fileName)
        {
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions() {Id = userId});

            if (!user.HasErrors())
            {
                return Apis.Get<IRoleUsers>().IsUserInRoles(user.Username, new[] {"Administrators"});
            }

            return false;
        }
    }
}