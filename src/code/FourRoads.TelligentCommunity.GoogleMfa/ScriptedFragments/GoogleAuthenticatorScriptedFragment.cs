using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.GoogleMfa.Extensions;
using FourRoads.TelligentCommunity.GoogleMfa.Interfaces;
using Google.Authenticator;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.GoogleMfa.ScriptedFragments
{
    public class GoogleAuthenticatorScriptedFragment
    {
        public SetupInfo GenerateSetupInfo()
        {
            var userService = Apis.Get<IUsers>();
            var groupsService = Apis.Get<IGroups>();

            var user = userService.AccessingUser;

            if (user.Username != userService.AnonymousUserName)
            {
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();

                var setupInfo = tfa.GenerateSetupCode(groupsService.GetRootGroup().Name, user.PrivateEmail, user.ContentId.ToString(), 300, 300);

                return new SetupInfo() {ManualEntrySetupCode = setupInfo.ManualEntryKey, QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl};
            }

            return null;
        }

        public bool TwoFactorEnabled()
        {
            var userService = Apis.Get<IUsers>();

            var user = userService.AccessingUser;

            return Injector.Get<IMfaLogic>().TwoFactorEnabled(user);
        }

        public void EnableTwoFactor(bool enabled)
        {
            var userService = Apis.Get<IUsers>();

            var user = userService.AccessingUser;

            Injector.Get<IMfaLogic>().EnableTwoFactor(user , enabled);
        }

        public bool Validate(string code)
        {
            var userService = Apis.Get<IUsers>();

            var user = userService.AccessingUser;

            if (user.Username != userService.AnonymousUserName)
            {
                return Injector.Get<IMfaLogic>().ValidateTwoFactorCode(user, code);
            }

            return false;
        }
    }
}