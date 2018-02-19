using System;
using System.Collections.Generic;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ApplicationInsights
{
    public class ApplicationInsightsPlugin : IApplicationInsightsPlugin, IInstallablePlugin, IBindingsLoader, IConfigurablePlugin, ISingletonPlugin
    {
        private PluginGroupLoader _pluginGroupLoader;
        private IPluginConfiguration _configuration;
        private TelemetryClient _tc;

        public TelemetryClient TelemetryClient => _tc;

        public void LoadBindings(IContainer module)
        {
            //module.Register<IUserHealth, UserHealth>();
        }

        public int LoadOrder => 100;

        public void Initialize()
        {
            if (_configuration != null)
            {
                TelemetryConfiguration.Active.InstrumentationKey = _configuration.GetString("InstrumentationKey");
                TelemetryConfiguration.Active.DisableTelemetry = !_configuration.GetBool("Enabled");

                _tc = new TelemetryClient();

                if (_configuration.GetBool("Enabled"))
                {
                    var user = Apis.Get<IUsers>();

                    user.Events.AfterLockout += EventsOnAfterLockout;
                    user.Events.AfterCreate += EventsOnAfterCreate;
                    user.Events.BeforeDelete += EventsOnBeforeDelete;
                    user.Events.AfterAuthenticate += EventsOnAfterAuthenticate;
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
                        typeof(DependencyInjectionPlugin)
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

        private void EventsOnAfterAuthenticate(UserAfterAuthenticateEventArgs userAfterAuthenticateEventArgs)
        {
            try
            {
                _tc.TrackEvent("TelligentUsersOnAfterAuthenticate", new Dictionary<string, string>
                {
                    {"UserId" , userAfterAuthenticateEventArgs.Id.ToString()},
                    {"UserName" , userAfterAuthenticateEventArgs.Username},
                    {"UserEmail" , userAfterAuthenticateEventArgs.PrivateEmail},
                });
            }
            catch (Exception e)
            {
                Apis.Get<IEventLog>().Write("Application Inisights Failed: " + e, new EventLogEntryWriteOptions() { Category = "Logging", EventType = "Error" });
            }
        }

        private void EventsOnBeforeDelete(UserBeforeDeleteEventArgs userBeforeDeleteEventArgs)
        {
            try
            {
                _tc.TrackEvent("TelligentUserOnBeforeDelete" , new Dictionary<string, string>
                {
                    {"UserId" , userBeforeDeleteEventArgs.Id.ToString()},
                    {"UserName" , userBeforeDeleteEventArgs.Username},
                    {"UserEmail" , userBeforeDeleteEventArgs.PrivateEmail},
                    {"Reassing UserId" , userBeforeDeleteEventArgs.ReassignedUserId.ToString()}
                });
            }
            catch (Exception e)
            {
                Apis.Get<IEventLog>().Write("Application Inisights Failed: " + e, new EventLogEntryWriteOptions() {Category = "Logging", EventType = "Error"});
            }
        }

        private void EventsOnAfterCreate(UserAfterCreateEventArgs userAfterCreateEventArgs)
        {
            try
            {
                _tc.TrackEvent("TelligentUserOnAfterCreate", new Dictionary<string, string>
                {
                    {"UserId" , userAfterCreateEventArgs.Id.ToString()},
                    {"UserName" , userAfterCreateEventArgs.Username},
                    {"UserEmail" , userAfterCreateEventArgs.PrivateEmail},
                });
            }
            catch (Exception e)
            {
                Apis.Get<IEventLog>().Write("Application Inisights Failed: " + e, new EventLogEntryWriteOptions() { Category = "Logging", EventType = "Error" });
            }
        }

        private void EventsOnAfterLockout(UserAfterLockoutEventArgs userAfterLockoutEventArgs)
        {
            try
            {
                _tc.TrackEvent("TelligentUserOnAfterLockout", new Dictionary<string, string>
                {
                    {"UserId" , userAfterLockoutEventArgs.Id.ToString()},
                    {"UserName" , userAfterLockoutEventArgs.Username},
                    {"UserEmail" , userAfterLockoutEventArgs.PrivateEmail},
                });
            }
            catch (Exception e)
            {
                Apis.Get<IEventLog>().Write("Application Inisights Failed: " + e, new EventLogEntryWriteOptions() { Category = "Logging", EventType = "Error" });
            }
        }

        public void Uninstall()
        {

        }


        public Version Version => GetType().Assembly.GetName().Version;
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
                optionsGroup.Properties.Add(new Property("Enabled", "Enabled", PropertyType.Bool, 0, "True"));

                return groupArray;
            }
        }
    }
}