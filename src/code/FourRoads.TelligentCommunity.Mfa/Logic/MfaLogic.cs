using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using FourRoads.Common.TelligentCommunity.Routing;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Model;
using Google.Authenticator;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;

namespace FourRoads.TelligentCommunity.Mfa.Logic
{
    public class MfaLogic : IMfaLogic
    {
        private static readonly string _mfaPageName = "mfa";
        private static readonly string _pageVerifyEmailName = "verifyemail";
        private readonly IUsers _usersService;
        private readonly IUrl _urlService;
        private readonly IMfaDataProvider _mfaDataProvider;
        private IVerifyEmailProvider _emailProvider;
        private ISocketMessage _socketMessenger;
        private bool _enableEmailVerification;
        private byte[] _jwtSecret;

        //{295391e2b78d4b7e8056868ae4fe8fb3}
        private static readonly string _defaultMfaPageLayout =
            $"<contentFragmentPage pageName=\"{_mfaPageName}\" isCustom=\"false\" layout=\"Content\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::295391e2b78d4b7e8056868ae4fe8fb3\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        private static readonly string _defaultVerifyEmailPageLayout =
            $"<contentFragmentPage pageName=\"{_pageVerifyEmailName}\" isCustom=\"false\" layout=\"Content\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::a8b6e56eac3246169d1727c84c17fd66\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        //this is version flag to distinguish major changes in MFA logic, so we can tell users if they should regenerate their keys
        private static readonly int _mfaLogicVersion = 2;
        private static readonly int _mfaLogicMinorVersion = 1;

        private static readonly int _oneTimeCodesToGenerate = 10;

        //Plaintext length of the code with any spaces removed
        private static readonly int _oneTimeCodeLength = 8;
        private static readonly string _eakey_mfaEnabled = "__mfaEnabled";
        private static readonly string _eakey_codesGeneratedOnUtc = "__mfaCodesGeneratedOnUtc";
        private static readonly string _eakey_mfaVersion = "__mfaVersion";
        private static readonly string _eakey_emailVerified = "___emailVerified";
        private static readonly string _eakey_emailVerifyCode = "_eakey_emailVerifyCode";
        private DateTime _emailValidationCutoffDate;
        private const string ImpersonatorV1114CookieName = "Impersonator";
        private const string ImpersonatorCookieName = "te.u";
        private bool _isPersistent;
        private List<string> _fileStoreNames = new List<string>();


        public MfaLogic(IUsers usersService, IUrl urlService, IMfaDataProvider mfaDataProvider)
        {
            _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            _urlService = urlService ?? throw new ArgumentNullException(nameof(urlService));
            _mfaDataProvider = mfaDataProvider ?? throw new ArgumentNullException(nameof(mfaDataProvider));
        }

        public void Initialize(bool enableEmailVerification, IVerifyEmailProvider emailProvider,
            ISocketMessage socketMessenger, DateTime emailValidationCutoffDate, string jwtSecret, bool isPersistent)
        {
            _enableEmailVerification = enableEmailVerification;
            _emailProvider = emailProvider;
            _socketMessenger = socketMessenger;
            _emailValidationCutoffDate = emailValidationCutoffDate;
            _usersService.Events.AfterAuthenticate += EventsOnAfterAuthenticate;
            _jwtSecret = Encoding.UTF8.GetBytes(jwtSecret).Take(32).ToArray();
            _isPersistent = isPersistent;
            _fileStoreNames = PluginManager.Get<ISecuredCentralizedFileStore>()
                .Select(fs => $"/{fs.FileStoreKey.ToLowerInvariant().Replace(".", "-")}/").ToList();
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

            if (IsImpersonator())
                return;

            TwoFactorCheckAndSetState(user);
        }

        private bool TwoFactorCheckAndSetState(User user)
        {
            if (IsTwoFactorEnabled(user))
            {
                var request = HttpContext.Current.Request;
                if (request.IsLocal && request.Url.LocalPath.ToLower().EndsWith("/controlpanel/localaccess.aspx"))
                {
                    // Bypass mfa for emergency local access
                    SetTwoFactorState(user, TwoFactorState.Passed);
                }

                return true;
            }

            SetTwoFactorState(user, TwoFactorState.NotEnabled);
            return false;
        }

