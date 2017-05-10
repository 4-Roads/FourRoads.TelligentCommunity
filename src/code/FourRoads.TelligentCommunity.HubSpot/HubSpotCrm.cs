using FourRoads.Common.TelligentCommunity.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Telligent.DynamicConfiguration.Components;
using Telligent.DynamicConfiguration.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.HubSpot.Models;
using System.Threading.Tasks;
using Telligent.Evolution.Extensibility;
using Formatting = Newtonsoft.Json.Formatting;
using TelligentProperty = Telligent.DynamicConfiguration.Components.Property;


/*

    Auth Process
    
    Requirements
    ============
    Hubspot developers account
    An application in hubspot with the "Read from and write to my Contacts" and "Basic OAuth functionality" scopes defined
    The client id and secret id from the above application
    Telligent application hosted with a https secured endpoint
    
    Setup 
    =====
    If in dev create an entry in your system32/drivers/etc/hosts file for your 'test' domain
    127.0.0.1 www.devurl.com
    127.0.0.1 devurl.com
    Configure iis to host your telligent application and add https bindings for your domain so for example www.devurl.com and devurl.com
    You may also need to configure your database access depending on the user you configure in iis

    Run the site and via the administration page enable the "4 Roads - Hubspot Core" plugin
    In the plugin configuration, enter the client id and secret id and save the configuration
    
    oAuth Overview
    ==============
    
    The first step is to call hubspot to allow the hubspot user account to auth access
    This takes the format below 
    https://app.hubspot.com/oauth/authorize?client_id=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx&scope=contacts%20automation&redirect_uri=https://www.devurl.com
    
    This will prompt the user to allow access to your application and upon successfull auth will call back to the redirect_uri passing a code.
    You must ensure this redirect_uri exactly matches your hosting and is secured using https 
  
    You will receive a call back to your url with a code as below (use this in the plugin config panel)
    https://www.devurl.com/?code=yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy

    In the configuration panel enter this code and click on the auth button.

    The stored values for AccessToken and RefreshToken are hidden in the plugin configuration.

    See TestControl for some sample usage.
*/


namespace FourRoads.TelligentCommunity.HubSpot
{
    public class HubspotCrm : IConfigurablePlugin, ISingletonPlugin, ICrmPlugin, IPluginGroup
    {
        IPluginConfiguration _configuration;
        string _accessToken;
        string _refreshToken;
        DateTime _expires = DateTime.Now;
        Dictionary<string, string> _mappings;
        
        private const string HubSpotBaseUrl = "https://api.hubapi.com/";

        public string Description => "Hubspot plugin";

        public string Name => "4 Roads - Hubspot Core";

        public void Initialize()
        {

        }

        private static void WriteJsonProp(JsonTextWriter JsonObject, string prop, string value)
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

            if (!string.IsNullOrWhiteSpace(config))
            {
                using (XmlTextReader rd = new XmlTextReader(config, XmlNodeType.Document, new XmlParserContext(null, null, string.Empty, XmlSpace.Default)))
                {
                    if (rd.ReadToNextSibling("Fields"))
                    {
                        rd.Read();

                        while (rd.ReadToFollowing("Field"))
                        {
                            string srcField = rd.GetAttribute("src_field");
                            string destField = rd.GetAttribute("dest_field");

                            if (!mappings.ContainsKey(srcField))
                            {
                                mappings.Add(srcField, destField);
                            }
                        }
                    }
                }
            }

            return mappings;
        }

        private Dictionary<string, string> Mappings
        {
            get
            {
                if (_mappings == null)
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

                var authbtn = new TelligentProperty("oAuthCode", "Authorize oAuth Code", PropertyType.Custom, 0, string.Empty);
                authbtn.ControlType = typeof(AuthorizeButton);
                pg.Properties.Add(authbtn);

                pg.Properties.Add(new TelligentProperty("ClientId", "Client Id", PropertyType.String, 0, string.Empty));
                pg.Properties.Add(new TelligentProperty("ClientSecret", "Client Secret", PropertyType.String, 0, string.Empty));

                var testControl = new TelligentProperty("Test", "Test Integration", PropertyType.Custom, 0, string.Empty);
                testControl.ControlType = typeof(TestControl);
                pg.Properties.Add(testControl);

                var pg3 = new PropertyGroup("ProfileConfig", "Profile Configuration", 0);
                var profile = new TelligentProperty("ProfileConfig", "Profile Config", PropertyType.String, 0, string.Empty);
                profile.ControlType = typeof(MultilineStringControl);
                pg3.Properties.Add(profile);

                var pg2 = new PropertyGroup("Running", "Running Values", 0);

                AddPrivateProp("AccessToken", "Access Token", pg2);
                AddPrivateProp("RefreshToken", "Refresh Token", pg2);
                pg2.Properties.Add(new TelligentProperty("Expires", "Expires", PropertyType.DateTime, 0, DateTime.Now.ToString()));

                return new PropertyGroup[] { pg, pg3, pg2 };
            }
        }

