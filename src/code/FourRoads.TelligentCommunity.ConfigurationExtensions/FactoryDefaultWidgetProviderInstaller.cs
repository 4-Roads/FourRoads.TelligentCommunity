using System;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Interfaces;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions
{
    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstaller, IApplicationPlugin, IInstallablePlugin
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
    }
}
