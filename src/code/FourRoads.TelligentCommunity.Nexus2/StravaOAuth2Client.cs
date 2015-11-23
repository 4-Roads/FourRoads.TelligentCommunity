using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Remoting.Channels;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json.Linq;
using Telligent.Common;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls.PropertyRules;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Authentication.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.OAuth.Version2;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;

namespace FourRoads.TelligentCommunity.Nexus2
{
    public class StravaOAuth2Client : IOAuthClient, IRequiredConfigurationPlugin , IPluginGroup , ITranslatablePlugin
    {
        private readonly IOAuth _oauth;
        private readonly IOAuthDataProvider _oauthDataProvider;
        private string _callbackUrl;
        private ITranslatablePluginController _translatablePluginController;

        public StravaOAuth2Client()
        {
            CreateUser = false;
            Link = false;
            _oauth = Services.Get<IOAuth>();
            _oauthDataProvider = Services.Get<IOAuthDataProvider>();
        }

        public virtual string AuthorizeBaseUrl
        {
            get
            {
                if (Configuration != null)
                    return Configuration.GetString("AuthorizeBaseUrl");

                return string.Empty;
            }
        }

        public virtual string AccessTokenUrl
        {
            get
            {
                if (Configuration != null)
                    return Configuration.GetString("AccessTokenUrl");

                return string.Empty;
            }
        }

        public virtual string FileStoreKey
        {
            get { return "oauthimages"; }
        }

        public string RefreshTokenUrl
        {
            get { return string.Empty; }
        }

        public bool CreateUser { get; set; }

        public bool Link { get; set; }

        protected IPluginConfiguration Configuration { get; private set; }

        public string[] Categories
        {
            get
            {
                return new string[1]
                {
                    "OAuth"
                };
            }
        }

        public virtual string ConsumerKey
        {
            get
            {
                if (Configuration != null)
                    return Configuration.GetString("ConsumerKey");

                return string.Empty;
            }
        }

        public virtual string ConsumerSecret
        {
            get
            {
                if (Configuration != null)
                    return Configuration.GetString("ConsumerSecret");

                return string.Empty;
            }
        }

        public virtual string CallbackUrl
        {
            get { return _callbackUrl; }
            set
            {
                if (!string.IsNullOrEmpty(value) && value.StartsWith("http:"))
                    _callbackUrl = "https" + value.Substring(4);
                else
                    _callbackUrl = value;
            }
        }

        public string ClientType
        {
            get { return "strava"; }
        }

        public string Name
        {
            get { return "4 Roads - Strava OAuth Client"; }
        }

        public string Description
        {
            get { return "Provides user authentication through Strava"; }
        }

        public string ThemeColor
        {
            get { return "FC4C02"; }
        }

        public string ClientName
        {
            get { return _translatablePluginController.GetLanguageResourceValue("OAuth_Strava_Name"); }
        }

        public string Privacy
        {
            get { return _translatablePluginController.GetLanguageResourceValue("OAuth_Strava_Privacy"); }
        }

        public string ClientLogoutScript
        {
            get
            {
                return string.Empty;
            }
        }

