using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.Mfa.Resources
{
    public class MfaSqlScriptsInstaller : FourRoads.Common.TelligentCommunity.Plugins.Base.SqlScriptsInstaller
    {
        protected override string ProjectName => "MFA SQL Installer";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.Mfa.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}
