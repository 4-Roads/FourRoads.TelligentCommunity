using Microsoft.ApplicationInsights;

namespace FourRoads.TelligentCommunity.Sentrus.Interfaces
{
    using Telligent.Evolution.Extensibility.Version1;

    public interface IApplicationInsightsApplication
    {
        
    }

    public interface IApplicationInsightsPlugin : IPluginGroup, ISingletonPlugin
    {
        TelemetryClient TelemetryClient { get; }
    }
}