using FourRoads.Common.TelligentCommunity.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Telligent.DynamicConfiguration.Components;
using Telligent.DynamicConfiguration.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class HubspotCrm : IConfigurablePlugin , ISingletonPlugin  , ICrmPlugin
    {
        IPluginConfiguration _configuration;
        string _accessToken;
        string _refreshToken;
        DateTime _expires = DateTime.Now;
        Dictionary<string, string> _mappings;

        public string Description
        {
            get
            {
                return "Hubspot plugin";
            }
        }

        public string Name
        {
            get
            {
                return "4 Roads - Hubspot Core";
            }
        }

        public void Initialize()
        {

        }

        private static void WriteJsonProp(JsonTextWriter JsonObject , string prop , string value)
        {
            JsonObject.WriteStartObject();
            JsonObject.WritePropertyName("property");
            JsonObject.WriteValue(prop);
            JsonObject.WritePropertyName("value");
            JsonObject.WriteValue(value);
            JsonObject.WriteEndObject();
        }

        private Dictionary<string, string> LoadMappings()
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>();

            string config = _configuration.GetString("ProfileConfig");

            if ( !string.IsNullOrWhiteSpace(config) )
            {
                using ( XmlTextReader rd = new XmlTextReader(config, XmlNodeType.Document, new XmlParserContext(null, null, string.Empty, XmlSpace.Default)) )
                {
                    if ( rd.ReadToNextSibling("Fields") )
                    {
                        rd.Read();

                        while ( rd.ReadToFollowing("Field") )
                        {
                            string srcField = rd.GetAttribute("src_field");
                            string destField = rd.GetAttribute("dest_field");

                            if ( !mappings.ContainsKey(srcField) )
                            {
                                mappings.Add(srcField, destField);
                            }
                        }
                    }
                }
            }

            return mappings;
        }

        private Dictionary<string,string> Mappings
        {
            get
            {
                if ( _mappings == null )
                {
                    _mappings = LoadMappings();
                }

                return _mappings;
            }
            set
            {
                _mappings = value;
            }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var pg = new PropertyGroup("Options", "Options", 0);

                var authbtn = new Property("oAuthCode", "Authorize oAuth Code", PropertyType.Custom, 0, string.Empty);
                authbtn.ControlType = typeof(AuthorizeButton);
                pg.Properties.Add(authbtn);

                pg.Properties.Add(new Property("ClientId", "Client Id", PropertyType.String, 0, string.Empty));
                pg.Properties.Add(new Property("ClientSecret", "Client Secret", PropertyType.String, 0, string.Empty));

                var pg3 = new PropertyGroup("ProfileConfig", "Profile Configuration", 0);
                var profile = new Property("ProfileConfig", "Profile Config", PropertyType.String, 0, string.Empty);
                profile.ControlType = typeof(MultilineStringControl);
                pg3.Properties.Add(profile);

                var pg2 = new PropertyGroup("Running", "Running Values", 0);

                AddPrivateProp("AccessToken", "Access Token" , pg2);
                AddPrivateProp("RefreshToken", "Access Token", pg2);
                pg2.Properties.Add(new Property("Expires", "Expires", PropertyType.DateTime, 0, DateTime.Now.ToString()));

                return new PropertyGroup[] { pg , pg3, pg2 };
            }
        }

        private void AddPrivateProp(string propName , string title , PropertyGroup pg)
        {
            var prop = new Property(propName, title, PropertyType.String, 0, string.Empty);
            prop.ControlType = typeof(PasswordPropertyControl);
            pg.Properties.Add(prop);
        }

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;

            _accessToken = _configuration.GetString("AccessToken");
            _refreshToken = _configuration.GetString("RefreshToken");
            _expires = _configuration.GetDateTime("Expires");

            Mappings = null;
        }

        private dynamic CreateApiRequest(string endPoint, string data)
        {
            using ( var content = new StringContent(data, Encoding.UTF8, "application/json") )
            {
                string token = GetAccessToken();

                return CreateRequest(endPoint, () => content, (cli) => cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token));
            }
        }

        private dynamic CreateOauthRequest(HttpContent parameters)
        {
            string endPoint = "oauth/v1/token";

            return CreateRequest(endPoint, () => parameters);
        }

        private dynamic CreateRequest(string endPoint , Func<HttpContent> preContent , Action<HttpClient> action = null)
        {
            HttpClient req = new HttpClient();

            if ( action != null )
                action(req);

            var task = req.PostAsync("https://api.hubapi.com/" + endPoint, preContent());

            task.Wait();

            if ( task.Result.Content != null )
            {
                var resultTask = task.Result.Content.ReadAsStringAsync();

                resultTask.Wait();

                string content = resultTask.Result;

                return JsonConvert.DeserializeObject(( content ));
            }

            throw new Exception("HttpClient error");
        }

        private void ProcessHubspotRequestRepsonse(dynamic jsonResponse)
        {
            if ( jsonResponse.error != null )
            {
                // throw new Exception("Hubspot API Issue:" + jsonResponse.error + jsonResponse.error_description);
                PublicApi.Eventlogs.Write("Hubspot API Issue:" + jsonResponse.error_description, new EventLogEntryWriteOptions());

                _expires = DateTime.Now;
                _configuration.SetString("AccessToken", string.Empty);
                _configuration.SetString("RefreshToken", string.Empty);
                _configuration.SetString("Expires", string.Empty);
            }

            if ( jsonResponse.access_token != null )
            {
                PublicApi.Eventlogs.Write("Obtained access token for Hubspot", new EventLogEntryWriteOptions() { EventType = "Debug" });

                _configuration.SetString("AccessToken", jsonResponse.access_token.ToString());
                _configuration.SetString("RefreshToken", jsonResponse.refresh_token.ToString());
                _configuration.SetString("Expires", DateTime.Now.AddHours(4).ToString());
                
            }
            _configuration.Commit();

        }

        public string GetAccessToken()
        {
            if ( PluginManager.IsEnabled(this) )
            {
                string url = PublicApi.Url.Absolute(PublicApi.Url.ApplicationEscape("~"));

                if ( !string.IsNullOrWhiteSpace(_refreshToken) )
                {
                    if ( DateTime.Now > _expires.AddHours(-1) )
                    {
                        FormUrlEncodedContent conent = new FormUrlEncodedContent(new Dictionary<string, string> {
                            {"grant_type","refresh_token"},
                            {"client_id",_configuration.GetString("ClientId")},
                            {"client_secret",_configuration.GetString("ClientSecret")},
                            {"redirect_uri",url},
                            {"refresh_token",_refreshToken}});

                        dynamic repsonse = CreateOauthRequest(conent);

                        ProcessHubspotRequestRepsonse(repsonse);
                    }
                }
                else
                {
                    PublicApi.Eventlogs.Write("Hubspot API Issue: Refresh token blank, youn need to re-link your account", new EventLogEntryWriteOptions());
                }
            }

            return _accessToken;
        }

        public bool InitialLinkoAuth(string authCOde)
        {
            if ( !string.IsNullOrWhiteSpace(authCOde))
            {
                string url = PublicApi.Url.Absolute(PublicApi.Url.ApplicationEscape("~"));

                FormUrlEncodedContent conent = new FormUrlEncodedContent(new Dictionary<string, string> {
                    {"grant_type","authorization_code"},
                    {"client_id",_configuration.GetString("ClientId")},
                    {"client_secret",_configuration.GetString("ClientSecret")},
                    {"redirect_uri",url},
                    {"code",authCOde}});

                dynamic repsonse = CreateOauthRequest(conent);

                ProcessHubspotRequestRepsonse(repsonse);

                return repsonse.error != null;
            }
            else
            {
                PublicApi.Eventlogs.Write("Hubspot API Issue: auth code blank, youn need to link your account", new EventLogEntryWriteOptions());
            }

            return false;
        }

        public void SynchronizeUser(User u)
        {
            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                PublicApi.Eventlogs.Write(string.Format("Syncronizing {0} to Hubspot", u.Username), new EventLogEntryWriteOptions() { EventType = "Information" });

                string parameters = string.Empty;
                StringBuilder sb = new StringBuilder();

                using ( var tw = new StringWriter(sb) )
                {
                    using ( var JsonObject = new Newtonsoft.Json.JsonTextWriter(tw) )
                    {
                        JsonObject.WritePropertyName("properties");
                        JsonObject.WriteStartArray();

                        WriteJsonProp(JsonObject, "email", u.PrivateEmail);

                        var fields = u.ProfileFields.ToLookup(k => k.Label, v => v.Value);

                        foreach ( string src in Mappings.Keys )
                        {
                            if ( fields.Contains(src) )
                            {
                                string data = fields[ src ].First();

                                if ( PublicApi.UserProfileFields.Get(src).HasMultipleValues??false )
                                {
                                    data = data.Replace(",", ";");
                                }

                                WriteJsonProp(JsonObject, Mappings[ src ], data);
                            }
                        }

                        JsonObject.WriteEndArray();
                    }
                }

                dynamic response = CreateApiRequest(string.Format("contacts/v1/contact/createOrUpdate/email/{0}/", PublicApi.Url.Encode(u.PrivateEmail)), "{" + sb.ToString() + "}");

                if ( response.status != null && response.status == "error")
                {
                    PublicApi.Eventlogs.Write("Hubspot API Issue:" + response, new EventLogEntryWriteOptions());
                }
            });
        }
    }

    public class AuthorizeButton : WebControl, IPropertyControl , INamingContainer
    {
        protected Button LinkButton;
        protected TextBox AuthCode;
        protected Literal Message; 

        protected override void EnsureChildControls()
        {
            base.EnsureChildControls();

            if ( AuthCode == null )
            {
                AuthCode = new TextBox();
                AuthCode.ID = "AuthCode";
                Controls.Add(AuthCode);
            }

            if ( Message == null )
            {
                Message = new Literal();
                Message.ID = "Message";
                Message.Text = "<label>Press this button once you have obtained your authorization code</label>";

                Controls.Add(Message);
            }

            if ( LinkButton == null )
            { 
                LinkButton = new Button();
                LinkButton.ID = "LinkBtn";
                LinkButton.Text = "Link oAuth";
                LinkButton.Click += LinkButton_Click;

                Controls.Add(LinkButton);
            }
        }

        private void LinkButton_Click(object sender, EventArgs e)
        {
            var plg = PluginManager.GetSingleton<HubspotCrm>();

            if ( plg != null )
            {
                if ( plg.InitialLinkoAuth(AuthCode.Text) )
                {
                    Message.Text = "<label style=\"color:red\">Failed to obtain oauth credentials</label>";
                }
                else
                {
                    Message.Text = "<label style=\"color:green\">oAuth Syncronized</label>";
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public Property ConfigurationProperty
        { get; set; }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public new Control Control
        {
            get { return this; }
        }

        public object GetConfigurationPropertyValue()
        {
            return null;
        }

        public void SetConfigurationPropertyValue(object value)
        {

        }
    }

}
