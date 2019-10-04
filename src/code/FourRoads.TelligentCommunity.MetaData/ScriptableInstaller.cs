using System;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using FourRoads.TelligentCommunity.MetaData.ScriptedFragmentss;

namespace FourRoads.TelligentCommunity.MetaData
{
    public class ScriptableInstaller : FactoryDefaultWidgetProviderInstallerV3<AdministrationPanel>, IApplicationPlugin
    {
        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier => AdministrationPanel._scriptedFragmentGuid;
        protected override string ProjectName => "Meta Data Scripted Panel";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.MetaData.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
        protected override ICallerPathVistor CallerPath()
        {
            return new InternalCallerPath();
        }
    }
}