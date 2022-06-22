using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.HubSpot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Formatting = Newtonsoft.Json.Formatting;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using System.Web;
using FourRoads.TelligentCommunity.HubSpot.Controls;
using Telligent.Evolution.Extensibility.Urls.Version1;
using IPermissions = Telligent.Evolution.Extensibility.Api.Version2.IPermissions;
using SitePermission = Telligent.Evolution.Components.SitePermission;

/*
    Auth Process
    
    Requirements
    ============
    -Hubspot developers account:
        visit https://developers.hubspot.com/ and create an account
    -An application in hubspot with the "Read from and write to my Contacts" and "Basic OAuth functionality" scopes defined
    -The client id and secret id from the above application
    -Telligent application hosted with a https secured endpoint
    
    Setup 
    =====
    If in dev create an entry in your system32/drivers/etc/hosts file for your 'test' domain
        127.0.0.1 www.devurl.com
        127.0.0.1 devurl.com

    Configure iis to host your telligent application and add https bindings for your domain so for example www.devurl.com and devurl.com
    Edit your telligent application connectionStrings.config make sure SiteUrl is set to a https url (e.g. https://www.devurl.com)
    You may also need to configure your database access depending on the user you configure in iis

    Run the site and via the administration page enable the following: 
		"4 Roads - Hubspot Core" plugin
		"Hubspot - Authorize Property Template"
		"Hubspot - Trigger Action Property Template"
    In the plugin configuration, enter the hubspot client id and secret id and save the configuration
    
    oAuth Overview
    ==============
    
    Configure the Auth settings for the hubspot application
        -Make sure the "Read from and write to my Contacts" and "Basic OAuth functionality" scopes are defined
        -Enter the telligent site url as the redirect url: e.g. https://www.devurl.com/
        
    You can then visit the install url in a browser, this takes the format below:
        https://app.hubspot.com/oauth/authorize?client_id=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx&redirect_uri=https://www.devurl.com/&scope=contacts%20oauth
    This will prompt the user to allow access to your application and upon successful auth will call back to the redirect_uri passing a code.
    You must ensure this redirect_uri exactly matches your hosting and is secured using https.
  
    You will receive a call back to your url with a code as below (use this in the plugin config panel)
    https://www.devurl.com/?code=yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy

    In the configuration panel enter this code and click on the auth button.

    The stored values for AccessToken and RefreshToken are hidden in the plugin configuration.

    See TestControl for some sample usage.
*/


namespace FourRoads.TelligentCommunity.HubSpot
{
    public class HubspotCrm : IConfigurablePlugin, ISingletonPlugin, ICrmPlugin, IPluginGroup, INavigable, IHttpCallback
    {
        private string _accessToken;
        private string _refreshToken;
        private DateTime _expires = DateTime.UtcNow;
        private Dictionary<string, string> _mappings;
        private IHttpCallbackController _callbackController;
        private IPluginConfiguration _configuration;

        private const string HubSpotBaseUrl = "https://api.hubapi.com/";

        public string Description => "Hubspot plugin";

        public void RegisterUrls(IUrlController controller)
        {
            //success page
            controller.AddPage("fr-hubspot-authorize-done", "hubspot/authorized", null, null, "HubSpot Authorization",
                new PageDefinitionOptions
                {
                    Title = "hubspot-ok",
                    DefaultPageXml = _defaultPageXml,
                    Validate = (ctx, urlAccessController) => { },
                    ParseContext = _ => { }
                });

            // https://hostname.my/hubspot/authorize?code=35083c76-d7e3-4cfe-be6c-57f462773728
            controller.AddRaw("fr-hubspot-authorize-start", "hubspot/authorize", null, null,
                (httpContext, pageContext) =>
                {
                    if (httpContext.Request.Url == null)
                    {
                        httpContext.Response.Redirect(Apis.Get<ICoreUrls>().Message(126), true);
                        return;
                    }

                    var code = httpContext.Request.QueryString["code"];
                    if (code != null && Guid.TryParse(code, out var dummy))
                    {
                        var isAdmin = Apis.Get<IPermissions>()
                            .CheckPermission(SitePermission.ManageSettings, pageContext.UserId)
                            .IsAllowed;
                        var url = httpContext.Request.Url.GetLeftPart(UriPartial.Path);
                        if (isAdmin)
                        {
                            httpContext.Response.Redirect(InitialLinkoAuth(code, url)
                                    ? "/hubspot/authorized" //success
                                    : Apis.Get<ICoreUrls>().Message(126), //Application Authorization Not Allowed
                                endResponse: true);
                        }

                        // non admin user might have already been redirected to login page in Validate() below
                        if (httpContext.Response.HeadersWritten) return;
                    }

                    // any invalid or nor admin requests see fake "success" page
                    httpContext.Response.Redirect("/hubspot/authorized", true);
                },
                new RawDefinitionOptions
                {
                    Title = "HubSpot Authorization",
                    Validate = (pageContext, urlAccessController) =>
                    {
                        var isAdmin = Apis.Get<IPermissions>()
                            .CheckPermission(SitePermission.ManageSettings, pageContext.UserId)
                            .IsAllowed;
                        if (!isAdmin)
                        {
                            var login = Apis.Get<ICoreUrls>().Message(6); //Message: "Not Found
                            urlAccessController.Redirect(login);
                        }
                    },
                    ParseContext = _ => { }
                });
        }

