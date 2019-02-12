using System;
using System.Runtime.CompilerServices;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.TelligentCommunity.GoogleMfa.Resources;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Plugins
{
    public class CommunityCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstallerV2
    {
        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier { get; } = new Guid("{3BD55B78-2E62-4B77-B6E7-30F6BCE17DE2}");

        protected override string ProjectName => "4 Roads - Google MFA";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.GoogleMfa.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }
    }
}
