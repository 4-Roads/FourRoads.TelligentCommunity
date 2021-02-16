using System;
using System.Collections.Generic;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Interfaces;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using FourRoads.TelligentCommunity.PwaFeatures.Plugins;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.PwaFeatures.Resources
{
    public class DefaultWidgetInstaller : FactoryDefaultWidgetProviderInstaller<WidgetScriptedFragmentPlugin>, IPluginGroup
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