        public string Name => "4 Roads - Hubspot Core";

        public void Initialize()
        {
        }

        private static void WriteJsonProp(JsonWriter jsonObject, string prop, string value)
        {
            jsonObject.WriteStartObject();
            jsonObject.WritePropertyName("property");
            jsonObject.WriteValue(prop);
            jsonObject.WritePropertyName("value");
            jsonObject.WriteValue(value);
            jsonObject.WriteEndObject();
        }

        private Dictionary<string, string> LoadMappings()
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>();

            string config = _configuration.GetString("ProfileConfig");

            if (!string.IsNullOrWhiteSpace(config))
            {
                using (XmlTextReader rd = new XmlTextReader(config, XmlNodeType.Document,
                           new XmlParserContext(null, null, string.Empty, XmlSpace.Default)))
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
            get => _mappings ?? (_mappings = LoadMappings());
            set => _mappings = value;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                
                var pg = new PropertyGroup() { Id = "Options", LabelText = "Options" };
                var callbackUrl = Apis.Get<IUrl>()?.Absolute("/hubspot/authorize") ?? string.Empty;
                pg.Properties.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.Property
                {
                    Id = "infoLabel",
                    LabelText = string.Empty,
                    DefaultValue =
                        $"Navigate to HubSpot and use <span style=\"font-weight:800\">{callbackUrl}</span> "+
                        "as a Redirect Url in the HubSpot App configuration.</br>"+
                        "After saving your HubSpot App configuration, use Install URL to obtain access token.",
                    DataType = "custom",
                    Template = "message_label",
                    OrderNumber = 0
                });
#if DEBUG
                var authBtn = new Telligent.Evolution.Extensibility.Configuration.Version1.Property()
                {
                    Id = "AuthBtn",
                    LabelText = "Auth Code",
                    DataType = "custom",
                    Template = "hubspot_authorize",
                    DefaultValue = "",
                };
                authBtn.Options.Add("outerButtonLabel",
                    "Press this button once you have obtained your authorization code");
                authBtn.Options.Add("innerButtonLabel", "Authorize oAuth Code");
                pg.Properties.Add(authBtn);
#endif
                pg.Properties.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.Property
                {
                    Id = "ClientId",
                    LabelText = "Client Id",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = ""
                });

