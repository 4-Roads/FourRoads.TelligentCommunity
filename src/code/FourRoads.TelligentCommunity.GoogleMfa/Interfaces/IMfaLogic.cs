using System.Collections.Generic;
using FourRoads.TelligentCommunity.GoogleMfa.Model;
using FourRoads.TelligentCommunity.GoogleMfa.Plugins;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.Interfaces
{
    public interface IMfaLogic
    {

        void Initialize(bool enableEmailVerification, IVerifyEmailProvider emailProvider , ISocketMessage sockentMessage);
        void RegisterUrls(IUrlController controller);

        bool TwoFactorEnabled(User user);
        void EnableTwoFactor(User user, bool enabled);
        bool ValidateTwoFactorCode(User user, string code);
        bool ValidateEmailCode(User user, string code);
        bool SendValidationCode(User user);
        string GetAccountSecureKey(User user);
        string GetAccountSecureKey(User user, bool useCache);

        List<OneTimeCode> GenerateCodes(User user);
        OneTimeCodesStatus GetCodesStatus(User user);
    }
}