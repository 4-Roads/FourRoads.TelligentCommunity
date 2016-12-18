using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Sentrus.Jobs
{

    public class HealthJob : IRecurringEvolutionJobPlugin, ISingletonPlugin
    {
        protected Guid JobId = new Guid("{055260B5-6C18-4DBD-BB76-E6EFFE502CF9}");
        public void Execute()
        {
     
        }

        public Telligent.Evolution.Extensibility.Jobs.Version1.JobSchedule DefaultSchedule
        {
            get { return new JobSchedule(ScheduleType.Daily); }
        }

        public System.Guid JobTypeId
        {
            get { return JobId; }
        }

        public Telligent.Evolution.Extensibility.Jobs.Version1.JobContext SupportedContext
        {
            get { return Telligent.Evolution.Extensibility.Jobs.Version1.JobContext.Service; }
        }

        public void Execute(Telligent.Evolution.Extensibility.Jobs.Version1.JobData jobData)
        {
            IHealthPlugin plugin = PluginManager.GetSingleton<IHealthPlugin>();

            if (plugin != null)
            {
                if (PluginManager.IsEnabled(plugin))
                {
                    IEnumerable<IHealthExtension> healthExtensions = PluginManager.Get<IHealthExtension>();

                    foreach (IHealthExtension healthExtension in healthExtensions)
                    {
                        healthExtension.ExecuteJob();
                    }
                }
            }
        }

        public string Description
        {
            get { return "Job for running health tasks";}
        }

        public void Initialize()
        {

        }

        public string Name
        {
             get { return "Health Maintenance Job"; }
        }
    }
}