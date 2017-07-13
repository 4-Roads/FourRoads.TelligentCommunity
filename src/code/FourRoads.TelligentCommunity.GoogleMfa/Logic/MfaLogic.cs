using System;
using System.Web;
using FourRoads.TelligentCommunity.GoogleMfa.Interfaces;
using Google.Authenticator;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Urls.Routing;

namespace FourRoads.TelligentCommunity.GoogleMfa.Logic
{
    public class MfaLogic
        : IMfaLogic
    {
        private static readonly string _pageName = "mfa";
        private readonly IUsers _usersService;
        private readonly IUrl _urlService;
        //{295391e2b78d4b7e8056868ae4fe8fb3}
        private static readonly string _defaultPageLayout = " <contentFragmentPage pageName=\"mfa\" isCustom=\"false\" layout=\"Content\">\r\n      <regions>\r\n        <region regionName=\"Content\">\r\n          <contentFragments>\r\n            <contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::295391e2b78d4b7e8056868ae4fe8fb3\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n          </contentFragments>\r\n        </region>\r\n      </regions>\r\n    </contentFragmentPage>";
        public MfaLogic(IUsers usersService, IUrl urlService)
        {
            _usersService = usersService;
            _urlService = urlService;

            _usersService.Events.AfterIdentify += EventsAfterIdentify;
            _usersService.Events.AfterAuthenticate += EventsOnAfterAuthenticate;
        }

        private void EventsOnAfterAuthenticate(UserAfterAuthenticateEventArgs userAfterAuthenticateEventArgs)
        {
            //user has authenticated
            //is 2 factor enabled for user?
            var user = _usersService.Get(new UsersGetOptions() {Username = userAfterAuthenticateEventArgs.Username});
            if (TwoFactorEnabled(user))
            {
                //Yes set flag to false
                SetTwoFactorState(user, false);
            }
            else
            {
                //no set flag to true
                SetTwoFactorState(user, true);
            }
        }

        protected void EventsAfterIdentify(UserAfterIdentifyEventArgs e)
        {
            var user = _usersService.AccessingUser;
            if (user.Username != _usersService.AnonymousUserName)
            {
                var reqeust = HttpContext.Current.Request;
                var response = HttpContext.Current.Response;

                if (response.ContentType == "text/html" &&
                    !reqeust.Path.StartsWith("/tinymce") &&
                    string.Compare(HttpContext.Current.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) == 0 && //Don't do post backs to avoid issues with callbacks etc
                    (reqeust.CurrentExecutionFilePathExtension == ".aspx" || reqeust.CurrentExecutionFilePathExtension == ".htm" || reqeust.CurrentExecutionFilePathExtension == string.Empty))
                {
                    //Requires 2nd factor
                    if (TwoFactorEnabled(user) && TwoFactorState(user) == false && reqeust.Url.LocalPath != "/mfa")
                    {
                        //redirect to 2 factor page
                        response.Redirect("/mfa" + "?ReturnUrl=" + _urlService.Encode(reqeust.RawUrl), true);
                    }
                }
            }
        }

        private void SetTwoFactorState(User user, bool passed)
        {
            UsersUpdateOptions updateOptions = new UsersUpdateOptions() {Id = user.Id, ExtendedAttributes = user.ExtendedAttributes};

            updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() {Key = "__mfaState", Value = passed.ToString()});

            _usersService.Update(updateOptions);
        }

        public bool TwoFactorEnabled(User user)
        {
            bool require2F = false;

            var mfaEnabled = user.ExtendedAttributes.Get("__mfaEnabled");

            if (mfaEnabled != null)
            {
                bool.TryParse(mfaEnabled.Value, out require2F);
            }

            return require2F;
        }

        public void EnableTwoFactor(User user, bool enabled)
        {
            UsersUpdateOptions updateOptions = new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = user.ExtendedAttributes };

            updateOptions.ExtendedAttributes.Add(new ExtendedAttribute() { Key = "__mfaEnabled", Value = enabled.ToString() });

            _usersService.Update(updateOptions);
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

        private bool TwoFactorState(User user)
        {
            bool state = false;

            var mfaState = user.ExtendedAttributes.Get("__mfaState");

            if (mfaState != null)
            {
                bool.TryParse(mfaState.Value, out state);
            }

            return state;
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_pageName,_pageName,new SiteRootRouteConstraint(),null,_pageName,new PageDefinitionOptions
                { 
                    DefaultPageXml = _defaultPageLayout,
                    Validate = (context, accessController) =>
                    {
                        if (_usersService.AccessingUser != null)
                        {
                            if (_usersService.AnonymousUserName == _usersService.AccessingUser.Username)
                            {
                                accessController.AccessDenied("This page is not availalble to you" , false);
                            }
                        }
                    }
                }
            );
        }
    }
}
