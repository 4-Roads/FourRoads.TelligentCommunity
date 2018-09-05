using System;
using System.Collections.Concurrent;
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
                    if (request.Path.StartsWith("/api.ashx") ||
                        request.Path.StartsWith("/oauth") ||
                        (request.Url.LocalPath == "/utility/scripted-file.ashx" &&
                        request.QueryString["_cf"] != null &&
                        request.QueryString["_cf"] != "logout.vm" &&
                        request.QueryString["_cf"] != "validate.vm"))
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
                         request.CurrentExecutionFilePathExtension == string.Empty))
                    {
                        //redirect to 2 factor page
                        response.Redirect("/mfa" + "?ReturnUrl=" + _urlService.Encode(request.RawUrl), true);
                    }
                }
            }
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
            //WARNING: Non API Safe code
            if (CSContext.Current.Impersonator != null)
            {
                return false;
            }

            bool require2F = false;

            //ensure we have access to user.ExtendedAttributes
            Apis.Get<IUsers>().RunAsUser("admin", () =>
            {
                var mfaEnabled = user.ExtendedAttributes.Get("__mfaEnabled");

                if (mfaEnabled != null)
                {
                    bool.TryParse(mfaEnabled.Value, out require2F);
                }
            });

            return require2F;
        }

        public void EnableTwoFactor(User user, bool enabled)
        {
            //ensure we have access to user.ExtendedAttributes
            Apis.Get<IUsers>().RunAsUser("admin", () =>
            {
                UsersUpdateOptions updateOptions = new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = user.ExtendedAttributes };

                updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = "__mfaEnabled", Value = enabled.ToString() });

                _usersService.Update(updateOptions);
            });
        }

        public bool ValidateTwoFactorCode(User user, string code)
        {
            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();

            if (tfa.ValidateTwoFactorPIN(user.ContentId.ToString(), code))
            {
                SetTwoFactorState(user, true);

                return true;
            }

            return false;
        }

        private string GetCacheKey(User user)
        {
            return $"MFA:CACHE:{user.Id}";
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
            }
            );
        }
    }
}
