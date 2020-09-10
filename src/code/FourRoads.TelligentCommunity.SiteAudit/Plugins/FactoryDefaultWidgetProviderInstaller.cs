using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using System;
using System.Runtime.CompilerServices;

namespace FourRoads.TelligentCommunity.SiteAudit.Plugins
{
    public class CommunityCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstallerV2
    {
        public static Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{cc29f4ef-f6f7-4d54-a56a-9a69068fddff}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => _scriptedContentFragmentFactoryDefaultIdentifier;

        protected override string ProjectName => "Site Audit";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.SiteAudit.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();

        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }
    }
}
