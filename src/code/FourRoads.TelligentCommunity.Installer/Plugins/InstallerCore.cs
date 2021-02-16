using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Installer.Plugins
{
    public class InstallerCore : IPluginGroup
    {
        public string Name => "Widget Installer - Core";

        public string Description => "Container for all the widget installer functionality";

        public void Initialize()
        {
        }

        public IEnumerable<Type> Plugins
        {
            get
            {
                Type[] plugins =
                {
                        typeof (TriggerActionPropertyTemplate)
                };

                return plugins;
            }
        }
    }
}
