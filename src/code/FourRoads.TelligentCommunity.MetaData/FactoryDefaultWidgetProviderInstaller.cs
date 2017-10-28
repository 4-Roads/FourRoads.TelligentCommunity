using System;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.MetaData
{
    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstaller, IApplicationPlugin
    {
        public static Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{2584523C-F405-4159-A205-5322F5957F27}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => _scriptedContentFragmentFactoryDefaultIdentifier;

        protected override string ProjectName => "Meta Data";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.MetaData.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}