        /// <summary>
        /// Intercept requests and trap when a user has logged in 
        /// but still needs to perform the second auth stage. 
        /// At this point the user is technically authenticated with telligent 
        /// so we also need to suppress any callbacks etc whilst the second stage 
        /// auth is being performed.
        /// </summary>
        public void FilterRequest(IHttpRequest request)
        {
            if (IsImpersonator(request.HttpContext.Request)
                || (!IsPageRequest(request.HttpContext.Request)
                    && !IsSecuredFileStoreRequest(request.HttpContext.Request)
                )
               )
                return;

            var user = _usersService.AccessingUser;

            if (user.Username == _usersService.AnonymousUserName)
                return;

            if (!(request.HttpContext.Request.Url is null) &&
                request.HttpContext.Request.Url.LocalPath.StartsWith("/logout"))
            {
                RemoveTwoFactorState();
                return;
            }

            if (TwoFactorCheckAndSetState(user))
            {
                var jwt = GetJwt(request.HttpContext);

                if (GetTwoFactorState(user, jwt) == false)
                {
                    ForceRedirect(request,
                        "/mfa" + "?ReturnUrl=" + _urlService.Encode(request.HttpContext.Request.RawUrl));
                }
            }

            if (!_enableEmailVerification || !EmailChanged(user))
                return;

            //Never validated and also joined before cutoff date so assumed a valid user
            if (user.JoinDate < _emailValidationCutoffDate &&
                string.IsNullOrWhiteSpace(user.ExtendedAttributes.Get(_eakey_emailVerified)?.Value))
            {
                SetEmailInExtendedAttributes(user);
                return;
            }

            ForceRedirect(request,
                "/verifyemail" + "?ReturnUrl=" + _urlService.Encode(request.HttpContext.Request.RawUrl));

            if (EmailNotSent(user))
            {
                SendValidationCode(user);
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
                    if (string.CompareOrdinal(user.ExtendedAttributes.Get(_eakey_emailVerifyCode)?.Value, code) == 0)
                    {
                        SetEmailInExtendedAttributes(user);
                        result = true;
                    }
                }
            });

            if (result)
            {
                //Now send a socket message bus response so the current page refreshes
                _socketMessenger.NotifyCodeAccepted(user);
            }

