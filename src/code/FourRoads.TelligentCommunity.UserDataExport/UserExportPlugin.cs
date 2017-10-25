using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.UserDataExport
{
    public class UserExportPlugin : IConfigurablePlugin , ISecuredCentralizedFileStore
    {
        public const string FILESTORE_KEY = "membership.secure.data";

        public string Description
        {
            get { return "Enables users list to be exported from Telligent"; }
        }

        public void Initialize()
        {
          
        }

        public string Name
        {
            get 
            {
                return "4 Roads - User Data Export";
            }
        }

        public bool UserHasAccess(int userId, string path, string fileName)
        {
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions() { Id= userId});

            if (user != null)
            {
                return Apis.Get<IPermissions>().Get(Telligent.Evolution.Components.SitePermission.ManageMembership, userId).IsAllowed;
            }

            return false;

        }

        public string FileStoreKey
        {
            get { return FILESTORE_KEY; }
        }

        public Telligent.DynamicConfiguration.Components.PropertyGroup[] ConfigurationOptions
        {
            get {

                PropertyGroup exportGroup = new PropertyGroup("ExportForm", "Export Form", 1);

                if (PluginManager.IsEnabled(this))
                {
                    Property exportControl = new Property("exportButton", "Export Settings", PropertyType.Custom, 1, "")
                    {
                        ControlType = typeof(UserExportControl)
                    };

                    exportGroup.Properties.Add(exportControl);
                }

                return new[] { exportGroup };   
            
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
  
        }

    }
}
