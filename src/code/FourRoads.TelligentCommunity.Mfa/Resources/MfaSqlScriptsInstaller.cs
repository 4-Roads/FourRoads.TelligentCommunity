using FourRoads.TelligentCommunity.Installer.Components.Utility;

namespace FourRoads.TelligentCommunity.Mfa.Resources
{
    public class MfaSqlScriptsInstaller : Installer.SqlScriptsInstaller
    {
        protected override string ProjectName => "MFA SQL Installer";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.Mfa.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}