            return result;
        }

        private void RemoveTwoFactorState()
        {
            HttpContext.Current.Response.Cookies.Add(new HttpCookie(GetMfaCookieName())
            {
                Expires = DateTime.UtcNow.AddDays(-7)
            });
        }

        private void SetEmailInExtendedAttributes(User user)
        {
            var attributes = new List<ExtendedAttribute>
            {
                new ExtendedAttribute() { Key = _eakey_emailVerifyCode, Value = string.Empty },
                new ExtendedAttribute() { Key = _eakey_emailVerified, Value = user.PrivateEmail }
            };

            _usersService.Update(new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = attributes });
        }

        public bool SendValidationCode(User user)
        {
            var code = MfaCryptoExtension.RandomAlphanumeric(6);
            //Send an email
            _emailProvider.SendEmail(user, code);

            //Send a validation code, store it in the users profile extended attributes
            var attributes = new List<ExtendedAttribute>
            {
                new ExtendedAttribute { Key = _eakey_emailVerifyCode, Value = code }
            };

            return _usersService.Update(new UsersUpdateOptions()
                {
                    Id = user.Id, ExtendedAttributes = attributes
                })
                .HasErrors();
        }

        private static bool EmailNotSent(User user)
        {
            return string.IsNullOrWhiteSpace(user.ExtendedAttributes.Get(_eakey_emailVerifyCode)?.Value);
        }

        private static bool EmailChanged(User user)
        {
            return (string.Compare(user.PrivateEmail, user.ExtendedAttributes.Get(_eakey_emailVerified)?.Value,
                StringComparison.OrdinalIgnoreCase) != 0);
        }

        private void ForceRedirect(IHttpRequest httpRequest, string page)
        {
            // user is logged in but has not completed the second auth stage
            var request = httpRequest.HttpContext.Request;

            if (request.Path.StartsWith("/socket.ashx"))
            {
                return;
            }

            var response = httpRequest.HttpContext.Response;

            // suppress any callbacks re search, notifications, header links etc
            if (IsOauthRequest(request) == false &&
                (request.Path.StartsWith("/api.ashx") ||
                 request.Path.StartsWith("/oauth") ||
                 (request.Url?.LocalPath == "/utility/scripted-file.ashx" &&
                  request.QueryString["_cf"] != null &&
                  request.QueryString["_cf"] != "logout.vm" &&
                  request.QueryString["_cf"] != "validate.vm" && request.QueryString["_cf"] != "newCode.vm")))
            {
                // this should only happen when in the second auth stage 
                // for blocked callbacks so a bit brutal
                response.Clear();

                httpRequest.HttpContext.ApplicationInstance.CompleteRequest();

                return;
            }

            // is it a suitable time to redirect the user to the second auth page
            if (response.ContentType == "text/html" &&
                !request.Path.StartsWith("/tinymce") &&
                request.Url?.LocalPath != "/logout" &&
                request.Url?.LocalPath != "/mfa" &&
                request.Url?.LocalPath != "/user/changepassword" &&
                request.Url?.LocalPath != "/verifyemail" &&
                string.Compare(httpRequest.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) ==
                0 &&
                //Is this a main page and not a callback etc 
                (IsPageRequest(request) || IsSecuredFileStoreRequest(request)))
            {
                //redirect to 2 factor page
                // ReSharper disable ConditionIsAlwaysTrueOrFalse
                var force = httpRequest.HttpContext.ApplicationInstance == null;
                response.Redirect(page, force);
                if (!force)
                {
                    httpRequest.HttpContext.ApplicationInstance.CompleteRequest();
                }
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
            }
        }

        private static bool IsPageRequest(HttpRequestBase request)
        {
            return (request.CurrentExecutionFilePathExtension == ".aspx" ||
                    request.CurrentExecutionFilePathExtension == ".htm" ||
                    request.CurrentExecutionFilePathExtension == ".ashx" ||
                    request.CurrentExecutionFilePathExtension == string.Empty);
        }

        private bool IsSecuredFileStoreRequest(HttpRequestBase request)
        {
            return request.Url != null && _fileStoreNames.Any(u => request.Url.AbsolutePath.Contains(u));
        }

        private bool IsOauthRequest(HttpRequestBase request)
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

        private string GetAuthCookieName()
        {
            return FormsAuthentication.FormsCookieName;
        }

        private string GetMfaCookieName()
        {
            return $"{GetAuthCookieName()}Mfa{_mfaLogicVersion}{_mfaLogicMinorVersion}";
        }

        private string GetJwt(HttpContextBase context)
        {
            var cookie = context.Request.Cookies[GetMfaCookieName()];

            return cookie != null ? cookie.Value : string.Empty;
        }

        private enum TwoFactorState
        {
            NotEnabled,
            Passed
        }

        private void SetTwoFactorState(User user, TwoFactorState twoFactorState)
        {
            Debug.Assert(user.Id != null, "user.Id != null");
            var payload = new Dictionary<string, object>
            {
                { nameof(PayLoad.userId), user.Id.Value },
                { nameof(PayLoad.state), twoFactorState },
            };
            var mfaCookieName = GetMfaCookieName();
            var token = CreateJoseJwtToken(payload);
            var expiration = GetMfaCookieExpirationDate();

            var mfaCookie = new HttpCookie(mfaCookieName)
            {
                Value = token,
                HttpOnly = true,
                Secure = true
            };

            if (expiration.HasValue)
            {
                mfaCookie.Expires = expiration.Value;
            }

            HttpContext.Current.Response.Cookies.Add(mfaCookie);
        }

        private DateTime? GetMfaCookieExpirationDate()
        {
            //do not set expiration of configured to use session cookie
            if (_isPersistent == false) return null;

            //decrypt FormsAuthentication cookie to get its expiration date and time
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            return authCookie == null ? null : FormsAuthentication.Decrypt(authCookie.Value)?.Expiration;
        }

        public bool IsTwoFactorEnabled(User user)
        {
            var require2F = false;
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

        private static bool IsImpersonator()
        {
            return IsImpersonator(HttpContext.Current.Request);
        }

        private static bool IsImpersonator(HttpRequest request)
        {
            return IsImpersonator(new HttpRequestWrapper(request));
        }

        private static bool IsImpersonator(HttpRequestBase request)
        {
            bool result;
            var cookie = request.Cookies[ImpersonatorCookieName];
            if (cookie != null)
            {
                result = HasImpersonatorFlag(cookie);
            }
            else
            {
                cookie = request.Cookies[ImpersonatorV1114CookieName];
                result = HasOldImpersonatorFlag(cookie);
            }

            return result;
        }

        /// <summary>
        /// uses the old way of storing impersonator flag.
        /// Versions 11.1.4 and below.
        /// </summary>
        /// <param name="httpCookie"></param>
        /// <returns></returns>
        private static bool HasOldImpersonatorFlag(HttpCookie httpCookie)
        {
            //just checking for existence of the cookie
            return (httpCookie != null && !string.IsNullOrEmpty(httpCookie.Value));
        }

        /// <summary>
        /// uses the new way of storing impersonator cookie, which now gets encrypted
        /// Versions 11.1.6 and up
        /// </summary>
        /// <param name="httpCookie"></param>
        /// <returns></returns>
        private static bool HasImpersonatorFlag(HttpCookie httpCookie)
        {
            if (httpCookie == null || string.IsNullOrWhiteSpace(httpCookie.Value)) return false;
            try
            {
                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(httpCookie.Value);
                return ticket != null && ticket.UserData.Contains("impersonating=");
            }
            catch
            {
                return false;
            }
        }

        public void EnableTwoFactor(User user, bool enabled)
        {
            //ensure we have access to user.ExtendedAttributes
            _usersService.RunAsUser(_usersService.ServiceUserName, () =>
            {
                var updateOptions = new UsersUpdateOptions
                {
                    Id = user.Id, 
                    ExtendedAttributes = user.ExtendedAttributes
                };

                if (enabled == false)
                {
                    //old codes should be deleted
                    Debug.Assert(user.Id != null, "user.Id != null");
                    _mfaDataProvider.ClearCodes(user.Id.Value);
                    _mfaDataProvider.ClearUserKey(user.Id.Value);
                    //remove version number
#if !SIMULATE_OLDMFA_KEY_VERSION
                    updateOptions.ExtendedAttributes.Add(new ExtendedAttribute()
                        { Key = _eakey_mfaVersion, Value = string.Empty });
#endif
                }
                else
                {
#if !SIMULATE_OLDMFA_KEY_VERSION
                    //store plugin version in EA
                    updateOptions.ExtendedAttributes.Add(new ExtendedAttribute()
                        { Key = _eakey_mfaVersion, Value = _mfaLogicVersion.ToString(CultureInfo.InvariantCulture) });
#endif
                }

                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute()
                    { Key = _eakey_mfaEnabled, Value = enabled.ToString() });
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
                    SetTwoFactorState(user, TwoFactorState.Passed);
                    return true;
                }
            }

            var tfa = new TwoFactorAuthenticator();

            if (!tfa.ValidateTwoFactorPIN(GetAccountSecureKey(user), code)) return false;

            SetTwoFactorState(user, TwoFactorState.Passed);
            return true;
        }

        private bool ValidateOneTimeCode(User user, string code)
        {
            Debug.Assert(user.Id != null, "user.Id != null");
            return _mfaDataProvider.RedeemCode(user.Id.Value, code.Hash(GetAccountSecureKey(user), user.Id.Value));
        }

        public string GetAccountSecureKey(User user)
        {
            string key;

            if (IsOldVersionUser(user))
            {
                key = user.ContentId.ToString();
            }
            else
            {
                Debug.Assert(user.Id != null, "user.Id != null");
                var userKey = _mfaDataProvider.GetUserKey(user.Id.Value);
                if (userKey == Guid.Empty)
                {
                    userKey = Guid.NewGuid();
                    _mfaDataProvider.SetUserKey(user.Id.Value, userKey);
                }

                key = userKey.ToString().ToUpperInvariant();
            }

            return key;
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
                        var extendedAttribute = user.ExtendedAttributes.Get(_eakey_mfaVersion);
                        var mfaEnabled = user.ExtendedAttributes.Get(_eakey_mfaEnabled);
                        //if user has no version stored and had mfa enabled, then it's old version user
                        result = (extendedAttribute == null || string.IsNullOrEmpty(extendedAttribute.Value.Trim()))
                                 && mfaEnabled != null && mfaEnabled.Value == "True";
                    });
                }
            }
            catch (Exception ex)
            {
                new Common.TelligentCommunity.Components.TCException(
                    $"Could not get user MFA version via EA '{_eakey_mfaVersion}'", ex).Log();
            }

            return result;
