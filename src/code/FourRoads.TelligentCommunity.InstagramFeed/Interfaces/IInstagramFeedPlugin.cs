using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.InstagramFeed.Interfaces
{
    public interface IInstagramFeedPlugin : ISingletonPlugin
    {
        string AppId { get; }

        string AppSecret { get; }
    }
}
