using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Interfaces
{
    public interface IVerifyEmailProvider
    {
        void SendEmail(User user, string code);
    }
}