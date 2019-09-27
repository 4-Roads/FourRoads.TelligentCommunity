using System;
using System.Runtime.CompilerServices;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.InlineContent.ScriptedContentFragments;

namespace FourRoads.TelligentCommunity.InlineContent
{
    internal class InternalCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class InlinePanelInstaller : FactoryDefaultWidgetProviderInstallerV3<InlineContentPanel>
    {
        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => InlineContentPanel._scriptedFragmentGuid;
        protected override string ProjectName => "Meta Data";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.InlineContent.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
        protected override ICallerPathVistor CallerPath()
        {
            return new InternalCallerPath();
        }
    }
}