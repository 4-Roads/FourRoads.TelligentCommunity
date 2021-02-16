using System;
using System.Runtime.CompilerServices;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Interfaces;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.ExtendedSearch
{
    public class CommunityCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class DefaultWidgetInstaller : FactoryDefaultWidgetProviderInstaller<DefaultWidgetInstaller>, IScriptedContentFragmentFactoryDefaultProvider
    {
        public static Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{D9589449-A65F-4477-A67F-6E25F525E25F}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => _scriptedContentFragmentFactoryDefaultIdentifier;

        protected override string ProjectName => "Search Suggestion";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.ExtendedSearch.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();

        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }
    }
}