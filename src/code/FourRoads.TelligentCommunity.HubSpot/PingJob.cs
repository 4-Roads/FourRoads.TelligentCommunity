using System;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class PingJob : IRecurringEvolutionJobPlugin
    {
        public void Initialize()
        {
        }

        public string Name => "Hubspot - Access Token Refresh Job";
        public string Description => "Keep alive service for hubspot";

        public void Execute(JobData jobData)
        {
            var token = PluginManager.GetSingleton<HubspotCrm>().GetAccessToken(forceRenew: true);
            Apis.Get<IEventLog>().Write($"Refreshed access token. New token prefix is {token.Substring(0,5)}.",
                new EventLogEntryWriteOptions
                {
                    Category = Name
                });
        }

        public Guid JobTypeId { get; } = new Guid("{1AA2ACFF-17BA-4FED-8E6B-9A54CAA6F614}");

        public JobSchedule DefaultSchedule => new JobSchedule(ScheduleType.Minutes) {Minutes = 25};
        public JobContext SupportedContext => JobContext.Service;
    }
}