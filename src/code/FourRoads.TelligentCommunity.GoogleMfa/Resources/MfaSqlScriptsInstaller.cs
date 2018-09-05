using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.GoogleMfa.Resources
{
    public class MfaSqlScriptsInstaller : FourRoads.Common.TelligentCommunity.Plugins.Base.SqlScriptsInstaller
    {
        protected override string ProjectName => "Google MFA";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.GoogleMfa.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}
