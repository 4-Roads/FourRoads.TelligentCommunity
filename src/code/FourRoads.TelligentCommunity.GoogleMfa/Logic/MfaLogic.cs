using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Security;
using FourRoads.Common.Interfaces;
using FourRoads.TelligentCommunity.GoogleMfa.Interfaces;
using Google.Authenticator;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Urls.Version1;
using FourRoads.Common.TelligentCommunity.Routing;
using Telligent.Evolution.Components;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;
using FourRoads.TelligentCommunity.GoogleMfa.Model;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Globalization;

namespace FourRoads.TelligentCommunity.GoogleMfa.Logic
{
    public interface ILock<T>
    {
        IDisposable Enter(T id);
    }

    public class ActionDisposable : IDisposable
    {
        private readonly Action action;

        public ActionDisposable(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            this.action = action;
        }

        public void Dispose()
        {
            action();
        }
    }

    public class NamedItemLockSpin<T> : ILock<T>
    {

        private readonly ConcurrentDictionary<T, object> locks = new ConcurrentDictionary<T, object>();

        private readonly int spinWait;

        public NamedItemLockSpin(int spinWait)
        {
            this.spinWait = spinWait;
        }

        public IDisposable Enter(T id)
        {
            while (!locks.TryAdd(id, new object()))
            {
                Thread.SpinWait(spinWait);
            }

            return new ActionDisposable(() => exit(id));
        }

        private void exit(T id)
        {
            object obj;
            locks.TryRemove(id, out obj);
        }
    }

    public class MfaLogic : IMfaLogic
    {
        private static readonly string _pageName = "mfa";
        private readonly IUsers _usersService;
        private readonly IUrl _urlService;
        private readonly IMfaDataProvider _mfaDataProvider;
        private readonly ICache _cache;
        private readonly NamedItemLockSpin<int> _namedLocks = new NamedItemLockSpin<int>(10);

        //{295391e2b78d4b7e8056868ae4fe8fb3}
        private static readonly string _defaultPageLayout = " <contentFragmentPage pageName=\"mfa\" isCustom=\"false\" layout=\"Content\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::295391e2b78d4b7e8056868ae4fe8fb3\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        //this is version flag to distinguish major changes in MFA logic, so we can tell users if they should regenerate their keys
        private static readonly int _mfaLogicVersion = 2;
        private static readonly int _oneTimeCodesToGenerate = 10;
        //Plaintext length of the code with any spaces removed
        private static readonly int _oneTimeCodeLength = 8;
        private static readonly string _eakey_mfaEnabled = "__mfaEnabled";
        private static readonly string _eakey_codesGeneratedOnUtc = "__mfaCodesGeneratedOnUtc";
        private static readonly string _eakey_mfaVersion = "__mfaVersion";

        public MfaLogic(IUsers usersService, IUrl urlService, IMfaDataProvider mfaDataProvider, ICache cache)
        {
            _usersService = usersService;
            _urlService = urlService;
            _mfaDataProvider = mfaDataProvider;
            _cache = cache;
        }

        public void Initialize()
        {
            _usersService.Events.AfterIdentify += EventsAfterIdentify;
            _usersService.Events.AfterAuthenticate += EventsOnAfterAuthenticate;
        }

        /// <summary>
        /// intercept the user has logged in and decide if we need to enforce mfa for this session
        /// </summary>
        /// <param name="userAfterAuthenticateEventArgs"></param>
        private void EventsOnAfterAuthenticate(UserAfterAuthenticateEventArgs userAfterAuthenticateEventArgs)
        {
            //user has authenticated
            //is 2 factor enabled for user?
            var user = _usersService.Get(new UsersGetOptions() { Username = userAfterAuthenticateEventArgs.Username });
            if (TwoFactorEnabled(user))
            {
                var request = HttpContext.Current.Request;
                if (request.Url.Host.ToLower() == "localhost" && request.Url.LocalPath.ToLower() == "/controlpanel/localaccess.aspx")
                {
                    //bypass mfa for emergency local access
                    SetTwoFactorState(user, true);
                }
                else
                {
                    //Yes set flag to false
                    SetTwoFactorState(user, false);
                }
            }
            else
            {
                //no set flag to true
                SetTwoFactorState(user, true);
            }
        }

