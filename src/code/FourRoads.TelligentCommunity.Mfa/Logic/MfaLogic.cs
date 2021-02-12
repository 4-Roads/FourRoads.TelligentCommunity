using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Security;
using FourRoads.Common.Interfaces;
using FourRoads.Common.TelligentCommunity.Routing;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Model;
using Google.Authenticator;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;

namespace FourRoads.TelligentCommunity.Mfa.Logic
{
    public class MfaLogic : IMfaLogic
    {
        private static readonly string _mfaPageName = "mfa";
        private static readonly string _pageVerifyEmailName = "verifyemail";
        private readonly IUsers _usersService;
        private readonly IUrl _urlService;
        private readonly IMfaDataProvider _mfaDataProvider;
        private readonly ICache _cache;
        private readonly NamedItemLockSpin<int> _namedLocks = new NamedItemLockSpin<int>(10);
        private IVerifyEmailProvider _emailProvider;
        private ISocketMessage _sockentMessager;
        private bool _enableEmailVerification;

        //{295391e2b78d4b7e8056868ae4fe8fb3}
        private static readonly string _defaultMfaPageLayout = $"<contentFragmentPage pageName=\"{_mfaPageName}\" isCustom=\"false\" layout=\"Content\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::295391e2b78d4b7e8056868ae4fe8fb3\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";
        private static readonly string _defaultVerifyEmailPageLayout = $"<contentFragmentPage pageName=\"{_pageVerifyEmailName}\" isCustom=\"false\" layout=\"Content\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::a8b6e56eac3246169d1727c84c17fd66\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";
        //this is version flag to distinguish major changes in MFA logic, so we can tell users if they should regenerate their keys
        private static readonly int _mfaLogicVersion = 2;
        private static readonly int _oneTimeCodesToGenerate = 10;
        //Plaintext length of the code with any spaces removed
        private static readonly int _oneTimeCodeLength = 8;
        private static readonly string _eakey_mfaEnabled = "__mfaEnabled";
        private static readonly string _eakey_codesGeneratedOnUtc = "__mfaCodesGeneratedOnUtc";
        private static readonly string _eakey_mfaVersion = "__mfaVersion";
        private static readonly string _eakey_emailVerified = "___emailVerified";
        private static readonly string _eakey_emailVerifyCode = "_eakey_emailVerifyCode";
        private DateTime _emailValilationCutoffDate;


        public MfaLogic(IUsers usersService, IUrl urlService, IMfaDataProvider mfaDataProvider, ICache cache)
        {
            _usersService = usersService;
            _urlService = urlService;
            _mfaDataProvider = mfaDataProvider;
            _cache = cache;
        }

        public void Initialize(bool enableEmailVerification, IVerifyEmailProvider emailProvider, ISocketMessage sockentMessager, DateTime emailValilationCutoffDate)
        {
            _enableEmailVerification = enableEmailVerification;
            _emailProvider = emailProvider;
            _sockentMessager = sockentMessager;
            _emailValilationCutoffDate = emailValilationCutoffDate;

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
        private void EventsAfterIdentify(UserAfterIdentifyEventArgs e)
        {
            var user = _usersService.AccessingUser;
            if (user.Username != _usersService.AnonymousUserName)
            {
                //Make safe for not running in webcontext
                if (HttpContext.Current != null)
                {
                    if (TwoFactorEnabled(user) && TwoFactorState(user) == false)
                    {
                        ForceRedirect("/mfa" + "?ReturnUrl=" + _urlService.Encode(HttpContext.Current.Request.RawUrl));
                    }

                    if (_enableEmailVerification && EmailChanged(user))
                    {
                        //Never validated and also joined before cutoff date so assumed a valid user
                        if (user.JoinDate < _emailValilationCutoffDate && string.IsNullOrWhiteSpace(user.ExtendedAttributes.Get(_eakey_emailVerified)?.Value))
                        {
                            SetEmailInExtendedAttributes(user);
                        }
                        else
                        {
                            ForceRedirect("/verifyemail" + "?ReturnUrl=" + _urlService.Encode(HttpContext.Current.Request.RawUrl));

                            if (EmailNotSent(user))
                            {
                                SendValidationCode(user);
                            }
                        }
                    }

                }
            }
        }

        public bool ValidateEmailCode(User user, string code)
        {
            var result = false;
            _usersService.RunAsUser(user.Username, () =>
            {
                if (!EmailChanged(user))
                {
                    result = true;
                }
                else
                {
                    //Read the users profile, if matches then clear the user profile and set _eakey_emailVerified to current email
                    if (String.CompareOrdinal(user.ExtendedAttributes.Get(_eakey_emailVerifyCode)?.Value, code) == 0)
                    {
                        SetEmailInExtendedAttributes(user);

                        result = true;
                    }
                }
            });

            if (result)
            {
                //Now send a socket message bus response so the current page refreshes
                _sockentMessager.NotifyCodeAccepted(user);
            }

            return result;
        }

        private void SetEmailInExtendedAttributes(User user)
        {
            List<ExtendedAttribute> attributes = new List<ExtendedAttribute>();

            attributes.Add(new ExtendedAttribute() { Key = _eakey_emailVerifyCode, Value = "" });
            attributes.Add(new ExtendedAttribute() { Key = _eakey_emailVerified, Value = user.PrivateEmail });

            _usersService.Update(new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = attributes });
        }

