using System.Collections.Generic;
using System.Linq;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Interfaces;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.SubscriptionProcessors
{
    public class SubscriptionUpdatProcessingFactory
    {
        private SubscriptionUpdatProcessingFactory()
        {
        }

        public static List<ISubscriptionUpdateProcessor> GetProcessors(JobData jobData)
        {
            return PluginManager.Get<ISubscriptionUpdateProcessor>().Where(p => p.CanProcess(jobData)).ToList();
        }
    }
}