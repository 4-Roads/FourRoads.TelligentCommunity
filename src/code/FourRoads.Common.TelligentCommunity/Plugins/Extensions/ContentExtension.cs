// //  ------------------------------------------------------------------------------
// //  <copyright company="Four Roads LLC">
// //  Copyright (c) Four Roads LLC.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Components.Logic;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.ScriptedContentFragments;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Extensions
{
    public class ContentExtension : IScriptedContentFragmentExtension  , IBindingsLoader , IPluginGroup
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

        public void LoadBindings(IContainer container)
        {
            container.Register<IContentLogic, ContentLogic>(Reuse.Singleton);
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

        public IEnumerable<Type> Plugins
        {
            get { return new[] {typeof(DependencyInjectionPlugin)}; }
        }
    }
}