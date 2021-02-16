using FourRoads.TelligentCommunity.Installer.Components.Utility;

namespace FourRoads.TelligentCommunity.Sentrus.Resources
{
    public class SqlScriptsInstaller : Installer.SqlScriptsInstaller
    {
        protected override string ProjectName
        {
            get { return "LastLogin"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.Sentrus.Resources."; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return new EmbeddedResources(); }
        }
    }
}
