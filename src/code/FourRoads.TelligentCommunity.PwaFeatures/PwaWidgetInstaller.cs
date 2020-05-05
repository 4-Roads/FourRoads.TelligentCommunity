using System;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.PwaFeatures.Resources;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.PwaFeatures
{
    public class PwaWidgetInstaller : FactoryDefaultWidgetProviderInstallerV3<WidgetScriptedFragmentPlugin>, IPluginGroup
    {
        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => WidgetScriptedFragmentPlugin.Id;
        protected override string ProjectName => "4 Roads PWA Features";
        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.PwaFeatures.Resources.";
        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
        public IEnumerable<Type> Plugins => new[] { typeof(WidgetScriptedFragmentPlugin) };
    }
}