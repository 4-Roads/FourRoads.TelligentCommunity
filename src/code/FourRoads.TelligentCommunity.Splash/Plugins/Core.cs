using System;
using System.Collections.Generic;
using FourRoads.Common;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.RenderingHelper;
using FourRoads.TelligentCommunity.Splash.Interfaces;
using FourRoads.TelligentCommunity.Splash.Logic;
using Ninject.Modules;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Splash.Plugins
{
    public class Core : CQObserverPluginBase, IConfigurablePlugin, INavigable, IBindingsLoader ,ISingletonPlugin
    {
        private ISplashLogic _splashLogic;
        private SplashConfigurationDetails? _splashConfig;

        public override string Name
        {
            get { return "4 Roads - Splash Page"; }
        }

        public override string Description
        {
            get { return "Show a splash page that restricts the site down to a single page that allows users to register an interest in your site"; }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_splashConfig.HasValue)
                SplashLogic.UpdateConfiguration(_splashConfig.Value);
        }

        public void RegisterUrls(IUrlController controller)
        {
            SplashLogic.RegisterUrls(controller);
        }

        public void Update(IPluginConfiguration configuration)
        {
            _splashConfig = new SplashConfigurationDetails()
            {
                RemoveFooter = configuration.GetBool("removeFooter"),
                RemoveHeader = configuration.GetBool("removeHeader"),
                Password = configuration.GetString("password"),
            };
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var group = new PropertyGroup("options", "Options", 0);

                group.Properties.Add(new Property("password", "Password", PropertyType.String, 1, string.Empty));
                group.Properties.Add(new Property("removeHeader", "Remove Header", PropertyType.Bool, 2, bool.TrueString));
                group.Properties.Add(new Property("removeFooter", "Remove Footer", PropertyType.Bool, 3, bool.TrueString));

                return new[] {group};
            }
        }

        protected override ICQProcessor GetProcessor()
        {
            return SplashLogic;
        }

        public override IEnumerable<Type> Plugins
        {
            get
            {
                List<Type> plugins = new List<Type>(base.Plugins);

                plugins.AddRange( new[]
                {
                    typeof (FactoryDefaultPageLayoutInstaller),
                    typeof (FactoryDefaultWidgetProviderInstaller),
                    typeof (ScriptedFragment),
                    typeof (Filestore)
                });

                return plugins;
            }
        }

        public void LoadBindings(NinjectModule module)
        {
            module.Bind<ISplashLogic>().To<SplashLogic>().InSingletonScope();
        }

        public int LoadOrder
        {
            get { return 0; }
        }

        protected internal ISplashLogic SplashLogic
        {
            get
            {
                if (_splashLogic == null)
                {
                    _splashLogic = Injector.Get<ISplashLogic>();
                }
                return _splashLogic;
            }
        }
    }
}
