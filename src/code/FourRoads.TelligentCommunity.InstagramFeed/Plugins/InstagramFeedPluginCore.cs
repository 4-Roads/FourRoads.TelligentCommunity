using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.InstagramFeed.Interfaces;
using FourRoads.TelligentCommunity.InstagramFeed.Logic;
using FourRoads.TelligentCommunity.InstagramFeed.Extensions;
using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using Telligent.DynamicConfiguration.Components;

namespace FourRoads.TelligentCommunity.InstagramFeed.Plugins
{
    public class InstagramFeedPluginCore : IPluginGroup, IBindingsLoader, INavigable, IInstagramFeedPlugin, IConfigurablePlugin
    {
        private IMediaSearch _mediaSearchLogic;

        protected internal IMediaSearch MediaSearchLogic
        {
            get
            {
                if (_mediaSearchLogic == null)
                {
                    _mediaSearchLogic = Injector.Get<IMediaSearch>();
                }
                return _mediaSearchLogic;
            }
        }

        public void LoadBindings(IContainer container)
        {
            container.Register<IMediaSearch, MediaSearch>(Reuse.Singleton);
        }

        public void Initialize()
        {
            
        }

        public string Name => "4 Roads - Instagram Feed";

        public string Description => "Plugin for adding Instagram Feed using Instagram Graph API";

        private IPluginConfiguration _configuration;
        private IPluginConfiguration Configuration
        {
            get { return _configuration; }
        }

        public void RegisterUrls(IUrlController controller)
        {
            MediaSearchLogic.RegisterUrls(controller);
        }

        public IEnumerable<Type> Plugins => new[]
        {
            typeof (DependencyInjectionPlugin),
            typeof (FactoryDefaultWidgetProviderInstaller),
            typeof (InstagramGraphExtension)
        };

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        
        public string AppId
        {
            get
            {
                return Configuration.GetString("AppId");
            }
        }

        public string AppSecret
        {
            get
            {
                return Configuration.GetString("AppSecret");
            }
        }

        public int CacheMinutes
        {
            get
            {
                return Configuration.GetInt("CacheMinutes");
            }
        }

        public int LoadOrder => 0;

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup optionsGroup = new PropertyGroup("settings", "Settings", 0);

                optionsGroup.Properties.Add(new Property("AppId", "App Id", PropertyType.String, 0, ""));
                optionsGroup.Properties.Add(new Property("AppSecret", "App Secret", PropertyType.String, 1, ""));
                optionsGroup.Properties.Add(new Property("CacheMinutes", "Cache Minutes (Default: 0 - None)", PropertyType.Int, 2, "0"));

                return new[] { optionsGroup };
            }
        }
    }
}