        public string IconUrl
        {
            get
            {
                try
                {
                    ICentralizedFile file = CentralizedFileStorage.GetFileStore("oauthimages").GetFile(string.Empty, "strava.png");
                    if (file != null)
                        return file.GetDownloadUrl();
                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool Enabled
        {
            get
            {
                if (PluginManager.IsEnabled(this))
                    return IsConfigured;
                return false;
            }
        }

        public void Initialize()
        {

        }

        public string GetAuthorizationLink()
        {
            return string.Format("{0}?client_id={1}&response_type=code&scope=view_private&redirect_uri={2}", AuthorizeBaseUrl, ConsumerKey, Globals.UrlEncode(CallbackUrl));
        }

        public OAuthData ProcessLogin(HttpContextBase context)
        {
            if (!Enabled || context.Request.QueryString["error"] != null || _oauth.GetVerificationParameters(context) == null)
                AuthenticationFailed();

            CallbackUrl = _oauth.RemoveVerificationCodeFromUri(context);

            dynamic parameters = GetAccessTokenResponse(_oauth.GetVerificationParameters(context));

            if (string.IsNullOrEmpty((string)parameters.access_token))
                AuthenticationFailed();

            return ParseUserProfileInformation(parameters);
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var propertyGroupArray = new PropertyGroup[1]
                {
                    new PropertyGroup("options", "Options", 0)
                };
                var property1 = new Property("ConsumerKey", "Consumer Key", PropertyType.String, 0, "");
                property1.Rules.Add(new PropertyRule(typeof (TrimStringRule), false));
                propertyGroupArray[0].Properties.Add(property1);
                var property2 = new Property("ConsumerSecret", "Consumer Secret", PropertyType.String, 0, "");
                property2.Rules.Add(new PropertyRule(typeof (TrimStringRule), false));
                propertyGroupArray[0].Properties.Add(property2);
                propertyGroupArray[0].Properties.Add(new Property("AuthorizeBaseUrl", "Authorize Base URL", PropertyType.String, 0, "https://www.strava.com/oauth/authorize"));
                propertyGroupArray[0].Properties.Add(new Property("AccessTokenUrl", "Access Token Base URL", PropertyType.String, 0, "https://www.strava.com/oauth/token"));
            
                return propertyGroupArray;
            }
        }

        public bool IsConfigured
        {
            get
            {
                if (!string.IsNullOrEmpty(ConsumerKey))
                    return !string.IsNullOrEmpty(ConsumerSecret);
                return false;
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected virtual void LinkAccount(string clientUserId, int localUserId)
        {
            _oauthDataProvider.AddOAuthLink(new OAuthLink(ClientType, clientUserId, localUserId));
        }

        protected virtual void LogUserIn(User u, HttpContext context)
        {
            if (u == null || context == null)
                return;

            HttpCookie authCookie = FormsAuthentication.GetAuthCookie(u.Username, true);

            context.Response.Cookies.WriteCookie(authCookie, FormsAuthentication.Timeout, true);
        }

        public JObject GetAccessTokenResponse(NameValueCollection securityParams)
        {
            string query = _oauth.WebRequest(Method.POST, AccessTokenUrl, string.Format("client_id={1}&client_secret={3}&code={4}&redirect_uri={2}", "", ConsumerKey, Globals.UrlEncode(CallbackUrl), ConsumerSecret, Globals.UrlEncode(securityParams["verificationCode"])), null);

            if (query.Length > 0)
            {
                return JObject.Parse(query);
            }

            return null;
        }

        private void AuthenticationFailed()
        {
            throw new CSException(CSExceptionType.OAuthLoginFailed);
        }

        private OAuthData ParseUserProfileInformation(dynamic repsonseJObject)
        {
            dynamic athlete = repsonseJObject.athlete;

            string clientUserId = athlete.id;
            OAuthLink oauthLink = _oauthDataProvider.GetOAuthLink(ClientType, clientUserId);

            var authData = new OAuthData();
            authData.ClientId = clientUserId;
            authData.ClientType = ClientType;
           
            var user = default(Telligent.Evolution.Extensibility.Api.Entities.Version1.User);

            if (oauthLink != null)
                user = PublicApi.Users.Get(new UsersGetOptions() { Id = oauthLink.UserId });

            if (oauthLink != null && user != null)
            {
                authData.UserName = user.Username;
                authData.Email = user.PublicEmail;
                authData.CommonName = user.DisplayName;
                authData.AvatarUrl = user.AvatarUrl;
            }
            else
            {
                authData.Email = athlete.email;
                authData.UserName = GetUniquueUserName(athlete);
                authData.AvatarUrl = athlete.profile;
                authData.CommonName = athlete.firstname + athlete.lastname;
            }
            return authData;
        }

        private bool UserNameExists(string userName)
        {
            var user = PublicApi.Users.Get(new UsersGetOptions() { Username = userName });

            if (user != null && !user.HasWarningsOrErrors())
                return true;

            return false;
        }

        private string GetUniquueUserName(dynamic athlete)
        {
            string firstname = Convert.ToString(athlete.firstname) ?? string.Empty;
            string lastname = Convert.ToString(athlete.lastname);

            if (!string.IsNullOrEmpty(firstname))
            {
                if (lastname.Length > 0)
                    lastname = lastname.Substring(0, 2);

                string test = firstname + lastname;

                if (!UserNameExists(test))
                    return test;

                test = string.Format("{0}-{1}", firstname, lastname);

                if (!UserNameExists(test))
                    return test;

                test = string.Format("{0}.{1}", firstname, lastname);

                if (!UserNameExists(test))
                    return test;

                string format = "{0}{1}{2}";

                int counter = 1;

                test = string.Format(format, firstname, lastname, counter);

                while (UserNameExists(test))
                    test = string.Format("{0}{1}{2}", firstname, lastname, counter++);

                return test;
            }

            int num = new Random().Next(9999999);
        
            while (UserNameExists(num.ToString()) != null)
                ++num;

            return num.ToString();
        }

        public IEnumerable<Type> Plugins {
            get { return new[] {typeof (FileInstaller)}; }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translatablePluginController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation en = new Translation("en-us");

                en.Set("OAuth_Strava_Name", "Strava");
                en.Set("OAuth_Strava_Privacy", "By signing in with Strava, data from your profile, such as your name, userID, and email address, will be collected so that an account can be created for you.  Your Facebook password will not be collected.  Please click on the link at the bottom of the page to be directed to our privacy policy for information on how the collected data will be protected.");

                return new[] { en }; 
            }
        }
    }
}