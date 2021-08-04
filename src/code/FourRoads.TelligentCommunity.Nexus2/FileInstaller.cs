using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using Telligent.Evolution.Extensibility.Storage.Version1;


namespace FourRoads.TelligentCommunity.Nexus2
{
    public class FileInstaller : CfsFilestoreInstaller , ICentralizedFileStore
    {
        public const string FILESTOREKEY = "customoauthimages";

        private EmbeddedResources _resources = new EmbeddedResources();

        protected override string ProjectName
        {
            get { return "Nexus2"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.Nexus2.Resources"; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return _resources; }
        }

        public string FileStoreKey {
            get { return FILESTOREKEY; }
        }
    }
}