        /// <summary>
        /// Intercept requests and trap when a user has logged in 
        /// but still needs to perform the second auth stage. 
        /// At this point the user is technically authenticated with telligent 
        /// so we also need to supress any callbacks etc whilst the second stage 
        /// auth is being performed.
        /// </summary>
        /// <param name="e"></param>
        protected void EventsAfterIdentify(UserAfterIdentifyEventArgs e)
        {
            var user = _usersService.AccessingUser;
            if (user.Username != _usersService.AnonymousUserName)
            {
                if (TwoFactorEnabled(user) && TwoFactorState(user) == false)
                {
                    // user is logged in but has not completed the second auth stage
                    var request = HttpContext.Current.Request;

                    if (request.Path.StartsWith("/socket.ashx"))
                    {
                        return;
                    }

                    var response = HttpContext.Current.Response;

                    // suppress any callbacks re search, notifications, header links etc
                    if (IsOauthRequest(request) == false && (request.Path.StartsWith("/api.ashx") ||
                        request.Path.StartsWith("/oauth") ||
                        (request.Url.LocalPath == "/utility/scripted-file.ashx" &&
                        request.QueryString["_cf"] != null &&
                        request.QueryString["_cf"] != "logout.vm" &&
                        request.QueryString["_cf"] != "validate.vm")))
                    {
                        // this should only happen when in the second auth stage 
                        // for blocked callbacks so a bit brutal
                        response.Clear();
                        response.End();
                    }

                    // is it a suitable time to redirect the user to the second auth page
                    if (response.ContentType == "text/html" &&
                        !request.Path.StartsWith("/tinymce") &&
                        request.Url.LocalPath != "/logout" &&
                        request.Url.LocalPath != "/mfa" &&
                        string.Compare(HttpContext.Current.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) == 0 &&
                        //Is this a main page and not a callback etc 
                        (request.CurrentExecutionFilePathExtension == ".aspx" ||
                         request.CurrentExecutionFilePathExtension == ".htm" ||
                         request.CurrentExecutionFilePathExtension == ".ashx" ||
                         request.CurrentExecutionFilePathExtension == string.Empty))
                    {
                        //redirect to 2 factor page
                        response.Redirect("/mfa" + "?ReturnUrl=" + _urlService.Encode(request.RawUrl), true);
                    }
                }
            }
        }

        private bool IsOauthRequest(HttpRequest request)
        {
            // path is authorize url
            if (request.Path == "/api.ashx/v2/oauth/authorize") return true;

            // path is 'get token' 
            if (request.Path == "/api.ashx/v2/oauth/token") return true;

            // or allow/deny page
            var result = (request.Path == "/utility/scripted-file.ashx"
                    && request.QueryString["client_id"] != null
                    && (request.QueryString["redirect_uri"] != null
                        || request.QueryString["response_type"] != null)
                        || request.QueryString["client_secret"] != null
                        || request.QueryString["code"] != null
                        || request.QueryString["grant_type"] != null
                        || request.QueryString["username"] != null
                    );

            return result;
        }

        private string GetSessionID(HttpContext context)
        {
            var cookie = context.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (cookie != null)
                return cookie.Value.Substring(0, 10); //Chances of collission with 10 chars is small

            return string.Empty;
        }

        private void SetTwoFactorState(User user, bool passed)
        {
            using (var sync = _namedLocks.Enter(user.Id.GetValueOrDefault(0)))
            {
                string cacheKey = GetCacheKey(user);

                _cache.Remove(cacheKey);

                _mfaDataProvider.SetUserState(user.Id.Value, GetSessionID(HttpContext.Current), passed);
            }
        }

        public bool TwoFactorEnabled(User user)
        {
            if (IsImpersonator())
            {
                return false;
            }

            bool require2F = false;

            //ensure we have access to user.ExtendedAttributes
            _usersService.RunAsUser(_usersService.ServiceUserName, () =>
            {
                var mfaEnabled = user.ExtendedAttributes.Get(_eakey_mfaEnabled);

                if (mfaEnabled != null)
                {
                    bool.TryParse(mfaEnabled.Value, out require2F);
                }
            });

            return require2F;
        }

        private bool IsImpersonator()
        {
            var context = HttpContext.Current;
            if (context == null)
            {
                return false;
            }
            HttpCookie httpCookie = context.Request.Cookies["Impersonator"];
            return (httpCookie != null && !string.IsNullOrEmpty(httpCookie.Value));
        }

