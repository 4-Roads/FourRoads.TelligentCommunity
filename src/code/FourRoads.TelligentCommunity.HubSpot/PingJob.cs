using System;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class PingJob : IRecurringEvolutionJobPlugin
    {
        public void Initialize()
        {
        }

        public string Name => "4 Roads Hubspot Ping Job";
        public string Description => "Keep alive service for hubspot";

        public void Execute(JobData jobData)
        {
            PluginManager.GetSingleton<HubspotCrm>().GetAccessToken();
        }

        public Guid JobTypeId { get; } = new Guid("{1AA2ACFF-17BA-4FED-8E6B-9A54CAA6F614}");

        public JobSchedule DefaultSchedule => new JobSchedule(ScheduleType.Hours) {Hours = 4};
        public JobContext SupportedContext => JobContext.Service;
    }
}