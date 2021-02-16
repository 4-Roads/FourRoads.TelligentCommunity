using System;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.Installer.Components.Interfaces;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using FourRoads.TelligentCommunity.MetaData.ScriptedFragmentss;

namespace FourRoads.TelligentCommunity.MetaData
{
    public class ScriptableInstaller : FactoryDefaultWidgetProviderInstaller<AdministrationPanel>, IApplicationPlugin
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