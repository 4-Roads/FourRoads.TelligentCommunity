using System;
using System.Collections.Generic;
using System.Text;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Model;
using Google.Authenticator;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins.WidgetApi
{
    public class MfaScriptedFragment
    {
        public SetupInfo GenerateSetupInfo()
        {
            var userService = Apis.Get<IUsers>();
            var groupsService = Apis.Get<IGroups>();

            var user = userService.AccessingUser;

            if (user.Username != userService.AnonymousUserName)
            {
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();

                var secretKey = Injector.Get<IMfaLogic>().GetAccountSecureKey(user);
                var setupInfo = tfa.GenerateSetupCode(groupsService.GetRootGroup().Name, user.PrivateEmail, ConvertSecretToBytes(secretKey, false));

                return new SetupInfo() { ManualEntrySetupCode = setupInfo.ManualEntryKey, QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl };
            }

            return null;
        }

        private static byte[] ConvertSecretToBytes(string secret, bool secretIsBase32) =>
            secretIsBase32 ? Base32Encoding.ToBytes(secret) : Encoding.UTF8.GetBytes(secret);

        public bool TwoFactorEnabled()
        {
            var user = Apis.Get<IUsers>().AccessingUser;
            return TwoFactorEnabled(user.Id.Value);
        }

        [Documentation("Returns MFA status for user associated with UserId. Only users in Administrators role can access this method, otherwise InvalidOperationException exception will be thrown")]
        public bool TwoFactorEnabled(int userId)
        {
            if (!IsValidAccessingUser(userId)) return false;
            
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions { Id = userId });
            return Injector.Get<IMfaLogic>().IsTwoFactorEnabled(user);
        }

        public void EnableTwoFactor(bool enabled)
        {
            var user = Apis.Get<IUsers>().AccessingUser;
            EnableTwoFactor(user.Id.Value, enabled);
        }

        [Documentation("Turns on/off MFA status for user associated with UserId. Only users in Administrators role can access this method, otherwise InvalidOperationException exception will be thrown")]
        public void EnableTwoFactor(int userId, bool enabled)
        {
            if (!IsValidAccessingUser(userId)) return;
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions { Id = userId });
            Injector.Get<IMfaLogic>().EnableTwoFactor(user, enabled);
        }

        public bool Validate(string code)
        {
            var userService = Apis.Get<IUsers>();
            var user = userService.AccessingUser;

            return user.Username != userService.AnonymousUserName 
                   && Injector.Get<IMfaLogic>().ValidateTwoFactorCode(user, code.Replace(" ", string.Empty));
        }

        public List<OneTimeCode> GenerateCodes(int userId)
        {
            if (!IsValidAccessingUser(userId) || !TwoFactorEnabled(userId)) return new List<OneTimeCode>();

            var user = Apis.Get<IUsers>().Get(new UsersGetOptions { Id = userId });
            return Injector.Get<IMfaLogic>().GenerateCodes(user);
        }

        /// <summary>
        /// check that accessing user matches the userId or the accessing user is admin
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private static bool IsValidAccessingUser(int userId)
        {
            var accessingUser = Apis.Get<IUsers>().AccessingUser;
            if (accessingUser.Id == userId)
            {
                //user is generating codes for him/herself
                return true;
            }
            //see if the accessing user is admin
            if (!Apis.Get<IRoleUsers>().IsUserInRoles(accessingUser.Username, new string[] { _adminRoleName }))
            {
                throw new InvalidOperationException(_nonAdminAccessingException);
            }
            return true;
        }

        public OneTimeCodesStatus GetOneTimeCodesStatus(int userId)
        {
            if (!IsValidAccessingUser(userId)) return null;
            
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions { Id = userId });
            return Injector.Get<IMfaLogic>().GetCodesStatus(user);
        }

        public bool ValidateEmailVerificationCode(string code)
        {
            var user = Apis.Get<IUsers>().AccessingUser;

            if (user.Username != Apis.Get<IUsers>().AccessingUser.Username)
            {
                return false;
            }

            return Injector.Get<IMfaLogic>().ValidateEmailCode(user , code);
        }

        public bool SendOneTimeEmailVerificationCode(int userId)
        {
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions { Id = userId });

            return Injector.Get<IMfaLogic>().SendValidationCode(user);
        }


        const string _nonAdminAccessingException = "Accessing user does not have sufficient rights";
        const string _adminRoleName = "Administrators";
    }
}