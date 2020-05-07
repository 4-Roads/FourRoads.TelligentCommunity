using System;
using System.Runtime.CompilerServices;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Interfaces;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions
{
    internal class InternalCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }

    public class FactoryDefaultWidgetProviderInstaller : FactoryDefaultWidgetProviderInstallerV3<FactoryDefaultWidgetProviderInstaller>, IApplicationPlugin, IScriptedContentFragmentFactoryDefaultProvider
    {
        private readonly Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("2e58b526724841b19a9908764342c024");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return _scriptedContentFragmentFactoryDefaultIdentifier; }
        }

        protected override string ProjectName
        {
            get { return "Configuration Extensions"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.ConfigurationExtensions.Resources."; }
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
