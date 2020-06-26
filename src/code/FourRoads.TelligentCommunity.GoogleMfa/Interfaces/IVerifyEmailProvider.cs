using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Interfaces
{
    public interface IVerifyEmailProvider
    {
        void SendEmail(User user, string code);
    }
}