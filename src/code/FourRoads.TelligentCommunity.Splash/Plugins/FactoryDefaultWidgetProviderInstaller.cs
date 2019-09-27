using System;
using System.Runtime.CompilerServices;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Splash.Plugins
{
    internal class InternalCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class FactoryDefaultWidgetProviderInstaller : FactoryDefaultWidgetProviderInstallerV3<FactoryDefaultWidgetProviderInstaller>, IInstallablePlugin, IScriptedContentFragmentFactoryDefaultProvider
    {
        private readonly Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{D6456600-9937-4927-8F04-32CD79F0052B}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return _scriptedContentFragmentFactoryDefaultIdentifier; }
        }

        protected override string ProjectName
        {
            get { return "Splash"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.Splash.Resources."; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return new EmbeddedResources(); }
        }
        protected override ICallerPathVistor CallerPath()
        {
            return new InternalCallerPath();
        }
    }
}
