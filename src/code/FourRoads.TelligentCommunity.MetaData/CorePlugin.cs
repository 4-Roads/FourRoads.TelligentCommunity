using System;
using System.Collections.Generic;
using System.Linq;
using FourRoads.Common;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using FourRoads.TelligentCommunity.MetaData.Logic;
using FourRoads.TelligentCommunity.MetaData.ScriptedFragmentss;
using FourRoads.TelligentCommunity.RenderingHelper;
using Ninject.Modules;
using Telligent.DynamicConfiguration.Components;
using Telligent.DynamicConfiguration.Controls;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.MetaData
{
    public class CorePlugin : CQObserverPluginBase, IBindingsLoader, IPluginGroup,IConfigurablePlugin
    {
        private IMetaDataLogic _metaDataLogic;
        private PluginGroupLoader _pluginGroupLoader;
        private MetaDataConfiguration _metaConfig = null;

        public override void Initialize()
        {
            base.Initialize();
            if (_metaConfig != null)
            {
                MetaDataLogic.UpdateConfiguration(_metaConfig);
            }
        }

        protected override ICQProcessor GetProcessor()
        {
            return (ICQProcessor) MetaDataLogic;
        }

        protected internal IMetaDataLogic MetaDataLogic
        {
            get
            {
                if (_metaDataLogic == null)
                {
                    _metaDataLogic = Injector.Get<IMetaDataLogic>();
                }
                return _metaDataLogic;
            }
        }

        public override string Name
        {
            get { return "4 Roads - MetaData Plugin"; }
        }

        public override string Description
        {
            get { return "This plugin allows a user to specify metadata overrides for any specific page in the site"; }
        }

        public int LoadOrder
        {
            get { return 0; }
        }

        public void LoadBindings(NinjectModule module)
        {
            module.Bind<IMetaDataLogic>().To<MetaDataLogic>().InSingletonScope();
            module.Bind<IMetaDataScriptedFragment>().To<MetaDataScriptedFragment>().InSingletonScope();
        }

        private class PluginGroupLoaderTypeVisitor : Common.TelligentCommunity.Plugins.Base.PluginGroupLoaderTypeVisitor
        {
            public override Type GetPluginType()
            {
                return typeof(IApplicationPlugin);
            }
        }

        public override IEnumerable<Type> Plugins
        {
            get
            {
                if (_pluginGroupLoader == null)
                {
                    Type[] priorityPlugins =
                    {
                        typeof (DependencyInjectionPlugin),
                        typeof (FactoryDefaultWidgetProviderInstaller),
                        typeof (RenderingObserverPlugin)
                    };

                    _pluginGroupLoader = new PluginGroupLoader();

                    _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor(), priorityPlugins);
                }

                return _pluginGroupLoader.GetPlugins();
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            if (_metaConfig == null)
            {
                _metaConfig = new MetaDataConfiguration();
            }

            _metaConfig.ExtendedEntries = configuration.GetString("extendedtags").Split(new[] {','} , StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            _metaConfig.GoogleTagHead = configuration.GetString("gtmHeadTag").Trim();
            _metaConfig.GoogleTagBody = configuration.GetString("gtmBodyTag").Trim();
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var propertyGroupArray = new PropertyGroup[]
                {
                    new PropertyGroup("options", "Options", 0)
                };

                var property1 = new Property("extendedtags", "Additional Tags", PropertyType.Custom, 0, "og:title,og:type,og:image,og:url,og:description,fb:admins,twitter:card,twitter:url,twitter:title,twitter:description,twitter:image");
                property1.DescriptionText = "Provide a list of comma seperated meta tags that will be available to the end user to configure";
                property1.ControlType = typeof (MultilineStringControl);

                propertyGroupArray[0].Properties.Add(property1);

                property1 = new Property("gtmHeadTag", "GTM Head tag", PropertyType.Custom, 1, "");
                property1.DescriptionText = "Google tag manager head tag";
                property1.ControlType = typeof(MultilineStringControl);
                propertyGroupArray[0].Properties.Add(property1);

                property1 = new Property("gtmBodyTag", "GTM Body tag", PropertyType.Custom, 2, "");
                property1.DescriptionText = "Google tag manager body tag";
                property1.ControlType = typeof(MultilineStringControl);
                propertyGroupArray[0].Properties.Add(property1);

                return propertyGroupArray;


            }
        }
    }
}
