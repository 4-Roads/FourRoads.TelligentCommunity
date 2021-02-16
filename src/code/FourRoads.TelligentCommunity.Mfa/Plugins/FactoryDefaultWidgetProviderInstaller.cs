using System;
using System.Runtime.CompilerServices;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Interfaces;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using FourRoads.TelligentCommunity.Mfa.Resources;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class CommunityCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class DefaultWidgetInstaller : FactoryDefaultWidgetProviderInstaller<DefaultWidgetInstaller>, IScriptedContentFragmentFactoryDefaultProvider
    {
        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier { get; } = new Guid("{3BD55B78-2E62-4B77-B6E7-30F6BCE17DE2}");

        protected override string ProjectName => "MFA Widget Installer";

        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.Mfa.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }
    }
}
