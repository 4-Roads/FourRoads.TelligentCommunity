using System;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.PowerBI
{
    public class PowerBIUserJob : IRecurringEvolutionJobPlugin
    {
        public void Initialize()
        {
        }

        public string Name => "4 Roads - Power BI - User Update Job";
        public string Description => "Updates the user profile data in Power BI";

        public void Execute(JobData jobData)
        {
            PluginManager.GetSingleton<PowerBIPlugin>().UpdateUserProfiles();
        }

        public Guid JobTypeId { get; } = new Guid("{065CC3BA-DE23-4A46-9DCE-8A31D09A3B83}");

        public JobSchedule DefaultSchedule => new JobSchedule(ScheduleType.Hours) { Hours = 12};

        public JobContext SupportedContext => JobContext.Service;
    }
}