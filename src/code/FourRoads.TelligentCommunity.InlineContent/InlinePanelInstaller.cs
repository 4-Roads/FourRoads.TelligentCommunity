using System;
using System.Runtime.CompilerServices;
using FourRoads.TelligentCommunity.InlineContent.ScriptedContentFragments;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Interfaces;
using FourRoads.TelligentCommunity.Installer.Components.Utility;

namespace FourRoads.TelligentCommunity.InlineContent
{
    internal class InternalCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class InlinePanelInstaller : FactoryDefaultWidgetProviderInstaller<InlineContentPanel>
    {
        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => InlineContentPanel._scriptedFragmentGuid;
        protected override string ProjectName => "Inline Content Panel";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.InlineContent.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
        protected override ICallerPathVistor CallerPath()
        {
            return new InternalCallerPath();
        }
    }
}