        public void EnableTwoFactor(User user, bool enabled)
        {
            //ensure we have access to user.ExtendedAttributes
            _usersService.RunAsUser(_usersService.ServiceUserName, () =>
            {
                UsersUpdateOptions updateOptions = new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = user.ExtendedAttributes };
                if (enabled == false)
                {
                    //old codes should be deleted
                    _mfaDataProvider.ClearCodes(user.Id.Value);
                    _mfaDataProvider.ClearUserKey(user.Id.Value);
                    //remove version number
                    updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_mfaVersion, Value = string.Empty });
                }
                else
                {
                    //store plugin version in EA
                    updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_mfaVersion, Value = _mfaLogicVersion.ToString(CultureInfo.InvariantCulture) });
                }
                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_mfaEnabled, Value = enabled.ToString() });
                _usersService.Update(updateOptions);
            });
        }

        public bool ValidateTwoFactorCode(User user, string code)
        {
            //check to see if we got backup code which is 8 digits, 
            //while the Authenticator app generates 6 digit codes
            if (code.Length == _oneTimeCodeLength)
            {
                if (ValidateOneTimeCode(user, code))
                {
                    SetTwoFactorState(user, true);
                    return true;
                }
            }

            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            if (tfa.ValidateTwoFactorPIN(GetAccountSecureKey(user), code))
            {
                SetTwoFactorState(user, true);

                return true;
            }

            return false;
        }

        private bool ValidateOneTimeCode(User user, string code)
        {
            return _mfaDataProvider.RedeemCode(user.Id.Value, code.Encrypt(GetAccountSecureKey(user), user.Id.Value));
        }

        private string GetCacheKey(User user)
        {
            return $"MFA:CACHE:{user.Id}";
        }

        private string GetUserKeyCacheKey(User user)
        {
            return $"MFA:KEY:{user.Id}";
        }
        public string GetAccountSecureKey(User user)
        {
            var key = _cache.Get<string>(GetUserKeyCacheKey(user));
            if (string.IsNullOrWhiteSpace(key))
            {
                Guid userKey = _mfaDataProvider.GetUserKey(user.Id.Value);
                if(userKey == Guid.Empty)
                {
                    userKey = Guid.NewGuid();
                    _mfaDataProvider.SetUserKey(user.Id.Value, userKey);
                }
                //N prints like '67be0d0d7e894d0cb1ee483d1e1f43fb'
                key = userKey.ToString("N");
                _cache.Insert(GetUserKeyCacheKey(user), key);
            }
            return key;
        }

        private bool TwoFactorState(User user)
        {
            string cacheKey = GetCacheKey(user);

            var valid = _cache.Get<bool?>(cacheKey);

            if (!valid.HasValue)
            {
                using (var sync = _namedLocks.Enter(user.Id.GetValueOrDefault(0)))
                {
                    valid = _mfaDataProvider.GetUserState(user.Id.Value, GetSessionID(HttpContext.Current));

                    _cache.Insert(cacheKey, valid);
                }
            }

            return valid.Value;
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_pageName, _pageName, new SiteRootRouteConstraint(), null, _pageName, new PageDefinitionOptions
            {
                DefaultPageXml = _defaultPageLayout,
                Validate = (context, accessController) =>
                {
                    if (_usersService.AccessingUser != null)
                    {
                        if (_usersService.AnonymousUserName == _usersService.AccessingUser.Username)
                        {
                            accessController.AccessDenied("This page is not available to you", false);
                        }
                    }
                }
            });
        }



        public List<OneTimeCode> GenerateCodes(User user)
        {
            //old codes should be deleted
            _mfaDataProvider.ClearCodes(user.Id.Value);

            var codes = new List<OneTimeCode>(_oneTimeCodesToGenerate);
            var generatedOnUtc = DateTime.UtcNow;

            for (int i = 0; i < _oneTimeCodesToGenerate; i++)
            {
                //generatig the code in form of XXXX XXXX with zero-padding
                string plainTextCode = $"{MfaCryptoExtension.RandomInteger(0, 9999):D4} {MfaCryptoExtension.RandomInteger(0, 9999):D4}";
                string encryptedCode = plainTextCode.Encrypt(GetAccountSecureKey(user), user.Id.Value);
                //we store just the hash value of the code, but...
                OneTimeCode code = _mfaDataProvider.CreateCode(user.Id.Value, encryptedCode);
                //..but return plain text code so users could see and print/save them
                code.PlainTextCode = plainTextCode;

                codes.Add(code);
            }
            // add note about time when codes were generated
            _usersService.RunAsUser(_usersService.ServiceUserName, () =>
            {
                UsersUpdateOptions updateOptions = new UsersUpdateOptions() { Id = user.Id.Value, ExtendedAttributes = user.ExtendedAttributes };
                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_codesGeneratedOnUtc, Value = DateTime.UtcNow.ToString("O") });
                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_mfaVersion, Value = _mfaLogicVersion.ToString(CultureInfo.InvariantCulture) });
                _usersService.Update(updateOptions);
            });
            return codes;
        }

        public OneTimeCodesStatus GetCodesStatus(User user)
        {
            var result = new OneTimeCodesStatus();
            //ensure we have access to user.ExtendedAttributes
            _usersService.RunAsUser(_usersService.ServiceUserName, () =>
            {
                var mfaVersion = user.ExtendedAttributes.Get(_eakey_mfaVersion);
                if (mfaVersion != null)
                {
                    if (int.TryParse(mfaVersion.Value, out int version))
                    {
                        result.Version = version;
                    }
                }

                var codesGenerated = user.ExtendedAttributes.Get(_eakey_codesGeneratedOnUtc);
                if (codesGenerated != null)
                {
                    if (DateTime.TryParse(codesGenerated.Value, out DateTime generatedOnUtc))
                    {
                        result.CodesGeneratedOn = generatedOnUtc;
                    }
                }

                result.CodesLeft = _mfaDataProvider.CountCodesLeft(user.Id.Value);
            });
            return result;
        }
    }
}
