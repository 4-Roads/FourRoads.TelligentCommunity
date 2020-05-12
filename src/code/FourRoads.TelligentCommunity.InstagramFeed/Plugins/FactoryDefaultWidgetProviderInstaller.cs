using System;
using System.Runtime.CompilerServices;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.TelligentCommunity.InstagramFeed.Resources;

namespace FourRoads.TelligentCommunity.InstagramFeed.Plugins
{
    public class CommunityCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstallerV2
    {
        public static Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{340f00f8-8a92-449a-b952-aa6e15a44620}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => _scriptedContentFragmentFactoryDefaultIdentifier;

        protected override string ProjectName => "4 Roads - Instagram Feed";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.InstagramFeed.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();

        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }
    }
}