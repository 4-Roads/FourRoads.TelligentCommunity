// //  ------------------------------------------------------------------------------
// //  <copyright company="Four Roads LLC">
// //  Copyright (c) Four Roads LLC.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using FourRoads.Common.TelligentCommunity.Plugins.HttpModules;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    /// <summary>
    ///   Plugin base class that loads Four Roads dependency injection correctly
    /// </summary>
    public abstract class PluginBase<T> : IPlugin where T : Settings<T>, new()
    {
        protected PluginBase()
        {
            Debug.Assert(Settings<T>.Instance() != null);

            Injector.LoadBindingsFromSettings(Settings<T>.Instance());
        }

        #region IPlugin Members

        public abstract string Description { get; }

        public virtual void Initialize() {}

        public abstract string Name { get; }

        #endregion
    }

    public class DependencyInjectionPlugin : PluginBase<DefaultIocHandler> , ISingletonPlugin
    {
        public override string Name
        {
            get { return "4-Roads DI"; }
        }

        public override string Description
        {
            get { return "This plugin ensures that dependency injection has been initialized"; }
        }
    }
}