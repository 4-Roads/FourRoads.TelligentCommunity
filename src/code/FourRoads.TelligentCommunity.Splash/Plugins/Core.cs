using System;
using System.Collections.Generic;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Installer;
using FourRoads.TelligentCommunity.RenderingHelper;
using FourRoads.TelligentCommunity.Splash.Interfaces;
using FourRoads.TelligentCommunity.Splash.Logic;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;

using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;


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
                RemoveFooter = configuration.GetBool("removeFooter").HasValue ? configuration.GetBool("removeFooter").Value : true,
                RemoveHeader = configuration.GetBool("removeHeader").HasValue ? configuration.GetBool("removeHeader").Value : true,
                Password = configuration.GetString("password"),
            };
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var group = new PropertyGroup() {Id = "options", LabelText = "Options"};

                group.Properties.Add(new Property
                {
                    Id = "password",
                    LabelText = "Password",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = ""
                });

                group.Properties.Add(new Property
                {
                    Id = "removeHeader",
                    LabelText = "Remove Header",
                    DataType = "bool",
                    Template = "bool",
                    OrderNumber = 0,
                    DefaultValue = bool.TrueString
                });
                group.Properties.Add(new Property
                {
                    Id = "removeFooter",
                    LabelText = "Remove Footer",
                    DataType = "bool",
                    Template = "bool",
                    OrderNumber = 0,
                    DefaultValue = bool.TrueString
                });

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
                    typeof (FactoryDefaultWidgetProviderInstaller<>),
                    typeof (ScriptedFragment),
                    typeof (Filestore)
                });

                return plugins;
            }
        }

        public void LoadBindings(IContainer module)
        {
            module.Register<ISplashLogic, SplashLogic>(Reuse.Singleton);
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
