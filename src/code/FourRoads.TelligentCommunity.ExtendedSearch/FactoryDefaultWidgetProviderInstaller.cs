using System;
using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.ExtendedSearch
{
    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstaller
    {
        public static Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{D9589449-A65F-4477-A67F-6E25F525E25F}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => _scriptedContentFragmentFactoryDefaultIdentifier;

        protected override string ProjectName => "Search Suggestion";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.ExtendedSearch.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}