using System;
using System.Text;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.ConfigurationExtensions.SubscriptionProcessors;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Interfaces;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Jobs
{
    public class SubscriptionUpdateJob : IEvolutionJob 
    {
        public void Execute(JobData jobData)
        {
            List<ISubscriptionUpdateProcessor> processors = SubscriptionUpdatProcessingFactory.GetProcessors(jobData);
            foreach (ISubscriptionUpdateProcessor p in processors)
            {
                try
                {
                    p.Process(jobData);
                }
                catch (Exception e) 
                { 
                    StringBuilder msg = new StringBuilder(p.Name);
                    msg.Append(" threw Exception ");
                    msg.Append(e.ToString());
                    new TCException( msg.ToString()).Log();
                }
            }
        }
    }
}