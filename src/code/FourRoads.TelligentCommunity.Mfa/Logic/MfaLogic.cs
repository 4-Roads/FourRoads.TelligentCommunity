﻿using System;
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
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using IRoles = Telligent.Evolution.Extensibility.Api.Version1.IRoles;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;
using System.Xml;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Logic
{
    public class MfaLogic : IMfaLogic
    {
        private static readonly string _mfaPageName = "mfa";
        private static readonly string _pageVerifyEmailName = "verify-email";
        private static readonly string _pageRequireMfaName = "manage-mfa";

        private readonly IUsers _usersService;
        private readonly IUrl _urlService;
        private readonly IMfaDataProvider _mfaDataProvider;
        private IVerifyEmailProvider _emailProvider;
        private ISocketMessage _socketMessenger;
        private bool _enableEmailVerification;
        private byte[] _jwtSecret;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEncryptedCookieService _encryptedCookieService;
        private readonly Telligent.Evolution.Extensibility.Api.Version2.IPermissions _permissions;

        //{295391e2b78d4b7e8056868ae4fe8fb3}
        private static readonly string _defaultMfaPageLayout =
            $"<contentFragmentPage pageName=\"{_mfaPageName}\" isCustom=\"false\" layout=\"Content\" title=\"4 Roads - MFA Entry\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::295391e2b78d4b7e8056868ae4fe8fb3\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        private static readonly string _defaultVerifyEmailPageLayout =
            $"<contentFragmentPage pageName=\"{_pageVerifyEmailName}\" isCustom=\"false\" layout=\"Content\"  title=\"4 Roads - Verify Email\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::a8b6e56eac3246169d1727c84c17fd66\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

        private static readonly string _defaultRequireMfaPageLayout =
            $"<contentFragmentPage pageName=\"{_pageRequireMfaName}\" isCustom=\"false\" layout=\"Content\" title=\"4 Roads - Manage MFA\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::6d9264a6f6c4434c9d9954b87a865e57\" showHeader=\"True\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";

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
        private static readonly string _eakey_emailVerifiedDate = "___emailVerifiedDate"; 
        private static readonly string _eakey_emailVerifyCode = "_eakey_emailVerifyCode";
        private DateTime _emailValidationCutoffDate;
        private const string ImpersonatorV1114CookieName = "Impersonator";
        private const string ImpersonatorCookieName = ".te.u";
        private bool _isPersistent;
        private List<string> _fileStoreNames = new List<string>();
        private int _emailVerificationExpiry;
        private string[] _requiredRoles;

        public MfaLogic(IUsers usersService, IUrl urlService, IMfaDataProvider mfaDataProvider,
            IAuthenticationService authService, IEncryptedCookieService encryptedCookieService,
            Telligent.Evolution.Extensibility.Api.Version2.IPermissions permissions)
        {
            _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            _urlService = urlService ?? throw new ArgumentNullException(nameof(urlService));
            _mfaDataProvider = mfaDataProvider ?? throw new ArgumentNullException(nameof(mfaDataProvider));
            _authenticationService = authService ?? throw new ArgumentNullException(nameof(authService));
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            _encryptedCookieService =
                encryptedCookieService ?? throw new ArgumentNullException(nameof(encryptedCookieService));
        }

        public void Initialize(bool enableEmailVerification, IVerifyEmailProvider emailProvider,
            ISocketMessage socketMessenger, DateTime emailValidationCutoffDate, string jwtSecret, bool isPersistent, int emailVerificationExpiry, int[] requiredRoles)
        {
            _enableEmailVerification = enableEmailVerification;
            _emailProvider = emailProvider;
            _socketMessenger = socketMessenger;
            _emailValidationCutoffDate = emailValidationCutoffDate;
            _usersService.Events.AfterAuthenticate += EventsOnAfterAuthenticate;
            _jwtSecret = Encoding.UTF8.GetBytes(jwtSecret).Take(32).ToArray();
            _isPersistent = isPersistent;
            _fileStoreNames = PluginManager.Get<ISecuredCentralizedFileStore>().Select(fs => $"/{fs.FileStoreKey.ToLowerInvariant().Replace(".", "-")}/").ToList();

            _emailVerificationExpiry = emailVerificationExpiry;
            _requiredRoles = requiredRoles?.Select(rid => Apis.Get<IRoles>().Get(rid).Name).ToArray();
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
            if (IsUnprotectedRequest(request.HttpContext.Request)) return;

            if (ShouldRemoveMfaToken(request.HttpContext.Request))
            {
                RemoveMfaToken();
                return;
            }

            var user = _usersService.AccessingUser;

            Debug.Assert(user.Id != null, "user.Id != null");
            bool mfaEnabled = TwoFactorCheckAndSetState(user);
            if (mfaEnabled)
            {
                PayLoad? payload = null;
                var jwtCookie = request.HttpContext.Request.Cookies[GetMfaCookieName()];
                if (jwtCookie != null)
                {
                    payload = GetJwtPayload(jwtCookie.Value);
                }

                if (jwtCookie == null || ValidateJwtToken(user.Id.Value, payload) == false)
                {
                    var returnUrl = _urlService.Encode(request.HttpContext.Request.RawUrl);
                    ForceRedirect(request, "/mfa" + $"?ReturnUrl={returnUrl}");
                }

                EnsureJwtCookieExpirationMatchesAuthCookie(payload);
            }

            if (!_enableEmailVerification || !EmailVerificationOutOfDate(user) || !EmailChanged(user))
            {
                //To get here must have validated email and 
                if (!mfaEnabled)
                {
                    //Is this user in the role that requires MFA
                    if (UserRequiresMfa(user))
                    {
                        ForceRedirect(request, "/manage_mfa" + "?ReturnUrl=" + _urlService.Encode(request.HttpContext.Request.RawUrl));
                    }
                }

                return;
            }

            //Never validated and also joined before cutoff date so assumed a valid user
            if (user.JoinDate < _emailValidationCutoffDate &&
                string.IsNullOrWhiteSpace(user.ExtendedAttributes.Get(_eakey_emailVerified)?.Value))
            {
                SetEmailInExtendedAttributes(user);
                return;
            }

            ForceRedirect(request, "/verifyemail" + "?ReturnUrl=" + _urlService.Encode(request.HttpContext.Request.RawUrl));

            if (EmailNotSent(user))
            {
                SendValidationCode(user);
            }
        }


        public bool UserRequiresMfa(User user)
        {
            return Apis.Get<IRoleUsers>().IsUserInRoles(user.Username, _requiredRoles);
        }

        private bool ShouldRemoveMfaToken(HttpRequestBase request)
        {
            if (_usersService.AccessingUser.Username == _usersService.AnonymousUserName)
            {
                return true;
            }

            if (request.Url != null && request.Url.LocalPath.StartsWith("/logout"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the request is one we should not be intercepting with MFA check
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool IsUnprotectedRequest(HttpRequestBase request)
        {
            if (IsImpersonator(request)) return true;

            if (IsOauthRequest(request)) return true;

            if (IsPageRequest(request) == false && IsSecuredFileStoreRequest(request) == false) return true;

            if (request.Url != null && request.IsLocal &&
                request.Url.LocalPath.ToLower().EndsWith("/controlpanel/localaccess.aspx"))
                return true;

            return false;
        }

        private void EnsureJwtCookieExpirationMatchesAuthCookie(PayLoad? payLoad)
        {
            if (payLoad == null || !DateTime.TryParse(payLoad.Value.expires, out var jwtExpiration)) return;

            var authCookieExpiration = _authenticationService.GetAuthenticationCookieExpiration();

            //using 'sortable' datetime to avoid comparing down to milliseconds
            if (!authCookieExpiration.ToString("s").Equals(jwtExpiration.ToUniversalTime().ToString("s")))
            {
                SetTwoFactorState(payLoad.Value.userId, TwoFactorState.Passed);
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

        private void RemoveMfaToken()
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
                new ExtendedAttribute { Key = _eakey_emailVerifyCode, Value = string.Empty },
                new ExtendedAttribute { Key = _eakey_emailVerified, Value = user.PrivateEmail },
                new ExtendedAttribute { Key = _eakey_emailVerifiedDate, Value = DateTime.Now.ToShortDateString() }
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


        private bool EmailVerificationOutOfDate(User user)
        {
            if (_emailVerificationExpiry > 0)
            {
                string dateStr = user.ExtendedAttributes.Get(_eakey_emailVerifiedDate)?.Value;

                if (dateStr != null)
                {
                    DateTime date;

                    if (DateTime.TryParse(dateStr, out date))
                    {
                        if (date.Add(new TimeSpan(_emailVerificationExpiry, 0, 0, 0)) < DateTime.Now)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    //Edge case of user with no set value, set it today 
                    var attributes = new List<ExtendedAttribute>
                    {
                        new ExtendedAttribute
                            { Key = _eakey_emailVerifiedDate, Value = DateTime.Now.ToShortDateString() }
                    };

                    _usersService.Update(new UsersUpdateOptions()
                    {
                        Id = user.Id,
                        ExtendedAttributes = attributes
                    });
                }
            }

            return false;
        }

        private static bool EmailChanged(User user)
        {
            return (string.Compare(user.PrivateEmail, user.ExtendedAttributes.Get(_eakey_emailVerified)?.Value, StringComparison.OrdinalIgnoreCase) != 0);
        }

        private void ForceRedirect(IHttpRequest httpRequest, string pageUrl)
        {
            // user is logged in but has not completed the second auth stage
            var request = httpRequest.HttpContext.Request;

            if (request.Path.StartsWith("/socket.ashx"))
            {
                return;
            }

            var response = httpRequest.HttpContext.Response;

            // suppress any callbacks re search, notifications, header links etc
            if (
                request.Path.StartsWith("/api.ashx") ||
                request.Path.StartsWith("/oauth") ||
                (request.Url?.LocalPath == "/utility/scripted-file.ashx" &&
                 request.QueryString["_cf"] != null &&
                 request.QueryString["_cf"] != "logout.vm" &&
                 request.QueryString["_cf"] != "validate.vm" && request.QueryString["_cf"] != "newCode.vm"))
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
                request.Url?.LocalPath != "/manage_mfa" &&
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
                response.Redirect(pageUrl, force);
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

        private static bool IsOauthRequest(HttpRequestBase request)
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
            return _authenticationService != null
                ? AuthenticationService.AuthCookieName
                : FormsAuthentication.FormsCookieName;
        }

        private string GetMfaCookieName()
        {
            return $"{GetAuthCookieName()}Mfa{_mfaLogicVersion}{_mfaLogicMinorVersion}";
        }

        private string GetJwtCookie(HttpContextBase context)
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
            SetTwoFactorState(user.Id.Value, twoFactorState);
        }

        private void SetTwoFactorState(int userId, TwoFactorState twoFactorState)
        {
            var mfaCookieName = GetMfaCookieName();
            var payload = new Dictionary<string, object>
            {
                { nameof(PayLoad.userId), userId },
                { nameof(PayLoad.state), twoFactorState },
            };
            var expiration = GetMfaCookieExpirationDate();
            if (expiration.HasValue)
            {
                payload.Add(nameof(PayLoad.expires), expiration.Value.ToString("O"));
            }

            var token = CreateJoseJwtToken(payload);

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
            if (authCookie != null)
            {
                return FormsAuthentication.Decrypt(authCookie.Value)?.Expiration;
            }

            //use new v12.1 service to read '.te.auth' cookie
            var telligentAuthCookie = _authenticationService?.GetAuthenticationCookie();
            return telligentAuthCookie?.ExpirationDate;
        }

        private bool IsTwoFactorEnabled(int userId)
        {
            var isEnabled = false;
            _usersService.RunAsUser(_usersService.ServiceUserName, () =>
            {
                var user = _usersService.Get(new UsersGetOptions { Id = userId });
                if (user != null && !user.HasErrors())
                {
                    var mfaEnabled = user.ExtendedAttributes.Get(_eakey_mfaEnabled);
                    if (mfaEnabled != null)
                    {
                        bool.TryParse(mfaEnabled.Value, out isEnabled);
                    }
                }
            });
            return isEnabled;
        }

        public bool IsTwoFactorEnabled(User user)
        {
            return user?.Id != null && IsTwoFactorEnabled(user.Id.Value);
        }

        private bool IsImpersonator()
        {
            return IsImpersonator(HttpContext.Current.Request);
        }

        private bool IsImpersonator(HttpRequest request)
        {
            return IsImpersonator(new HttpRequestWrapper(request));
        }

        private bool IsImpersonator(HttpRequestBase request)
        {
            var cookieValues = _encryptedCookieService.GetCookieValues(CookieUtility.UserCookieName);
            if (cookieValues == null) return false;

            var token = cookieValues["PrivateToken"];
            var impersonating = cookieValues["Impersonating"];
            if (int.TryParse(cookieValues["UserID"], out var userId)
                && !string.IsNullOrEmpty(token)
                && !string.IsNullOrEmpty(impersonating)
               )
            {
                var user = _usersService.Get(new UsersGetOptions { Id = userId });
                if (user != null && !user.HasErrors())
                {
                    return _permissions.CheckPermission(SitePermission.ImpersonateUser, userId).IsAllowed;
                }

                return false;
            }

            //pre 12.1 check
            var cookie = request.Cookies[ImpersonatorCookieName];
            return cookie != null
                ? HasImpersonatorFlag(cookie)
                : HasOldImpersonatorFlag(request.Cookies[ImpersonatorV1114CookieName]);
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

            var tfa = new FourRoadsTwoFactorAuthenticator();

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

        private PayLoad? GetJwtPayload(string sessionToken)
        {
            PayLoad payload;
            try
            {
                payload = Jose.JWT.Decode<PayLoad>(sessionToken, _jwtSecret, Jose.JweAlgorithm.A256KW,
                    Jose.JweEncryption.A128CBC_HS256);
            }
            catch (Exception)
            {
                return null;
            }

            return payload;
        }

        private bool ValidateJwtToken(int userId, PayLoad? payload)
        {
            if (!payload.HasValue || payload.Value.userId != userId) return false;

            var state = IsTwoFactorEnabled(userId) ? TwoFactorState.Passed : TwoFactorState.NotEnabled;
            return payload.Value.state == state;
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_mfaPageName, "mfa", new SiteRootRouteConstraint(), null, "MFA",
                new PageDefinitionOptions
                {
                    DefaultPageXml = _defaultMfaPageLayout,
                    HasApplicationContext = false,
                    Validate = ValidateNonAnonymous
                });

            controller.AddPage(_pageVerifyEmailName, "verifyemail", new SiteRootRouteConstraint(), null,
                "Advanced Verify Email", new PageDefinitionOptions
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

            controller.AddPage(_pageRequireMfaName, "manage_mfa", new SiteRootRouteConstraint(), null,
                "Manage MFA Settings", new PageDefinitionOptions
                {
                    DefaultPageXml = _defaultRequireMfaPageLayout,
                    HasApplicationContext = false,
                    Validate = ValidateNonAnonymous
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

        public void Install()
        {
            //Install the pages and also add the settings widget to the settings page

            void InstallPage(string xmlString)
            {
                try
                {
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(xmlString);
                    XmlNode node = xml.SelectSingleNode("//contentFragmentPage");

                    if (node == null || node.Attributes == null)
                        return;

                    string pageName = node.Attributes["pageName"].Value;
                    Guid themeType = Guid.Parse("0c647246-6735-42f9-875d-c8b991fe739b");

                    if (string.IsNullOrEmpty(pageName))
                        return;

                    foreach (Theme theme in Themes.List(themeType))
                    {
                        if (theme != null)
                        {
                            if (theme.IsConfigurationBased)
                            {
                                ThemePages.AddUpdateFactoryDefault(theme, node);
                                ThemePages.DeleteDefault(theme, pageName, true);
                            }
                            else
                            {
                                ThemePages.AddUpdateDefault(theme, node);
                            }

                            ThemePages.Delete(theme, pageName, true);
                        }
                    }
                }
                catch (Exception exception)
                {

                    new Exception($"Couldn't load page from {xmlString}.", exception);
                }
            }

            InstallPage(_defaultMfaPageLayout);
            InstallPage(_defaultVerifyEmailPageLayout);
            InstallPage(_defaultRequireMfaPageLayout);

            //Update the settings page to include 
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                if (ThemePages.FactoryDefaultExists(theme, "user-edituser", false))
                {
                    var fragments = ThemePageContentFragments.ListFactoryDefault(theme, "user-edituser", false);

                    var findFrag = GetScriptedFragmentName("3317de4f74eb434da129a95a41aebc5b");

                    if (fragments.All(f => f.ContentFragmentType != findFrag))
                    {
                        ThemePageContentFragments.InsertInFactoryDefault(theme, "user-edituser", false, GetScriptedFragmentName("af9000b6cc8c4f658c2046ffdc09c5db"), ContentFragmentPlacement.After, findFrag, "fragmentHeader=%24%7Bresource%3AContentDiscuss_Header%7D&amp;textAreaSelector=.blog-post%20.post-content.user-defined-markup", "full-border with-header");
                    }
                }
            }
        }

        private const string _leaderAssemblyName = "Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::{0}";

        protected string GetScriptedFragmentName(string id)
        {
            return string.Format(_leaderAssemblyName, id);
        }

        private struct PayLoad
        {
            //props set in JWT decoder
            //prop names match claims
            // ReSharper disable InconsistentNaming
#pragma warning disable 648,649
            public int userId;

            public TwoFactorState state;

            //ISO 8601 format, use ToString("O")
            public string expires;
#pragma warning restore 648,649
            // ReSharper restore InconsistentNaming
        }
    }
}