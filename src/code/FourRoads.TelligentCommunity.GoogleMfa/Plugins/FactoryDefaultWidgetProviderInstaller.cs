using System;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.GoogleMfa.Resources;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Plugins
{
    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstaller, IInstallablePlugin
    {
        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier { get; } = new Guid("{3BD55B78-2E62-4B77-B6E7-30F6BCE17DE2}");

        protected override string ProjectName => "Google MFA";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.GoogleMfa.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}
