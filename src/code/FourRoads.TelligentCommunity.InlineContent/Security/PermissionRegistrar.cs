using System;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Security.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.InlineContent.Security
{
    public class PermissionRegistrar : IPermissionRegistrar , ITranslatablePlugin
    {
        private ITranslatablePluginController _controller;

        public static Guid EditInlineContentPermission = new Guid("{BD392694-45D6-4605-AC11-491175E7D014}");

        public void Initialize()
        {
             
        }

        public string Name
        {
            get { return "Permissions"; }
        }

        public string Description
        {
            get { return "Handles the registration of permissions for inline content"; }
        }

        public void RegisterPermissions(IPermissionRegistrarController permissionController)
        {
            ApplicationType groupType = Apis.Get<IApplicationTypes>().List().FirstOrDefault(a => a.Name.Equals("group", StringComparison.OrdinalIgnoreCase));

            if (groupType != null)
            {
                permissionController.Register(new Permission(EditInlineContentPermission, "inlinecontent_editcontent", "inlinecontent_editcontentdescription", _controller, groupType.Id.Value,
                    new PermissionConfiguration()
                    {
                        Joinless = new JoinlessGroupPermissionConfiguration { Administrators = true },
                        PublicOpen = new MembershipGroupPermissionConfiguration { Owners = true },
                        PublicClosed = new MembershipGroupPermissionConfiguration { Owners = true },
                        PrivateListed = new MembershipGroupPermissionConfiguration { Owners = true },
                        PrivateUnlisted = new MembershipGroupPermissionConfiguration { Owners = true }
                    }));
            }
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

                trn.Set("inlinecontent_editcontent", "Inline Content - Edit Content");
                trn.Set("inlinecontent_editcontentdescription", "Grants a user permission to edit inline content");

                return new[] { trn };
            }
        }
    }
}
