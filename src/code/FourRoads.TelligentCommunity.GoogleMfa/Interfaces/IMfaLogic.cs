using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Interfaces
{
    public interface IMfaLogic
    {

        void Initialize();
        void RegisterUrls(IUrlController controller);

        bool TwoFactorEnabled(User user);
        void EnableTwoFactor(User user, bool enabled);
        bool ValidateTwoFactorCode(User user, string code);
    }
}