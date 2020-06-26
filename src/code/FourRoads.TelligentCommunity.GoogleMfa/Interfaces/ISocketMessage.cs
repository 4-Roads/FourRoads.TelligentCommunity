using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Interfaces
{
    public interface ISocketMessage
    {
        void NotifyCodeAccepted(User user);
    }
}