                pg.Properties.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.Property
                {
                    Id = "ClientSecret",
                    LabelText = "Client Secret",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = ""
                });

                var testControl = new Telligent.Evolution.Extensibility.Configuration.Version1.Property()
                {
                    Id = "widgetRefresh",
                    LabelText = "Click to test hubspot API integration",
                    DescriptionResourceName = "Request a background job to refresh the custom widgets",
                    DataType = "custom",
                    Template = "hubspot_triggerAction",
                    DefaultValue = ""
                };
                if (_callbackController != null)
                {
                    testControl.Options.Add("callback", _callbackController.GetUrl());
                }

                testControl.Options.Add("resturl", "");
                testControl.Options.Add("data", "refresh:true");
                testControl.Options.Add("buttonLabel", "Test Integration");
                testControl.Options.Add("actionSuccessMessage", "Successfully read contact property groups");
                testControl.Options.Add("actionFailureMessage", "Failed to read contact property groups");
                pg.Properties.Add(testControl);

                var pg3 = new PropertyGroup() { Id = "ProfileConfig", LabelText = "Profile Configuration" };

                pg3.Properties.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.Property
                {
                    Id = "ProfileConfig",
                    LabelText = "Profile Config",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "",
                    Options = new NameValueCollection
                    {
                        { "rows", "10" },
                        { "columns", "80" },
                        { "syntax", "xml" }
                    }
                });

                var pg2 = new PropertyGroup() { Id = "Running", LabelText = "Running Values" };

                pg2.Properties.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.Property
                {
                    Id = "AccessToken",
                    LabelText = "Access Token",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "",
                    Options = new NameValueCollection
                    {
                        { "obscure", "true" },
                    }
                });

                pg2.Properties.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.Property
                {
                    Id = "RefreshToken",
                    LabelText = "Refresh Token",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "",
                    Options = new NameValueCollection
                    {
                        { "obscure", "true" },
                    }
                });

                pg2.Properties.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.Property
                {
                    Id = "Expires",
                    LabelText = "Expires",
                    DataType = "datetime",
                    Template = "datetime",
                    OrderNumber = 0,
                    DefaultValue = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                });

                return new[] { pg, pg3, pg2 };
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;

            _accessToken = _configuration.GetString("AccessToken");
            _refreshToken = _configuration.GetString("RefreshToken");
            Debug.Assert(_configuration != null, nameof(_configuration) + " != null");
            _expires = _configuration.GetDateTime("Expires").HasValue
                ? _configuration.GetDateTime("Expires").Value
                : DateTime.UtcNow;

            Mappings = null;
        }

        private dynamic CreateApiRequest(string requestType, string endPoint, string data)
        {
            using (var content = new StringContent(data, Encoding.UTF8, "application/json"))
            {
                var token = GetAccessToken();
                return CreateRequest(requestType, endPoint, 
                    // ReSharper disable once AccessToDisposedClosure
                    () => content,
                    cli => cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token));
            }
        }

        private dynamic CreateOauthRequest(HttpContent parameters)
        {
            const string endPoint = "oauth/v1/token";

            return CreateRequest("POST", endPoint, () => parameters);
        }

        private dynamic CreateRequest(string requestType, string endPoint, Func<HttpContent> preContent,
            Action<HttpClient> action = null)
        {
            var req = new HttpClient();

            action?.Invoke(req);

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

                var contentObj = JsonConvert.DeserializeObject(content);

                if (!task.Result.IsSuccessStatusCode)
                {
                    Apis.Get<IEventLog>()
                        .Write($"Hubspot API Issue: http status code {task.Result.StatusCode} - {content}",
                            new EventLogEntryWriteOptions());

                    if (task.Result.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }

                    if (task.Result.StatusCode == HttpStatusCode.Unauthorized
                        && content.Contains("EXPIRED_AUTHENTICATION"))
                    {
                        //[{"status":"error","message":"The OAuth token used to make this call expired 75 second(s) ago.",
                        //"correlationId":"xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx","category":"EXPIRED_AUTHENTICATION",
                        //"context":{"expire time":["2022-06-21T10:37:52.040Z"]}}]
                        throw new Exception($"HttpClient Unauthorized. [${content}]");
                    }

                    throw new Exception($"HttpClient Failed : {task.Result.StatusCode}", new Exception(content));
                }

                return contentObj;
            }

            throw new Exception("HttpClient error");
        }

        private void ProcessHubspotRequestResponse(dynamic jsonResponse)
        {
            if (jsonResponse.error != null)
            {
                Apis.Get<IEventLog>().Write("Hubspot API Issue:" + jsonResponse.error_description,
                    new EventLogEntryWriteOptions
                    {
                        EventType = nameof(EventLogEntryType.Error)
                    });

                _expires = DateTime.UtcNow.AddSeconds(-60);
                _configuration.SetString("AccessToken", string.Empty);
                _configuration.SetString("RefreshToken", string.Empty);
                _configuration.SetString("Expires", string.Empty);
            }

            if (jsonResponse.access_token != null)
            {
                Apis.Get<IEventLog>().Write("Obtained access token for Hubspot",
                    new EventLogEntryWriteOptions() { EventType = nameof(EventLogEntryType.Information) });

                _configuration.SetString("AccessToken", jsonResponse.access_token.ToString());
                _configuration.SetString("RefreshToken", jsonResponse.refresh_token.ToString());
                var expiresIn = jsonResponse.expires_in.ToString();
                _configuration.SetString("Expires", DateTime.UtcNow
                    .AddSeconds(Convert.ToInt32(expiresIn)) // use expiration sent with response
                    .AddSeconds(-60) // and decrease it by a minute
                    .ToString(CultureInfo.InvariantCulture));
            }

            _configuration.Commit();
        }

        public string GetAccessToken()
        {
            if (!PluginManager.IsEnabled(this)) return _accessToken;

            if (DateTime.UtcNow < _expires) return _accessToken;

            if (!string.IsNullOrWhiteSpace(_refreshToken))
            {
                var url = Apis.Get<IUrl>().Absolute(Apis.Get<IUrl>().ApplicationEscape("~"));

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", _configuration.GetString("ClientId") },
                    { "client_secret", _configuration.GetString("ClientSecret") },
                    { "redirect_uri", url },
                    { "refresh_token", _refreshToken }
                });

                var response = CreateOauthRequest(content);

                ProcessHubspotRequestResponse(response);
            }
            else
            {
                Apis.Get<IEventLog>()
                    .Write("Hubspot API Issue: Refresh token blank, you need to re-link your account",
                        new EventLogEntryWriteOptions());
            }

            return _accessToken;
        }

        public bool InitialLinkoAuth(string authCode, string url)
        {
            if (!string.IsNullOrWhiteSpace(authCode))
            {
                FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", _configuration.GetString("ClientId") },
                    { "client_secret", _configuration.GetString("ClientSecret") },
                    { "redirect_uri", url },
                    { "code", authCode }
                });

                dynamic repsonse = CreateOauthRequest(content);

                ProcessHubspotRequestResponse(repsonse);

                return repsonse.access_token != null;
            }

            Apis.Get<IEventLog>().Write("Hubspot API Issue: auth code blank, you need to link your account",
                new EventLogEntryWriteOptions());
            throw new ArgumentException("Hubspot API Issue: auth code blank, you need to link your account");
        }

        public void SynchronizeUser(User u)
        {
            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName, () =>
            {
                Apis.Get<IEventLog>().Write($"Synchronizing {u.Username} to Hubspot",
                    new EventLogEntryWriteOptions() { EventType = "Information" });

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

                                if (Apis.Get<IUserProfileFields>().Get(src).HasMultipleValues ?? false)
                                {
                                    data = data.Replace(",", ";");
                                }

                                WriteJsonProp(JsonObject, Mappings[src], data);
                            }
                        }

                        JsonObject.WriteEndArray();
                    }
                }

                dynamic response = CreateApiRequest("POST",
                    $"contacts/v1/contact/createOrUpdate/email/{Apis.Get<IUrl>().Encode(u.PrivateEmail)}/",
                    "{" + sb + "}");

                if (response.status != null && response.status == "error")
                {
                    Apis.Get<IEventLog>().Write("Hubspot API Issue:" + response, new EventLogEntryWriteOptions());
                }
            });
        }

        public dynamic GetUserProperties(string email)
        {
            dynamic response = null;
            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName,
                () =>
                {
                    response = CreateApiRequest("GET",
                        $"contacts/v1/contact/email/{Apis.Get<IUrl>().Encode(email)}/profile", string.Empty);
                });
            return response;
        }

        public List<ContactPropertyGroup> GetContactPropertyGroups()
        {
            List<ContactPropertyGroup> contactPropertyGroups = new List<ContactPropertyGroup>();
            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("GET", "properties/v1/contacts/groups", string.Empty);

                try
                {
                    contactPropertyGroups = response.ToObject<List<ContactPropertyGroup>>();

                    if (!contactPropertyGroups.Any())
                    {
                        Apis.Get<IEventLog>().Write("Hubspot API Issue: No contact property groups found ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    Apis.Get<IEventLog>().Write(
                        $"Hubspot API Issue: Failed to map contact property groups : {response} {e.Message}",
                        new EventLogEntryWriteOptions());
                }
            });

            return contactPropertyGroups;
        }

        public ContactPropertyGroup AddContactPropertyGroup(ContactPropertyGroup contactPropertyGroup)
        {
            ContactPropertyGroup newContactPropertyGroup = null;

            contactPropertyGroup.name = contactPropertyGroup.name.ToLower();

            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("POST", "properties/v1/contacts/groups",
                    JsonConvert.SerializeObject(contactPropertyGroup));

                try
                {
                    newContactPropertyGroup = response.ToObject<ContactPropertyGroup>();

                    if (newContactPropertyGroup == null ||
                        string.IsNullOrWhiteSpace(newContactPropertyGroup.displayName))
                    {
                        Apis.Get<IEventLog>().Write("Hubspot API Issue: Failed to create contact property group ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    Apis.Get<IEventLog>().Write(
                        $"Hubspot API Issue: Failed to create contact property group : {response} {e.Message}",
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

            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName, () =>
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
                        Apis.Get<IEventLog>().Write("Hubspot API Issue: Failed to create contact property ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    Apis.Get<IEventLog>().Write(
                        $"Hubspot API Issue: Failed to create contact property : {response} {e.Message}",
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

            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("GET", $"properties/v1/contacts/properties/named/{name.ToLower()}",
                    string.Empty);

                try
                {
                    contactProperty = response.ToObject<ContactProperty>();

                    if (contactProperty == null || string.IsNullOrWhiteSpace(contactProperty.name))
                    {
                        Apis.Get<IEventLog>().Write("Hubspot API Issue: Failed to locate contact property ",
                            new EventLogEntryWriteOptions());
                    }
                }
                catch (Exception e)
                {
                    Apis.Get<IEventLog>().Write(
                        $"Hubspot API Issue: Failed to locate contact property : {response} {e.Message}",
                        new EventLogEntryWriteOptions());
                }
            });

            return contactProperty;
        }

        public bool UpdateContactProperties(string email, Properties properties)
        {
            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName, () =>
            {
                dynamic response = CreateApiRequest("POST",
                    $"contacts/v1/contact/email/{Apis.Get<IUrl>().Encode(email)}/profile",
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

            Apis.Get<IUsers>().RunAsUser(Apis.Get<IUsers>().ServiceUserName, () =>
            {
                response = CreateApiRequest("POST",
                    $"contacts/v1/contact/createOrUpdate/email/{Apis.Get<IUrl>().Encode(email)}",
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

        public void ProcessRequest(HttpContextBase httpContext)
        {
            List<ContactPropertyGroup> listContacts = GetContactPropertyGroups();
            string message = string.Empty;

            httpContext.Response.ContentType = "text/javascript";

            if (listContacts != null && listContacts.Any())
            {
                message = $"Found {listContacts.Count} contact property groups";

                httpContext.Response.Write(
                    $"$.telligent.evolution.notifications.show('{Apis.Get<IHtml>().EnsureEncoded(message)}', {{ type: 'success' }});");
            }
            else
            {
                message = "Failed to read contact property groups";

                httpContext.Response.Write(
                    $"$.telligent.evolution.notifications.show('{Apis.Get<IHtml>().EnsureEncoded(message)}', {{ type: 'error' }});");
            }

            Apis.Get<IEventLog>().Write($"Hubspot API: {message}", new EventLogEntryWriteOptions());
        }

        public void SetController(IHttpCallbackController controller)
        {
            this._callbackController = controller;
        }

        public IEnumerable<Type> Plugins => new[]
        {
            typeof(AuthorizePropertyTemplate),
            typeof(TriggerActionPropertyTemplate),
            typeof(LabelPropertyTemplate),
            typeof(PingJob)
        };

        private string _defaultPageXml = @"
<contentFragmentPage pageName=""hubspot-ok"" isCustom=""true"" layout=""Content"" themeType=""0c647246-6735-42f9-875d-c8b991fe739b"" title=""HubSpot Authorization Success"" lastModified=""2022-06-21 11:30:04Z"">
  <regions>
    <region regionName=""Content"">
      <contentFragments>
        <contentFragment type=""Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::42eb3aec20af4b97bd8e5ab3f84a1feb"" showHeader=""False"" cssClassAddition=""no-wrapper with-spacing"" isLocked=""False"" configuration=""title=%24%7Bresource%3ACF_RawContent%7D&amp;html=%3Ch1%3ESuccess%21%3C%2Fh1%3E%0D%0A%3Cdiv%20class%3D%22message%22%3E%0D%0AHubSpot%20authorization%20code%20has%20been%20accepted%0D%0A%3C%2Fdiv%3E&amp;backgroundImage=&amp;width=page&amp;cssClass="" />
      </contentFragments>
    </region>
  </regions>
  <contentFragmentTabs />
</contentFragmentPage>";
    }
}