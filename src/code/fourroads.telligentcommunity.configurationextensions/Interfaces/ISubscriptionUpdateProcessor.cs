using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Interfaces
{
    public interface ISubscriptionUpdateProcessor : IPlugin, IApplicationPlugin
    {
        bool CanProcess(JobData jobData);
        void Process(JobData jobData);
    }
}