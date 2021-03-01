using System;
using System.Collections.Generic;
using DryIoc;
using Telligent.Evolution.Extensibility.Version1;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Sentrus.Logic;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Sentrus.Jobs;
using FourRoads.TelligentCommunity.Sentrus.Controls;

namespace FourRoads.TelligentCommunity.Sentrus
{

    public class HealthPlugin : IHealthPlugin, IInstallablePlugin, IBindingsLoader
    {
        private PluginGroupLoader _pluginGroupLoader;

        public void LoadBindings(IContainer module)
        {
            module.Register<IUserHealth, UserHealth>();
        }

        public int LoadOrder
        {
            get { return 100; }
        }

        public void Initialize()
        {

        }

        public string Name
        {
            get { return "4 Roads - Sentrus"; }
        }

        public string Description
        {
            get
            {
                return "This plugin provides several features to help you keep your Zimbra Community site running at peak performance.";
            }
        }


        private class PluginGroupLoaderTypeVisitor : FourRoads.Common.TelligentCommunity.Plugins.Base.PluginGroupLoaderTypeVisitor
        {
            public override Type GetPluginType()
            {
                return typeof(IHealthExtension);
            }
        }

        public IEnumerable<Type> Plugins
        {
            get
            {
                if (_pluginGroupLoader == null)
                {
                    _pluginGroupLoader = new PluginGroupLoader();
                }

                Type[] priorityPlugins =
                {
                    typeof(DependencyInjectionPlugin),
                    typeof(HealthJob),
                    typeof(InactiveUserManagementPropertyTemplate),
                    typeof(TestSettingsPropertyTemplate),
                    typeof(Resources.SqlScriptsInstaller)
                };

                _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor(), priorityPlugins);

                return _pluginGroupLoader.GetPlugins();
            }
        }


        public void Install(Version lastInstalledVersion)
        {

        }

        private static void WriteEmailFile(string resource)
        {
  
        }

        public void Uninstall()
        {
        }

        public Version Version
        {
            get { return new Version("1.0.0.0"); }
        }
    }
}