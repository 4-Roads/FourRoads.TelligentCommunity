using Telligent.Evolution.Extensibility.Jobs.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Jobs
{
    public class SubscriptionUpdateJob : IEvolutionJob 
    {
        public void Execute(JobData jobData)
        {
            SubscriptionUpdatProcessingFactory.GetProcessors(jobData).ForEach(p => p.Process(jobData));
        }
    }
}