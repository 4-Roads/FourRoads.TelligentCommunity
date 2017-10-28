using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.MetaData.Interfaces;

namespace FourRoads.TelligentCommunity.MetaData
{
    public class ScriptableInstaller : Common.TelligentCommunity.Plugins.Base.ScriptableInstaller, IApplicationPlugin
    {
        protected override string ProjectName => "Meta Data";

        protected override string BaseResourcePath { get; } = "FourRoads.TelligentCommunity.MetaData.Resources.";

        protected override EmbeddedResourcesBase EmbeddedResources => new EmbeddedResources();
    }
}