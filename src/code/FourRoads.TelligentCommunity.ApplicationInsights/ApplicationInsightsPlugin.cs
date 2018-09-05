using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public TelemetryClient TelemetryClient { get; private set; }

        public void Initialize()
        {
            if (_configuration != null)
            {
                TelemetryClient = null;

                bool enabled = _configuration.GetBool("Enabled");

                TelemetryConfiguration.Active.DisableTelemetry = !enabled;

                if (enabled)
                {
                    //var config = TelemetryConfiguration.CreateFromConfiguration(_configuration.GetString("Configuration"));
                    TelemetryConfiguration.Active.InstrumentationKey = _configuration.GetString("InstrumentationKey");

                    TelemetryClient = new TelemetryClient(new TelemetryConfiguration(TelemetryConfiguration.Active.InstrumentationKey));

                    IApplicationInsightsFilter filter = (IApplicationInsightsFilter)TelemetryConfiguration.Active.TelemetryProcessors.FirstOrDefault(
                        p =>
                        {
                            var processor = p as IApplicationInsightsFilter;
                            return processor != null;
                        });

                    if (filter != null)
                    {
                        filter.ExludeSynthetic = _configuration.GetBool("excludeSynthetic");

                        string ignore  =_configuration.GetString("ignorePathsRegex");

                        if (!string.IsNullOrWhiteSpace(ignore))
                        {
                            filter.IgnorePathsRegex = new Regex(ignore , RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.Singleline);
                        }
                        else
                        {
                            filter.IgnorePathsRegex = null;
                        }
                    }
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
                    _pluginGroupLoader = new PluginGroupLoader();
                }

                Type[] priorityPlugins =
                {

                };

                _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor(), priorityPlugins);

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
                optionsGroup.Properties.Add(new Property("excludeSynthetic", "Exclude Synthetic Requests", PropertyType.Bool, 2, Boolean.TrueString));
                optionsGroup.Properties.Add(new Property("ignorePathsRegex", "Ignore Path Regex", PropertyType.String, 1, "socket\\.ashx|/utility/error-notfound\\.aspx"));
                

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