#endif
        }

        private string CreateJoseJwtToken(Dictionary<string, object> payload)
        {
            var token = Jose.JWT.Encode(payload, _jwtSecret, Jose.JweAlgorithm.A256KW,
                Jose.JweEncryption.A128CBC_HS256);
            return token;
        }

        private bool GetTwoFactorState(User user, string token)
        {
            Debug.Assert(user.Id != null, "user.Id != null");
            return ValidateJwtToken(user.Id.Value, token) ?? false;
        }

        private bool? ValidateJwtToken(int userId, string sessionToken)
        {
            PayLoad payload;
            try
            {
                payload = Jose.JWT.Decode<PayLoad>(sessionToken, _jwtSecret, Jose.JweAlgorithm.A256KW,
                    Jose.JweEncryption.A128CBC_HS256);
            }
            catch (Exception)
            {
                return false;
            }

            if (payload.state == TwoFactorState.NotEnabled) return true;

            return payload.userId == userId && payload.state == TwoFactorState.Passed;
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_mfaPageName, _mfaPageName, new SiteRootRouteConstraint(), null, _mfaPageName,
                new PageDefinitionOptions
                {
                    DefaultPageXml = _defaultMfaPageLayout,
                    HasApplicationContext = false,
                    Validate = ValidateNonAnonymous
                });

            controller.AddPage(_pageVerifyEmailName, _pageVerifyEmailName, new SiteRootRouteConstraint(), null,
                _pageVerifyEmailName, new PageDefinitionOptions
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
                            var userValidation = _usersService.Get(new UsersGetOptions()
                                { Username = HttpContext.Current.Request.QueryString["userName"] });

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
            Debug.Assert(user.Id != null, "user.Id != null");
            _mfaDataProvider.ClearCodes(user.Id.Value);

            var codes = new List<OneTimeCode>(_oneTimeCodesToGenerate);

            for (var i = 0; i < _oneTimeCodesToGenerate; i++)
            {
                //generating the code in form of XXXX XXXX with zero-padding
                var plainTextCode =
                    $"{MfaCryptoExtension.RandomInteger(0, 9999):D4} {MfaCryptoExtension.RandomInteger(0, 9999):D4}";
                var encryptedCode = plainTextCode.Hash(GetAccountSecureKey(user), user.Id.Value);
                //we store just the hash value of the code, but...
                var code = _mfaDataProvider.CreateCode(user.Id.Value, encryptedCode);
                //..but return plain text code so users could see and print/save them
                code.PlainTextCode = plainTextCode;

                codes.Add(code);
            }

            // add note about time when codes were generated
            _usersService.RunAsUser(_usersService.ServiceUserName, () =>
            {
                UsersUpdateOptions updateOptions = new UsersUpdateOptions()
                    { Id = user.Id.Value, ExtendedAttributes = user.ExtendedAttributes };
                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute()
                    { Key = _eakey_codesGeneratedOnUtc, Value = DateTime.UtcNow.ToString("O") });
                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute()
                    { Key = _eakey_mfaVersion, Value = _mfaLogicVersion.ToString(CultureInfo.InvariantCulture) });
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
                
                if (codesGenerated == null) return;
                
                if (DateTime.TryParse(codesGenerated.Value, out DateTime generatedOnUtc))
                {
                    result.CodesGeneratedOn = generatedOnUtc;
                }

                Debug.Assert(user.Id != null, "user.Id != null");
                result.CodesLeft = _mfaDataProvider.CountCodesLeft(user.Id.Value);
            });
            return result;
        }

        private struct PayLoad
        {
            //props set in JWT decoder
            //prop names match claims
            // ReSharper disable InconsistentNaming
#pragma warning disable 648,649
            public int userId;
            public TwoFactorState state;
#pragma warning restore 648,649
            // ReSharper restore InconsistentNaming
        }
    }
}