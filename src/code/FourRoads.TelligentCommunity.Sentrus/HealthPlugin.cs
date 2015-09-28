using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using Ninject.Modules;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Sentrus.Logic;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Sentrus.Resources;
using FourRoads.TelligentCommunity.Sentrus.Jobs;

namespace FourRoads.TelligentCommunity.Sentrus
{

    public class HealthPlugin : IHealthPlugin, IInstallablePlugin, IBindingsLoader
    {
        private PluginGroupLoader _pluginGroupLoader;

        public void LoadBindings(NinjectModule module)
        {
            module.Bind<IUserHealth>().To<UserHealth>();
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
                    Type[] priorityPlugins =
                    {
                        typeof(DependencyInjectionPlugin),
                        typeof(HealthJob),
                         typeof(Resources.SqlScriptsInstaller)
                    };

                    _pluginGroupLoader = new PluginGroupLoader();

                    _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor(), priorityPlugins);
                }

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