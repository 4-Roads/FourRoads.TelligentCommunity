using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Logic;
using FourRoads.TelligentCommunity.Mfa.Model;
using Google.Authenticator;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins.WidgetApi
{
    public class MfaScriptedFragment
    {
        private IMfaLogic _mfaLogic;
        private IUsers _users;
        private IGroups _groups;
        private IRoleUsers _roleUsers;

        public MfaScriptedFragment(IMfaLogic mfaLogic, IUsers users, IGroups grooups , IRoleUsers roleUsers)
        {
             _mfaLogic = mfaLogic;
             _users = users;
             _groups = grooups;
             _roleUsers = roleUsers;
        }

        public SetupInfo GenerateSetupInfo()
        {
            var userService = _users;
            var groupsService = _groups;

            var user = userService.AccessingUser;

            if (user.Username != userService.AnonymousUserName)
            {
                var tfa = new FourRoadsTwoFactorAuthenticator();

                var secretKey = _mfaLogic.GetAccountSecureKey(user);
                var setupInfo = tfa.GenerateSetupCode(groupsService.GetRootGroup().Name, user.PrivateEmail, ConvertSecretToBytes(secretKey, false));

                return new SetupInfo() { ManualEntrySetupCode = setupInfo.ManualEntryKey, QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl };
            }

            return null;
        }
        [Documentation("Returns the type of persistence for the MFA cookie, Off, UserDefined, Authentication")]
        public string PersistenceType => PluginManager.Get<MfaPluginCore>().FirstOrDefault().PersistenceType.ToString();

        [Documentation("Returns the length in days that the MFA cookie will be persisted, only valid for UserDefined")]
        public int PersistenceDuration => PluginManager.Get<MfaPluginCore>().FirstOrDefault().PersistenceDuration;

        private static byte[] ConvertSecretToBytes(string secret, bool secretIsBase32) => secretIsBase32 ? Base32Encoding.ToBytes(secret) : Encoding.UTF8.GetBytes(secret);

        [Documentation("Returns if MFA is enabled for the current user")]
        public bool TwoFactorEnabled()
        {
            var user = _users.AccessingUser;
            return TwoFactorEnabled(user.Id.Value);
        }

        [Documentation("Returns MFA status for user associated with UserId. Only users in Administrators role can access this method, otherwise InvalidOperationException exception will be thrown")]
        public bool TwoFactorEnabled(int userId)
        {
            if (!IsValidAccessingUser(userId)) return false;
            
            var user =  _users.Get(new UsersGetOptions { Id = userId });
        
            return _mfaLogic.IsTwoFactorEnabled(user);
        }

        [Documentation("Enables or disables MFA for the current user")]
        public void EnableTwoFactor(bool enabled)
        {
            var user = _users.AccessingUser;

            EnableTwoFactor(user.Id.Value, enabled);
        }

        [Documentation("Turns on/off MFA status for user associated with UserId. Only users in Administrators role can access this method, otherwise InvalidOperationException exception will be thrown")]
        public void EnableTwoFactor(int userId, bool enabled)
        {
            if (!IsValidAccessingUser(userId)) return;
            var user = _users.Get(new UsersGetOptions { Id = userId });
            _mfaLogic.EnableTwoFactor(user, enabled);
        }
       
        [Documentation("Validates a code, and if persist flag is set then cookie is set for that period")]
        public bool Validate(string code, bool persist)
        {
            var userService = _users;
            var user = userService.AccessingUser;

            return user.Username != userService.AnonymousUserName 
                   && _mfaLogic.ValidateTwoFactorCode(user, code.Replace(" ", string.Empty), persist);
        }

        [Documentation("Generates a set of backup codes for this user id that is passed in")]
        public List<OneTimeCode> GenerateCodes(int userId)
        {
            if (!IsValidAccessingUser(userId) || !TwoFactorEnabled(userId)) return new List<OneTimeCode>();

            var user = _users.Get(new UsersGetOptions { Id = userId });
            return _mfaLogic.GenerateCodes(user);
        }

        /// <summary>
        /// check that accessing user matches the userId or the accessing user is admin
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private bool IsValidAccessingUser(int userId)
        {
            var accessingUser = _users.AccessingUser;
            if (accessingUser.Id == userId)
            {
                //user is generating codes for him/herself
                return true;
            }
            //see if the accessing user is admin
            if (!_roleUsers.IsUserInRoles(accessingUser.Username, new string[] { _adminRoleName }))
            {
                throw new InvalidOperationException(_nonAdminAccessingException);
            }
            return true;
        }

        [Documentation("Gets the status of the requested onetime backup code for this user id ")]
        public OneTimeCodesStatus GetOneTimeCodesStatus(int userId)
        {
            if (!IsValidAccessingUser(userId)) return null;
            
            var user = _users.Get(new UsersGetOptions { Id = userId });
            return _mfaLogic.GetCodesStatus(user);
        }

        [Documentation("Validates the email verification code for the current user")]
        public bool ValidateEmailVerificationCode(string code)
        {
            var user = _users.AccessingUser;

            if (user.Username != _users.AccessingUser.Username)
            {
                return false;
            }

            return _mfaLogic.ValidateEmailCode(user , code);
        }

        [Documentation("Forces sending a onetime code for email verification")]
        public bool SendOneTimeEmailVerificationCode(int userId)
        {
            var user = _users.Get(new UsersGetOptions { Id = userId });

            return _mfaLogic.SendValidationCode(user);
        }

        [Documentation("")]
        public string ManageMfaUrl()
        {
            return "/manage_mfa";
        }

        public string VerifiedEmail(int userId)
        {
            if (!EmailValidationEnabled)
                return string.Empty; ;

            if (!IsValidAccessingUser(userId))
                return string.Empty;

            var user = _users.Get(new UsersGetOptions { Id = userId });

            return _mfaLogic.VerifiedEmail(user);
        }

        [Documentation("Sets the users email as verified")]
        public bool VerifyEmail(int userId)
        {
            if (!EmailValidationEnabled)
                return true;

            if (!IsValidAccessingUser(userId))
                return false;

            var user = _users.Get(new UsersGetOptions { Id = userId });

            return _mfaLogic.VerifyEmail(user);
        }

        [Documentation("Is the current email address for this user id out of date")]
        public bool EmailVerificationRequired(int userId)
        {
            if (!EmailValidationEnabled)
                return false;

            if (!IsValidAccessingUser(userId)) 
                return false;

            var user = _users.Get(new UsersGetOptions { Id = userId });

            return _mfaLogic.EmailValidationRequired(user);
        }

        [Documentation("Flag to indicate if email validation is turned on")]
        public bool EmailValidationEnabled => _mfaLogic.EmailValidationEnabled;

        [Documentation("")]
        public bool CurrentUserRequiresMfa()
        {
            return _mfaLogic.UserRequiresMfa(_users.AccessingUser);
        }

        const string _nonAdminAccessingException = "Accessing user does not have sufficient rights";
        
        const string _adminRoleName = "Administrators";
    }
}