        public bool SendValidationCode(User user)
        {
            string code = MfaCryptoExtension.RandomAlphanumeric(6);
            ;
            //Send an emial
            _emailProvider.SendEmail(user, code);

            //Send a validation code, store it in the users profile extended attributes
            List<ExtendedAttribute> attributes = new List<ExtendedAttribute> { new ExtendedAttribute() { Key = _eakey_emailVerifyCode, Value = code } };

            return _usersService.Update(new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = attributes }).HasErrors();

        }
        private bool EmailNotSent(User user)
        {
            return string.IsNullOrWhiteSpace(user.ExtendedAttributes.Get(_eakey_emailVerifyCode)?.Value);
        }

        private bool EmailChanged(User user)
        {
            return (string.Compare(user.PrivateEmail, user.ExtendedAttributes.Get(_eakey_emailVerified)?.Value, StringComparison.OrdinalIgnoreCase) != 0);
        }

        private void ForceRedirect(string page)
        {
            // user is logged in but has not completed the second auth stage
            var request = HttpContext.Current.Request;

            if (request.Path.StartsWith("/socket.ashx"))
            {
                return;
            }

            var response = HttpContext.Current.Response;

            // suppress any callbacks re search, notifications, header links etc
            if (IsOauthRequest(request) == false &&
                (request.Path.StartsWith("/api.ashx") ||
                 request.Path.StartsWith("/oauth") ||
                 (request.Url.LocalPath == "/utility/scripted-file.ashx" &&
                  request.QueryString["_cf"] != null &&
                  request.QueryString["_cf"] != "logout.vm" &&
                  request.QueryString["_cf"] != "validate.vm" && request.QueryString["_cf"] != "newCode.vm")))
            {
                // this should only happen when in the second auth stage 
                // for blocked callbacks so a bit brutal
                response.Clear();
                if (HttpContext.Current.ApplicationInstance == null)
                {
                    response.End();
                }
                else
                {
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
            }

            // is it a suitable time to redirect the user to the second auth page
            if (response.ContentType == "text/html" &&
                !request.Path.StartsWith("/tinymce") &&
                request.Url.LocalPath != "/logout" &&
                request.Url.LocalPath != "/mfa" &&
                request.Url.LocalPath != "/verifyemail" &&
                string.Compare(HttpContext.Current.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) == 0 &&
                //Is this a main page and not a callback etc 
                IsPageRequest(request))
            {
                //redirect to 2 factor page
                bool force = HttpContext.Current.ApplicationInstance == null;
                response.Redirect(page, force);
                if (!force)
                {
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
            }
        }

        private static bool IsPageRequest(HttpRequest request)
        {
            return (request.CurrentExecutionFilePathExtension == ".aspx" ||
                    request.CurrentExecutionFilePathExtension == ".htm" ||
                    request.CurrentExecutionFilePathExtension == ".ashx" ||
                    request.CurrentExecutionFilePathExtension == string.Empty);
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
#if !SIMULATE_OLDMFA_KEY_VERSION 
                    updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_mfaVersion, Value = string.Empty });
#endif
                }
                else
                {
#if !SIMULATE_OLDMFA_KEY_VERSION
                    //store plugin version in EA
                    updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_mfaVersion, Value = _mfaLogicVersion.ToString(CultureInfo.InvariantCulture) });
