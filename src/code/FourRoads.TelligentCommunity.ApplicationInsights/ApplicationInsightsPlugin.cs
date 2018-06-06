using System;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ApplicationInsights
{
    public class ApplicationInsightsPlugin : IApplicationInsightsPlugin, IConfigurablePlugin
    {
        private PluginGroupLoader _pluginGroupLoader;
        private IPluginConfiguration _configuration;
        private TelemetryClient _tc;

        public TelemetryClient TelemetryClient => _tc;

        public void Initialize()
        {
            if (_configuration != null)
            {
                _tc = null;

                bool enabled = _configuration.GetBool("Enabled");

                TelemetryConfiguration.Active.DisableTelemetry = !enabled;

                if (enabled)
                {
                    //var config = TelemetryConfiguration.CreateFromConfiguration(_configuration.GetString("Configuration"));
                    TelemetryConfiguration.Active.InstrumentationKey = _configuration.GetString("InstrumentationKey");

                    _tc = new TelemetryClient();
                }
            }
        }

        public string Name => "4 Roads - Application Insights";

        public string Description => "This plugin provides a configuration wrapper to Application Insights and also adds logging for key events.";

        private class PluginGroupLoaderTypeVisitor : FourRoads.Common.TelligentCommunity.Plugins.Base.PluginGroupLoaderTypeVisitor
        {
            public override Type GetPluginType()
            {
                return typeof(IApplicationInsightsApplication);
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
                       
                    };

                    _pluginGroupLoader = new PluginGroupLoader();

                    _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor(), priorityPlugins);
                }

                return _pluginGroupLoader.GetPlugins();
            }
        }
        
        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup[] groupArray = new PropertyGroup[1];
                PropertyGroup optionsGroup = new PropertyGroup("Options", "Options", 0);
                groupArray[0] = optionsGroup;

                optionsGroup.Properties.Add(new Property("InstrumentationKey", "Instrumentation Key", PropertyType.String, 1, ""));

                //var configuration = new Property("Configuration", "ApplicationInsights.Config", PropertyType.String, 1, new EmbededResources().GetString("FourRoads.TelligentCommunity.ApplicationInsights.ApplicationInsights.config"));

                //configuration.ControlType = typeof(MultilineStringControl);
                //configuration.Attributes["rows"] = "150";
                //configuration.Attributes["cols"] = "100";

                //optionsGroup.Properties.Add(configuration);

                optionsGroup.Properties.Add(new Property("Enabled", "Enabled", PropertyType.Bool, 0, "True"));

                return groupArray;
            }
        }
    }

}