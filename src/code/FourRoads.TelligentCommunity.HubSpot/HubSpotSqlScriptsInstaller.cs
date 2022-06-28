using FourRoads.TelligentCommunity.HubSpot.Resources;
using FourRoads.TelligentCommunity.Installer.Components.Utility;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class HubSpotSqlScriptsInstaller : Installer.SqlScriptsInstaller
    {
        protected override string ProjectName => "HubSpot SQL Installer";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.HubSpot.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}