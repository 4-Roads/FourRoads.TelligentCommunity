using System.Collections.Generic;
using FourRoads.TelligentCommunity.GoogleMfa.Model;
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

        string GetAccountSecureKey(User user);
        string GetAccountSecureKey(User user, bool useCache);

        List<OneTimeCode> GenerateCodes(User user);
        OneTimeCodesStatus GetCodesStatus(User user);
    }
}