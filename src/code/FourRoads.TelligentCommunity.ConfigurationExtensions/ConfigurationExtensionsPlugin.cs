using System;
using System.Collections.Generic;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Interfaces;
using Ninject.Modules;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions
{
    public class ConfigurationExtensionsPlugin : IConfigurationExtensionsPlugin
    {
        private PluginGroupLoader _pluginGroupLoader;

        public void Initialize()
        {
  
        }

        public string Name
        {
            get { return "4 Roads - Configuration Extensions"; }
        }

        public string Description
        {
            get { return "This plugin provides configuration extensions so a user is able to specify advanced default options for individual areas of the site"; }
        }

        public void LoadBindings(NinjectModule module)
        {
            
        }

        public int LoadOrder {
            get { return 0; }
        }


        private class PluginGroupLoaderTypeVisitor : FourRoads.Common.TelligentCommunity.Plugins.Base.PluginGroupLoaderTypeVisitor
        {
            public override Type GetPluginType()
            {
                return typeof(IApplicationPlugin);
            }
        }

        public IEnumerable<Type> Plugins
        {
            get
            {
                if (_pluginGroupLoader == null)
                {
                    _pluginGroupLoader = new PluginGroupLoader();

                    _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor());
                }

                return _pluginGroupLoader.GetPlugins();
            }
        }
    }
}
