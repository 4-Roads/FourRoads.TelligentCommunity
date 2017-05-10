using System;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Triggers;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.ThreadViews.Jobs
{
    public class ThreadViewJob : IRecurringEvolutionJobPlugin
    {
        public void Initialize()
        {
        }

        public string Name => "4 Roads - Achievements - Check Thread Views Job";
        public string Description => "Check to see if any thread views need to trigger the achievements rule";

        public void Execute(JobData jobData)
        {
            PluginManager.GetSingleton<ThreadView>().CheckViewsforTrigger();
        }

        public Guid JobTypeId { get; } = new Guid("{3DAF64EC-DCCF-48F4-99FC-7F62610D1110}");

        public JobSchedule DefaultSchedule => new JobSchedule(ScheduleType.Hours) {Hours = 1};
        public JobContext SupportedContext => JobContext.Service;
    }
}