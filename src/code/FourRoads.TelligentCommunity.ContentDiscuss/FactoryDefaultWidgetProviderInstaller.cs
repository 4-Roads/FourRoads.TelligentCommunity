using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.ContentDiscuss
{
    public class FactoryDefaultWidgetProviderInstaller : Common.TelligentCommunity.Plugins.Base.FactoryDefaultWidgetProviderInstaller, IApplicationPlugin, IInstallablePlugin
    {
        private readonly Guid _scriptedContentFragmentFactoryDefaultIdentifier = new Guid("{84CC3E33-4337-4212-9E4A-3700EE06F721}");

        public override Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return _scriptedContentFragmentFactoryDefaultIdentifier; }
        }

        protected override string ProjectName
        {
            get { return "Content Discussions"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.ContentDiscuss.Resources."; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return new EmbeddedResources(); }
        }

        void IInstallablePlugin.Install(Version lastInstalledVersion)
        {
            base.Install(lastInstalledVersion);

            foreach(var theme in Themes.List(ThemeTypes.Weblog))
            {
                if (ThemePages.FactoryDefaultExists(theme, "post", false))
                {
                    var fragments = ThemePageContentFragments.ListFactoryDefault(theme , "post" , false);

                    var findFrag = GetScriptedFragmentName("66335d7ac5c841429709a730aec55ac9");

                    if (!fragments.Any(f => f.ContentFragmentType == findFrag))
                    {
                        ThemePageContentFragments.InsertInFactoryDefault(theme, "post", false, GetScriptedFragmentName("aa55795b63a949718dea4032197f3507"), ContentFragmentPlacement.After, findFrag, "fragmentHeader=%24%7Bresource%3AContentDiscuss_Header%7D&amp;textAreaSelector=.blog-post%20.post-content.user-defined-markup", "full-border with-header");
                    }
                }
            }
        }

        const string _leaderAssemblyName = "Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.ScriptedContentFragments::{0}";

        protected string GetScriptedFragmentName(string id)
        {
            return string.Format(_leaderAssemblyName, id);
        }
    }
}