#endif
                }
                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = _eakey_mfaEnabled, Value = enabled.ToString() });
                _usersService.Update(updateOptions);
                _cache.Remove(GetUserKeyCacheKey(user));
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
            return _mfaDataProvider.RedeemCode(user.Id.Value, code.Hash(GetAccountSecureKey(user), user.Id.Value));
        }

        private string GetCacheKey(User user)
        {
            return $"MFA:CACHE:{user.Id}";
        }

        private string GetUserKeyCacheKey(User user)
        {
            return $"MFA:KEY:{user.Id}";
        }

        public string GetAccountSecureKey(User user, bool useCache)
        {
            var key = useCache ? _cache.Get<string>(GetUserKeyCacheKey(user)) : null;
            if (string.IsNullOrWhiteSpace(key))
            {
                if (IsOldVersionUser(user))
                {
                    key = user.ContentId.ToString();
                }
                else
                {
                    Guid userKey = _mfaDataProvider.GetUserKey(user.Id.Value);
                    if (userKey == Guid.Empty)
                    {
                        userKey = Guid.NewGuid();
                        _mfaDataProvider.SetUserKey(user.Id.Value, userKey);
                    }
                    key = userKey.ToString().ToUpperInvariant();
                }
                _cache.Insert(GetUserKeyCacheKey(user), key);
            }
            return key;
        }
        public string GetAccountSecureKey(User user)
        {

            return GetAccountSecureKey(user, true);

        }

        private bool IsOldVersionUser(User user)
        {
#if SIMULATE_OLDMFA_KEY_VERSION
            return true;
#else
            var result = false;
            try
            {
                if (user != null)
                {
                    _usersService.RunAsUser(_usersService.ServiceUserName, () =>
                    {
                        var mfaVersionEA = user.ExtendedAttributes.Get(_eakey_mfaVersion);
                        var mfaEnabled = user.ExtendedAttributes.Get(_eakey_mfaEnabled);
                        //if user has no version stored and had mfa enabled, then it's old version user
                        result = (mfaVersionEA == null || string.IsNullOrEmpty(mfaVersionEA.Value.Trim()))
                                 && mfaEnabled != null && mfaEnabled.Value == "True";

                    });
                }
            }
            catch (Exception ex)
            {
                new Common.TelligentCommunity.Components.TCException($"Could not get user MFA version via EA '{_eakey_mfaVersion}'", ex).Log();
            }
            return result;
#endif
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
            controller.AddPage(_mfaPageName, _mfaPageName, new SiteRootRouteConstraint(), null, _mfaPageName, new PageDefinitionOptions
            {
                DefaultPageXml = _defaultMfaPageLayout,
                HasApplicationContext = false,
                Validate = ValidateNonAnonymous
            });

            controller.AddPage(_pageVerifyEmailName, _pageVerifyEmailName, new SiteRootRouteConstraint(), null, _pageVerifyEmailName, new PageDefinitionOptions
            {
                DefaultPageXml = _defaultVerifyEmailPageLayout,
                HasApplicationContext = false,
                Validate = (context, accessController) =>
                {
                    var user = _usersService.Get(new UsersGetOptions { Id = context.UserId });
                    if (_usersService.AnonymousUserName == user.Username &&
                        !string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["code"]) &&
                        !string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["userName"]))
                    {
                        var userValidation = _usersService.Get(new UsersGetOptions() { Username = HttpContext.Current.Request.QueryString["userName"] });

                        if (userValidation != null && !userValidation.HasErrors())
                        {
                            if (ValidateEmailCode(userValidation, HttpContext.Current.Request.QueryString["code"]))
                            {
                                accessController.Redirect(Apis.Get<ICoreUrls>().Home(false));
                            }

                        }
                    }

                    ValidateNonAnonymous(context, accessController);
                }
            });
        }


        private void ValidateNonAnonymous(PageContext context, IUrlAccessController accessController)
        {
            var user = _usersService.Get(new UsersGetOptions { Id = context.UserId });
            if (_usersService.AnonymousUserName == user.Username)
            {
                accessController.Redirect(Apis.Get<ICoreUrls>().LogIn());
            }
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
                string encryptedCode = plainTextCode.Hash(GetAccountSecureKey(user), user.Id.Value);
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
                    result.CodesLeft = _mfaDataProvider.CountCodesLeft(user.Id.Value);
                }

            });
            return result;
        }

    }
}
