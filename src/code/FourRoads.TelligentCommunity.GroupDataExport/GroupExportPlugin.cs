using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using System.Collections.Generic;
using System;

namespace FourRoads.TelligentCommunity.GroupDataExport
{
    public class GroupExportPlugin : IConfigurablePlugin , ISecuredCentralizedFileStore, IPluginGroup
    {
        public const string FILESTORE_KEY = "group.secure.data";

        public string Description
        {
            get { return "Enables group details to be exported from Telligent"; }
        }

        public IEnumerable<Type> Plugins
        {
            get
            {
                return new[]
                {
                    typeof (GroupExportPropertyTemplate),
                };
            }
        }

        public void Initialize()
        {
          
        }

        public string Name
        {
            get 
            {
                return "4 Roads - Group Data Export";
            }
        }

        public bool UserHasAccess(int userId, string path, string fileName)
        {
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions() { Id= userId});

            if (user != null)
            {
                return Apis.Get<Telligent.Evolution.Extensibility.Api.Version2.IPermissions>()
                    .CheckPermission(Telligent.Evolution.Components.SitePermission.ManageMembership, userId).IsAllowed;
            }

            return false;

        }

        public string FileStoreKey
        {
            get { return FILESTORE_KEY; }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get {

                PropertyGroup exportGroup = new PropertyGroup {Id="ExportForm", LabelText = "Export Form" };

                if (PluginManager.IsEnabled(this))
                {
                    var exportControl = new Property
                    {
                        Id = "exportButton",
                        LabelText = "Export Settings",
                        DescriptionText = "",
                        DataType = "custom",
                        Template = "groupExport_template",
                        OrderNumber = 1,
                        DefaultValue = ""
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
