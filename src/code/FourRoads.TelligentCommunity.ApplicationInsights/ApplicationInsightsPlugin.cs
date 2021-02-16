using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;

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

                bool enabled = _configuration.GetBool("Enabled").HasValue ? _configuration.GetBool("Enabled").Value : false;

                TelemetryConfiguration.Active.DisableTelemetry = !enabled;

                if (enabled)
                {
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
                        filter.ExludeSynthetic = _configuration.GetBool("excludeSynthetic").HasValue
                            ? _configuration.GetBool("excludeSynthetic").Value
                            : true;

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
                PropertyGroup optionsGroup = new PropertyGroup() {Id="Options", LabelText = "Options"};
                groupArray[0] = optionsGroup;

                optionsGroup.Properties.Add(new Property
                {
                    Id = "InstrumentationKey",
                    LabelText = "Instrumentation Key",
                    DataType = "string",
                    Template = "string",
                    DefaultValue =  ""
                });

                optionsGroup.Properties.Add(new Property {
                        Id ="excludeSynthetic",
                        LabelText = "Exclude Synthetic Requests",
                        DataType = "bool", 
                        Template = "bool", 
                        DefaultValue = Boolean.TrueString
                });

                optionsGroup.Properties.Add(new Property {
                    Id="ignorePathsRegex",
                    LabelText = "Ignore Path Regex",
                    DataType = "string",
                    Template = "string",
                    DefaultValue = "socket\\.ashx|/utility/error-notfound\\.aspx"
                });
                
                optionsGroup.Properties.Add(new Property()
                {
                    Id="Enabled",
                    LabelText = "Enabled",
                    DataType = "bool" ,
                    Template = "bool",
                    DefaultValue = Boolean.FalseString
                });

                return groupArray;
            }
        }
    }

}