using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.PwaFeatures.Resources
{
    public class PwaSqlScriptsInstaller : FourRoads.Common.TelligentCommunity.Plugins.Base.SqlScriptsInstaller
    {
        protected override string ProjectName => "PWA";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.PwaFeatures.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}
