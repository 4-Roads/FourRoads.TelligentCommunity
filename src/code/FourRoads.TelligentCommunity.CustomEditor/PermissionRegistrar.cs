using System;
using Telligent.Evolution.Extensibility.Security.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.CustomEditor
{
    public class PermissionRegistrar : IPermissionRegistrar , ITranslatablePlugin
    {
        private ITranslatablePluginController _controller;

        public static Guid CustomEditorSourceButton = new Guid("{56DF5D97-C64E-4253-8AA8-EA16445EDB1D}");

        public void Initialize()
        {
             
        }

        public string Name
        {
            get { return "Permissions"; }
        }

        public string Description
        {
            get { return "Handles the display of the 'source' button in redactor"; }
        }

        public void RegisterPermissions(IPermissionRegistrarController permissionController)
        {
            permissionController.Register(new Permission(CustomEditorSourceButton, "customeditor_source", "customeditor_sourcedescription", _controller, Telligent.Evolution.Api.Content.ContentTypes.RootApplication,
                new PermissionConfiguration()
                {
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

                trn.Set("customeditor_source", "Custom Editor - Edit Source");
                trn.Set("customeditor_sourcedescription", "Custom Editor - allows a user to use the 'source' button");

                return new[] { trn };
            }
        }
    }
}
