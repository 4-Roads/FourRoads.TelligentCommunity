using System;
using System.Linq;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Security.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MetaData.Security
{
    public class PermissionRegistrar : IPermissionRegistrar, ITranslatablePlugin, IApplicationPlugin
    {
        private ITranslatablePluginController _controller;

        public static Guid SiteEditMetaDataPermission = new Guid("{DD353225-54CF-4F65-8E07-B7E38D19316E}");
        public static Guid EditMetaDataPermission = new Guid("{A54AB949-9113-4E1D-A3BC-F4A639BA510D}");

        public void Initialize()
        {
             
        }

        public string Name => "Permissions";

        public string Description => "Handles the registration of permissions for meta data";

        public void RegisterPermissions(IPermissionRegistrarController permissionController)
        {
            permissionController.Register(new Permission(EditMetaDataPermission, "metadata_updatepage", "metadata_updatepagedescription", _controller, 
                Apis.Get<IGroups>().ApplicationTypeId,
                new PermissionConfiguration()
                {
                    Joinless = new JoinlessGroupPermissionConfiguration { Administrators = true },
                    PublicOpen = new MembershipGroupPermissionConfiguration { Owners = true },
                    PublicClosed = new MembershipGroupPermissionConfiguration { Owners = true },
                    PrivateListed = new MembershipGroupPermissionConfiguration { Owners = true },
                    PrivateUnlisted = new MembershipGroupPermissionConfiguration { Owners = true }
                }));

            permissionController.Register(
                new Permission(
                    SiteEditMetaDataPermission,
                    "metadata_updatesite",
                    "metadata_updatesitedescription",
                    _controller,
                    Guid.Empty,
                    new PermissionConfiguration()
                    {
                        Joinless = new JoinlessGroupPermissionConfiguration {Administrators = true},
                        PublicOpen = new MembershipGroupPermissionConfiguration {Owners = true},
                        PublicClosed = new MembershipGroupPermissionConfiguration {Owners = true},
                        PrivateListed = new MembershipGroupPermissionConfiguration {Owners = true},
                        PrivateUnlisted = new MembershipGroupPermissionConfiguration {Owners = true}
                    }));
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _controller = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation trn= new Translation("en-us");

                trn.Set("metadata_updatepage", "Meta Data - Edit");
                trn.Set("metadata_updatepagedescription", "Grants a user permission to edit meta data for a page");


                trn.Set("metadata_updatesite", "Meta Data - Sitewide Edit");
                trn.Set("metadata_updatesitedescription", "Grants a user permission to edit meta data for the site");

                return new[] { trn };
            }
        }
    }
}
