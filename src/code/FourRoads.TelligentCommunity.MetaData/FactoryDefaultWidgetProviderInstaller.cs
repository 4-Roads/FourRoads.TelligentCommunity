using System;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.MetaData.Interfaces;

namespace FourRoads.TelligentCommunity.MetaData
{
    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstaller, IApplicationPlugin
    {
        public static Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{2584523C-F405-4159-A205-5322F5957F27}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return _scriptedContentFragmentFactoryDefaultIdentifier; }
        }

        protected override string ProjectName
        {
            get { return "Meta Data"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.MetaData.Resources."; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return new EmbeddedResources(); }
        }
    }
}
