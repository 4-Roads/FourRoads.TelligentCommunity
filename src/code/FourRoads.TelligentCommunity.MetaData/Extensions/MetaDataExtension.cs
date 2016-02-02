using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using FourRoads.Common;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using Ninject.Modules;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.MetaData.Extensions
{
    public class MetaDataExtension : IScriptedContentFragmentExtension, IApplicationPlugin
    {
        public void Initialize()
        {
 
        }

        public string Name
        {
            get { return "Meta Data Scripted Fragment"; }
        }

        public string Description
        {
            get { return "This plugin allows exposes the extension for Meta Data"; }
        }

        public string ExtensionName {
            get { return "frcommon_v1_metaData"; }
        }

        public object Extension
        {
            get
            {
                return Injector.Get<IMetaDataScriptedFragment>(); 
            }
        }
    }
}
