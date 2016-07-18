using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Nexus2.Common;
using Newtonsoft.Json.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls.PropertyRules;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Authentication.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Nexus2.Strava
{
    public class StravaOAuth2Client : IOAuthClient, IRequiredConfigurationPlugin , IPluginGroup , ITranslatablePlugin
    {
        private string _callbackUrl;
        private ITranslatablePluginController _translatablePluginController;

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
            get { return FileInstaller.FILESTOREKEY; }
        }

        public string RefreshTokenUrl
        {
            get { return string.Empty; }
        }

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
                    ICentralizedFile file = CentralizedFileStorage.GetFileStore(FileInstaller.FILESTOREKEY).GetFile(string.Empty, "strava.png");
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
                //This property is not used
                return true;
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
            if (!Enabled || context.Request.QueryString["error"] != null || OAuthFunctions.GetVerificationParameters(context) == null)
                AuthenticationFailed();

            CallbackUrl = OAuthFunctions.RemoveVerificationCodeFromUri(context);

            dynamic parameters = GetAccessTokenResponse(OAuthFunctions.GetVerificationParameters(context));

            if (string.IsNullOrEmpty((string)parameters.access_token))
                AuthenticationFailed();

            return ParseUserProfileInformation(parameters);
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var propertyGroupArray = new PropertyGroup[]
                {
                    new PropertyGroup("options", "Options", 0)
                };

                var property1 = new Property("ConsumerKey", "Client ID", PropertyType.String, 0, "");
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

        public JObject GetAccessTokenResponse(NameValueCollection securityParams)
        {
            string query = OAuthFunctions.WebRequest("POST", AccessTokenUrl, string.Format("client_id={1}&client_secret={3}&code={4}&redirect_uri={2}", "", ConsumerKey, HttpUtility.UrlEncode(CallbackUrl), ConsumerSecret, HttpUtility.UrlEncode(securityParams["verificationCode"])), null);

            if (query.Length > 0)
            {
                return JObject.Parse(query);
            }

            return null;
        }

        private void AuthenticationFailed()
        {
            throw new TCException(CSExceptionType.OAuthLoginFailed, "OAuth login failed");
        }

        private OAuthData ParseUserProfileInformation(dynamic repsonseJObject)
        {
            dynamic athlete = repsonseJObject.athlete;

            string clientUserId = athlete.id;

            var authData = new OAuthData();
   
            authData.ClientId = clientUserId;
            authData.ClientType = ClientType;
            authData.Email = athlete.email;
            authData.UserName = GetUniquueUserName(athlete);  //Note although this is called every time the user authenticates it's only used when the account is first created
            authData.AvatarUrl = athlete.profile;
            authData.CommonName = athlete.firstname + athlete.lastname;
         
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
                if (lastname.Length > 1)
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
        
            while (UserNameExists(num.ToString()))
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