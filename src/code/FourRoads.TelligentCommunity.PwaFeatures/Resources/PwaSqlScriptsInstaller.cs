using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Utility;

namespace FourRoads.TelligentCommunity.PwaFeatures.Resources
{
    public class PwaSqlScriptsInstaller : Installer.SqlScriptsInstaller
    {
        protected override string ProjectName => "PWA";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.PwaFeatures.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}
