using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.ForumThreadViews.Resources
{
    public class SqlScriptsInstaller : FourRoads.Common.TelligentCommunity.Plugins.Base.SqlScriptsInstaller
    {
        protected override string ProjectName
        {
            get { return "Forum Thread Views"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.ForumThreadViews.Resources."; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return new EmbeddedResources(); }
        }
    }
}
