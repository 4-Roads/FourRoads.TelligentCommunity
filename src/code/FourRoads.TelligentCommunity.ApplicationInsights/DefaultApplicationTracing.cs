using System;
using System.Collections.Generic;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ApplicationInsights
{
    public class DefaultApplicationTracing : IApplicationInsightsApplication, IPlugin
    {
        private IApplicationInsightsPlugin _mainPlugin;

        public void Initialize()
        {
            _mainPlugin = PluginManager.GetSingleton<IApplicationInsightsPlugin>();

            if (_mainPlugin.TelemetryClient != null)
            {
                var user = Apis.Get<IUsers>();

                user.Events.AfterLockout += EventsOnAfterLockout;
                user.Events.AfterCreate += EventsOnAfterCreate;
                user.Events.BeforeDelete += EventsOnBeforeDelete;
                user.Events.AfterAuthenticate += EventsOnAfterAuthenticate;

            }
        }

        private void EventsOnAfterAuthenticate(UserAfterAuthenticateEventArgs userAfterAuthenticateEventArgs)
        {
            try
            {
                _mainPlugin.TelemetryClient.TrackEvent(
                    "TelligentUsersOnAfterAuthenticate",
                    new Dictionary<string, string>
                    {
                        {"UserId", userAfterAuthenticateEventArgs.Id.ToString()},
                        {"UserName", userAfterAuthenticateEventArgs.Username},
                        {"UserEmail", userAfterAuthenticateEventArgs.PrivateEmail},
                    });
            }
            catch (Exception e)
            {
                Apis.Get<IEventLog>().Write("Application Inisights Failed: " + e, new EventLogEntryWriteOptions() {Category = "Logging", EventType = "Error"});
            }
        }

        private void EventsOnBeforeDelete(UserBeforeDeleteEventArgs userBeforeDeleteEventArgs)
        {
            try
            {
                _mainPlugin.TelemetryClient.TrackEvent(
                    "TelligentUserOnBeforeDelete",
                    new Dictionary<string, string>
                    {
                        {"UserId", userBeforeDeleteEventArgs.Id.ToString()},
                        {"UserName", userBeforeDeleteEventArgs.Username},
                        {"UserEmail", userBeforeDeleteEventArgs.PrivateEmail},
                        {"Reassing UserId", userBeforeDeleteEventArgs.ReassignedUserId.ToString()}
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
                _mainPlugin.TelemetryClient.TrackEvent(
                    "TelligentUserOnAfterCreate",
                    new Dictionary<string, string>
                    {
                        {"UserId", userAfterCreateEventArgs.Id.ToString()},
                        {"UserName", userAfterCreateEventArgs.Username},
                        {"UserEmail", userAfterCreateEventArgs.PrivateEmail},
                    });
            }
            catch (Exception e)
            {
                Apis.Get<IEventLog>().Write("Application Inisights Failed: " + e, new EventLogEntryWriteOptions() {Category = "Logging", EventType = "Error"});
            }
        }

        private void EventsOnAfterLockout(UserAfterLockoutEventArgs userAfterLockoutEventArgs)
        {
            try
            {
                _mainPlugin.TelemetryClient.TrackEvent(
                    "TelligentUserOnAfterLockout",
                    new Dictionary<string, string>
                    {
                        {"UserId", userAfterLockoutEventArgs.Id.ToString()},
                        {"UserName", userAfterLockoutEventArgs.Username},
                        {"UserEmail", userAfterLockoutEventArgs.PrivateEmail},
                    });
            }
            catch (Exception e)
            {
                Apis.Get<IEventLog>().Write("Application Inisights Failed: " + e, new EventLogEntryWriteOptions() {Category = "Logging", EventType = "Error"});
            }
        }

        public string Name => "Default Analytics";

        public string Description => "tracks user events and exceptions";
    }

}