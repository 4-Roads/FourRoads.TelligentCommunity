using System;
using System.Runtime.CompilerServices;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MetaData
{
    internal class InternalCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class FactoryDefaultWidgetProviderInstaller : FactoryDefaultWidgetProviderInstallerV3<FactoryDefaultWidgetProviderInstaller>, IScriptedContentFragmentFactoryDefaultProvider, IApplicationPlugin
    {
        public static Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{2584523C-F405-4159-A205-5322F5957F27}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => _scriptedContentFragmentFactoryDefaultIdentifier;

        protected override string ProjectName => "Meta Data";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.MetaData.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
        protected override ICallerPathVistor CallerPath()
        {
            return new InternalCallerPath();
        }
    }
}
