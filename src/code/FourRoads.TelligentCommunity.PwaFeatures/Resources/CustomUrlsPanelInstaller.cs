using System;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.PwaFeatures.Plugins;

namespace FourRoads.TelligentCommunity.PwaFeatures.Resources
{
    public class CustomUrlsPanelInstaller : FactoryDefaultWidgetProviderInstallerV3<PwaFeaturesPlugin>
    {
        protected override ICallerPathVistor CallerPath()
        {
            return new CommunityCallerPath();
        }

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => new Guid("1fe74a21eab446279f261d167bd86d0a");
        protected override string ProjectName => "4 Roads PWA SW Features";
        protected override string BaseResourcePath => "FourRoads.TelligentCommunity.PwaFeatures.Resources.";
        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}