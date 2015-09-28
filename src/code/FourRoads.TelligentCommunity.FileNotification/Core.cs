using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.FileNotification.Api.Public.ScriptedContentFragment;
using FourRoads.TelligentCommunity.FileNotification.Extensions;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Plugins;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.FileNotification
{
    public class Core : PluginBase<Configuration>, ICore, IPluginGroup 
    {
        public static Guid FactoryDefaultIdentifierGuid = new Guid("{CDE921ED-63E9-4FDF-B5DD-FE9E49CAD07F}"); 
     
        public Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return FactoryDefaultIdentifierGuid; }
        }

        public override string Description
        {
            get { return "File Notification core plugin. Please do not disable it."; }
        }

        //public override void Initialize()
        //{}

        public override string Name
        {
            get { return "FileNotification - Core"; }
        }


        public IEnumerable<Type> Plugins
        {
            get
            {
                return new[]
                           {
                               // Modules
                               // Plugins
                               // Scripted Fragments
                               typeof(FileSubscriptionScriptedContentFragmentExtension)
                           };
            }
        }

       
    }
}