        private void AddPrivateProp(string propName, string title, PropertyGroup pg)
        {
            var prop = new Telligent.DynamicConfiguration.Components.Property(propName, title, PropertyType.String, 0, string.Empty);
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

        private dynamic CreateApiRequest(string requestType, string endPoint, string data)
        {
            using (var content = new StringContent(data, Encoding.UTF8, "application/json"))
            {
                string token = GetAccessToken();

                return CreateRequest(requestType, endPoint, () => content, (cli) => cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token));
            }
        }

        private dynamic CreateOauthRequest(HttpContent parameters)
        {
            string endPoint = "oauth/v1/token";

            return CreateRequest("POST", endPoint, () => parameters);
        }

        private dynamic CreateRequest(string requestType, string endPoint, Func<HttpContent> preContent, Action<HttpClient> action = null)
        {
            HttpClient req = new HttpClient();

            if (action != null)
                action(req);

            Task<HttpResponseMessage> task;

            switch (requestType)
            {
                default: // POST
                    task = req.PostAsync(HubSpotBaseUrl + endPoint, preContent());
                    break;
                case "DELETE":
                    task = req.DeleteAsync(HubSpotBaseUrl + endPoint);
                    break;
                case "PUT":
                    task = req.PutAsync(HubSpotBaseUrl + endPoint, preContent());
                    break;
                case "GET":
                    task = req.GetAsync(HubSpotBaseUrl + endPoint);
                    break;
            }

            task.Wait();

            if (task.Result.Content != null)
            {
                var resultTask = task.Result.Content.ReadAsStringAsync();

                resultTask.Wait();

                string content = resultTask.Result;

                if (!task.Result.IsSuccessStatusCode)
                {
                    PublicApi.Eventlogs.Write($"Hubspot API Issue: http status code {task.Result.StatusCode} - {content}", new EventLogEntryWriteOptions());

                    if (task.Result.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    throw new Exception($"HttpClient Failed : {task.Result.StatusCode}", new Exception(content));
                }

                return JsonConvert.DeserializeObject(content);
            }

            throw new Exception("HttpClient error");
        }

        private void ProcessHubspotRequestRepsonse(dynamic jsonResponse)
        {
            if (jsonResponse.error != null)
            {
                // throw new Exception("Hubspot API Issue:" + jsonResponse.error + jsonResponse.error_description);
                PublicApi.Eventlogs.Write("Hubspot API Issue:" + jsonResponse.error_description, new EventLogEntryWriteOptions());

                _expires = DateTime.Now;
                _configuration.SetString("AccessToken", string.Empty);
                _configuration.SetString("RefreshToken", string.Empty);
                _configuration.SetString("Expires", string.Empty);
            }

            if (jsonResponse.access_token != null)
            {
                PublicApi.Eventlogs.Write("Obtained access token for Hubspot", new EventLogEntryWriteOptions() {EventType = "Debug"});

                _configuration.SetString("AccessToken", jsonResponse.access_token.ToString());
                _configuration.SetString("RefreshToken", jsonResponse.refresh_token.ToString());
                _configuration.SetString("Expires", DateTime.Now.AddHours(4).ToString());
            }

            _configuration.Commit();

        }

        public string GetAccessToken()
        {
            if (PluginManager.IsEnabled(this))
            {
                string url = PublicApi.Url.Absolute(PublicApi.Url.ApplicationEscape("~"));

                if (!string.IsNullOrWhiteSpace(_refreshToken))
                {
                    if (DateTime.Now > _expires.AddHours(-1))
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
            if (!string.IsNullOrWhiteSpace(authCOde))
            {
                string url = PublicApi.Url.Absolute(PublicApi.Url.ApplicationEscape("~"));

                FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    {"grant_type","authorization_code"},
                    {"client_id",_configuration.GetString("ClientId")},
                    {"client_secret",_configuration.GetString("ClientSecret")},
                    {"redirect_uri",url},
                    {"code",authCOde}});

                dynamic repsonse = CreateOauthRequest(content);

                ProcessHubspotRequestRepsonse(repsonse);

                return repsonse.access_token != null;
            }
            else
            {
                PublicApi.Eventlogs.Write("Hubspot API Issue: auth code blank, you need to link your account", new EventLogEntryWriteOptions());
            }

            return false;
        }

        public void SynchronizeUser(User u)
        {
            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                PublicApi.Eventlogs.Write($"Syncronizing {u.Username} to Hubspot", new EventLogEntryWriteOptions() { EventType = "Information" });

                string parameters = string.Empty;
                StringBuilder sb = new StringBuilder();

                using (var tw = new StringWriter(sb))
                {
                    using (var JsonObject = new Newtonsoft.Json.JsonTextWriter(tw))
                    {
                        JsonObject.WritePropertyName("properties");
                        JsonObject.WriteStartArray();

                        WriteJsonProp(JsonObject, "email", u.PrivateEmail);

                        var fields = u.ProfileFields.ToLookup(k => k.Label, v => v.Value);

                        foreach (string src in Mappings.Keys)
                        {
                            if (fields.Contains(src))
                            {
                                string data = fields[src].First();

                                if (PublicApi.UserProfileFields.Get(src).HasMultipleValues ?? false)
                                {
                                    data = data.Replace(",", ";");
                                }

                                WriteJsonProp(JsonObject, Mappings[src], data);
                            }
                        }

                        JsonObject.WriteEndArray();
                    }
                }

                dynamic response = CreateApiRequest("POST", $"contacts/v1/contact/createOrUpdate/email/{PublicApi.Url.Encode(u.PrivateEmail)}/", "{" + sb + "}");

                if (response.status != null && response.status == "error")
                {
                    PublicApi.Eventlogs.Write("Hubspot API Issue:" + response, new EventLogEntryWriteOptions());
                }
            });
        }

        public dynamic GetUserProperties(string email)
        {
            dynamic response = null;
            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                response = CreateApiRequest("GET", $"contacts/v1/contact/email/{PublicApi.Url.Encode(email)}/profile", string.Empty);
            });
            return response;
        }

        public List<ContactPropertyGroup> GetContactPropertyGroups()
        {
            List<ContactPropertyGroup> contactPropertyGroups = new List<ContactPropertyGroup>();
            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("GET", "properties/v1/contacts/groups", string.Empty);

                try
                {
                    contactPropertyGroups = response.ToObject<List<ContactPropertyGroup>>();

                    if (!contactPropertyGroups.Any())
                    {
                        PublicApi.Eventlogs.Write("Hubspot API Issue: No contact property groups found ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    PublicApi.Eventlogs.Write($"Hubspot API Issue: Failed to map contact property groups : {response} {e.Message}",
                        new EventLogEntryWriteOptions());
                }
            });

            return contactPropertyGroups;
        }

        public ContactPropertyGroup AddContactPropertyGroup(ContactPropertyGroup contactPropertyGroup)
        {
            ContactPropertyGroup newContactPropertyGroup = null;

            contactPropertyGroup.name = contactPropertyGroup.name.ToLower();

            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("POST", "properties/v1/contacts/groups", JsonConvert.SerializeObject(contactPropertyGroup));

                try
                {
                    newContactPropertyGroup = response.ToObject<ContactPropertyGroup>();

                    if (newContactPropertyGroup == null || string.IsNullOrWhiteSpace(newContactPropertyGroup.displayName))
                    {
                        PublicApi.Eventlogs.Write("Hubspot API Issue: Failed to create contact property group ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    PublicApi.Eventlogs.Write($"Hubspot API Issue: Failed to create contact property group : {response} {e.Message}",
                        new EventLogEntryWriteOptions());
                }
            });

            return newContactPropertyGroup;
        }

        public ContactProperty AddContactProperty(ContactProperty contactProperty)
        {
            ContactProperty newContactProperty = null;
            contactProperty.name = contactProperty.name.ToLower();
            contactProperty.groupName = contactProperty.groupName.ToLower();

            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("POST", "properties/v1/contacts/properties",
                    JsonConvert.SerializeObject(contactProperty,
                    Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                    )
                );

                try
                {
                    newContactProperty = response.ToObject<ContactProperty>();

                    if (newContactProperty == null || string.IsNullOrWhiteSpace(newContactProperty.name))
                    {
                        PublicApi.Eventlogs.Write("Hubspot API Issue: Failed to create contact property ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    PublicApi.Eventlogs.Write($"Hubspot API Issue: Failed to create contact property : {response} {e.Message}",
                        new EventLogEntryWriteOptions());
                }
            });

            return newContactProperty;
        }

        public ContactProperty GetContactProperty(string name)
        {
            ContactProperty contactProperty = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("GET", $"properties/v1/contacts/properties/named/{name.ToLower()}", string.Empty);

                try
                {
                    contactProperty = response.ToObject<ContactProperty>();

                    if (contactProperty == null || string.IsNullOrWhiteSpace(contactProperty.name))
                    {
                        PublicApi.Eventlogs.Write("Hubspot API Issue: Failed to locate contact property ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    PublicApi.Eventlogs.Write($"Hubspot API Issue: Failed to locate contact property : {response} {e.Message}",
                        new EventLogEntryWriteOptions());
                }
            });

            return contactProperty;
        }

        public bool UpdateContactProperties(string email, Properties properties)
        {
            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("POST", $"contacts/v1/contact/email/{Apis.Get<IUrl>().Encode(email)}/profile",
                    JsonConvert.SerializeObject(properties,
                    Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                    )
                );
            });

            return true;
        }

        public dynamic UpdateorCreateContact(string email, Properties properties)
        {
            dynamic response = null;
            if (!properties.properties.Exists(p => p.value == email))
            {
                properties.properties.Add(new Models.Property() { property = "email", value = email });
            }

            PublicApi.Users.RunAsUser(PublicApi.Users.ServiceUserName, () =>
            {
                response = CreateApiRequest("POST", $"contacts/v1/contact/createOrUpdate/email/{Apis.Get<IUrl>().Encode(email)}",
                    JsonConvert.SerializeObject(properties,
                    Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                    )
                );
            });

            return response;
        }

        public IEnumerable<Type> Plugins => new Type[] { typeof(PingJob) };
    }
}
