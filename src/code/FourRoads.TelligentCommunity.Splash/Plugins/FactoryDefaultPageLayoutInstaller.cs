using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.Splash.Plugins
{
    public class FactoryDefaultPageLayoutInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultThemeInstaller
    {
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
    }
}