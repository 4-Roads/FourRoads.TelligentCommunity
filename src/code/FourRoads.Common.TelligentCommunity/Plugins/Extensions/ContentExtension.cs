// //  ------------------------------------------------------------------------------
// //  <copyright company="Four Roads LLC">
// //  Copyright (c) Four Roads LLC.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Components.Logic;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.ScriptedContentFragments;
using Ninject.Modules;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Extensions
{
    public class ContentExtension : IScriptedContentFragmentExtension  , IBindingsLoader
    {
        #region IScriptedContentFragmentExtension Members

        public object Extension
        {
			get { return Injector.Get<ContentScriptedContentFragment>(); }
        }

        public string ExtensionName
        {
            get { return "frcommon_v1_content"; }
        }

        public string Description
        {
            get { return "Enables scripted content fragments to access and manipulate extended information for content items"; }
        }

        public void LoadBindings(NinjectModule module)
        {
            module.Rebind<IContentLogic>().To<ContentLogic>().InSingletonScope();
        }

        public int LoadOrder {
            get { return 0; }
        }

        public void Initialize() {}

        public string Name
        {
            get { return string.Format("4 Roads - Content NVelocity Extension ({0})", ExtensionName); }
        }

        #endregion
    }
}