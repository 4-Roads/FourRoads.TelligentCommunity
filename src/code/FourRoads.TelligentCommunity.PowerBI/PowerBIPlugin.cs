using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.PowerBI.Analytics.Language;
using FourRoads.TelligentCommunity.PowerBI.Analytics.Language.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;

namespace FourRoads.TelligentCommunity.PowerBI
{
    public class PowerBIPlugin : IConfigurablePlugin, ISingletonPlugin, IPluginGroup, IHttpCallback
    {
        private IPluginConfiguration _configuration;
        private Client _client;
        private static object _lockObj = new object();
        private List<string> _fields = new List<string>();
        private static int _maxdocs = 10000;
        private IHttpCallbackController _callbackController;

        public string Name
        {
            get
            {
                return "4 Roads - Power BI Connector";
            }
        }


        public string Description
        {
            get { return "Enables community data to be used in Microsoft PowerBI"; }
        }

        public void Initialize()
        {
            Apis.Get<ISearchIndexing>().Events.BeforeBulkIndex += Events_BeforeBulkIndex;

            lock (_lockObj)
            {
                _fields.Clear();

                string fieldList = _configuration.GetCustom("fields") ?? string.Empty;

                //Convert the querystring to a list from format Name=STRING&Name=STRING
                string[] fieldFilter = fieldList.Split('&').Select(c => Uri.UnescapeDataString(c.Split('=')[1])).ToArray();

                _fields.AddRange(Helpers.UserProfile.GetUserProfileFields().Where(k => fieldFilter.Contains(k.Name) || fieldFilter.Length == 0).Select(f => f.Name));

                _client = new Client(_configuration);
                if (_client.Init())
                {
                    _client.UpdateUserProfileSchema(_fields);
                }
            }
        }
        public IPluginConfiguration GetConfiguration()
        {
            return _configuration;
        }

        public void UpdateUserProfiles()
        {
            Client client = new Client(_configuration);

            if (client.Init())
            {
                if (client.DeleteTableRows())
                {
                    bool moreRecords = true;

                    UsersListOptions list = new UsersListOptions()
                    {
                        PageIndex = 0,
                        PageSize = _maxdocs,
                        IncludeHidden = true,
                        AccountStatus = "All"
                    };

                    while (moreRecords)
                    {
                        var results = Apis.Get<IUsers>().List(list);
                        moreRecords = results.TotalCount > (++list.PageIndex * list.PageSize);

                        if (results.Count > 0)
                        {
                            client.UploadUserProfiles(results, _fields);
                        }
                    }
                }
            }
        }

        private void Events_BeforeBulkIndex(BeforeBulkIndexingEventArgs e)
        {

            if (_client.GetToken())
            {
                // Get the services for use later
                var threadService = Apis.Get<IForumThreads>();
                var threadReplyService = Apis.Get<IForumReplies>();
                var blogPostService = Apis.Get<IBlogPosts>();
                var wikiPageService = Apis.Get<IWikiPages>();
                var commentsService = Apis.Get<IComments>();

                List<SearchIndexDocument> docs = new List<SearchIndexDocument>();

                foreach (var doc in e.Documents)
                {
                    // Select documents that are forum threads
                    if (doc.ContentTypeId == threadService.ContentTypeId ||
                        doc.ContentTypeId == threadReplyService.ContentTypeId ||
                        doc.ContentTypeId == blogPostService.ContentTypeId ||
                        doc.ContentTypeId == wikiPageService.ContentTypeId ||
                        doc.ContentTypeId == commentsService.ContentTypeId)
                    {
                        docs.Add(doc);
                    }

                    if (docs.Count >= _maxdocs)
                    {
                        _client.Upload(docs);
                        docs.Clear();
                    }
                }

                if (docs.Any())
                {
                    _client.Upload(docs);
                }
            }
        }

        public IEnumerable<Type> Plugins => new Type[] 
        { 
            typeof(PowerBIUserJob),
            typeof(AzureTestPropertyTemplate),
            typeof(WatsonTestPropertyTemplate)
        };

        public void Update(IPluginConfiguration configuration)
        {
            lock (_lockObj)
            {
                _configuration = configuration;
            }
        }

        public void ProcessRequest(HttpContextBase httpContext)
        {
            if (httpContext.Request.QueryString["azuretest"] != null && httpContext.Request.QueryString["azuretest"].ToString(CultureInfo.InvariantCulture) == "true")
            {
                DoAzureTest(httpContext);
            }
            else if (httpContext.Request.QueryString["watsontest"] != null && httpContext.Request.QueryString["watsontest"].ToString(CultureInfo.InvariantCulture) == "true")
            {
                DoWatsonTest(httpContext);
            }
            else
            {
                ReturnMessage(httpContext, "Unsupported action.", false);
                httpContext.Response.StatusCode = 404;
                return;
            }
        }

        public void SetController(IHttpCallbackController controller)
        {
            this._callbackController = controller;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup optionsGroup = new PropertyGroup() { Id = "PowerBI", LabelText = "PowerBI" };

                optionsGroup.Properties.Add(new Property
                {
                    Id = "userName",
                    LabelText = "Power BI User Name",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = ""
                });

                optionsGroup.Properties.Add(new Property
                {
                    Id = "password",
                    LabelText = "Power BI User Password",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "",
                    Options = new NameValueCollection
                    {
                        { "obscure", "true" }
                    }
                });

                optionsGroup.Properties.Add(new Property
                {
                    Id = "clientId",
                    LabelText = "App Client Id",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = ""
                });

                optionsGroup.Properties.Add(new Property
                {
                    Id = "groupName",
                    LabelText = "Group Name (optional)",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = ""
                });

                optionsGroup.Properties.Add(new Property
                {
                    Id = "datasetName",
                    LabelText = "Dataset Name",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "Community"
                });

                optionsGroup.Properties.Add(new Property
                {
                    Id = "tableName",
                    LabelText = "Table Name",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "Posts"
                });

                PropertyGroup urlsGroup = new PropertyGroup() { Id = "URLS", LabelText = "Urls" };

                urlsGroup.Properties.Add(new Property
                {
                    Id = "authorityUrl",
                    LabelText = "Azure AD authority Url",
                    DataType = "url",
                    Template = "url",
                    OrderNumber = 0,
                    DefaultValue = "https://login.windows.net/common/oauth2/authorize/"
                });

                urlsGroup.Properties.Add(new Property
                {
                    Id = "resourceUrl",
                    LabelText = "Azure AD resource Url",
                    DataType = "url",
                    Template = "url",
                    OrderNumber = 0,
                    DefaultValue = "https://analysis.windows.net/powerbi/api"
                });

                urlsGroup.Properties.Add(new Property
                {
                    Id = "apiUrl",
                    LabelText = "API Url",
                    DataType = "url",
                    Template = "url",
                    OrderNumber = 0,
                    DefaultValue = "https://api.powerbi.com/"
                });

                PropertyGroup azureGroup = new PropertyGroup() { Id = "AzureAnalytics", LabelText = "Azure Analytics" };

                azureGroup.Properties.Add(new Property
                {
                    Id = "azureRegion",
                    LabelText = "Azure Analytics Region",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "Westeurope"
                });

                azureGroup.Properties.Add(new Property
                {
                    Id = "azureTextAnalyticsAPI",
                    LabelText = "Text Analytics API Key",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "",
                    Options = new NameValueCollection
                    {
                        { "obscure", "true" }
                    }
                });

                azureGroup.Properties.Add(new Property
                {
                    Id = "azureTestContent",
                    LabelText = "Test content",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "I still have a dream. It is a dream deeply rooted in the American dream. I have a dream that one day this nation will rise up and live out the true meaning of its creed: \"We hold these truths to be self-evident, that all men are created equal.\""
                });

                var azureTestControl = new Property
                {
                    Id = "azureTestControl",
                    LabelText = "Test Integration",
                    DataType = "custom",
                    Template = "powerBI_azureTest",
                    OrderNumber = 0,
                    DefaultValue = ""
                };

                if (_callbackController != null)
                {
                    azureTestControl.Options.Add("callback", _callbackController.GetUrl());
                }

                azureTestControl.Options.Add("resturl", "");
                azureTestControl.Options.Add("data", "azuretest:true");
                azureTestControl.Options.Add("label", "Click to test Azure NLP API");

                azureGroup.Properties.Add(azureTestControl);

                PropertyGroup watsonGroup = new PropertyGroup() { Id = "WatsonAnalytics", LabelText = "Watson Analytics" };

                watsonGroup.Properties.Add(new Property
                {
                    Id = "watsonLanguageUrl",
                    LabelText = "NLP Url",
                    DataType = "url",
                    Template = "url",
                    OrderNumber = 0,
                    DefaultValue = "https://gateway-fra.watsonplatform.net/natural-language-understanding/api/v1/analyze?version=2018-03-16"
                });

                watsonGroup.Properties.Add(new Property
                {
                    Id = "watsonTextAnalyticsAPI",
                    LabelText = "NLP API Key",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "",
                    Options = new NameValueCollection
                    {
                        { "obscure", "true" }
                    }
                });

                watsonGroup.Properties.Add(new Property
                {
                    Id = "watsonTestContent",
                    LabelText = "Test content",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "I still have a dream. It is a dream deeply rooted in the American dream. I have a dream that one day this nation will rise up and live out the true meaning of its creed: \"We hold these truths to be self-evident, that all men are created equal.\""
                });

                var watsonTestControl = new Property
                {
                    Id = "watsonTestControl",
                    LabelText = "Test Integration",
                    DataType = "custom",
                    Template = "powerBI_watsonTest",
                    OrderNumber = 0,
                    DefaultValue = ""
                };

                if (_callbackController != null)
                {
                    watsonTestControl.Options.Add("callback", _callbackController.GetUrl());
                }

                watsonTestControl.Options.Add("resturl", "");
                watsonTestControl.Options.Add("data", "watsontest:true");
                watsonTestControl.Options.Add("label", "Click to test Watson NLP API");

                watsonGroup.Properties.Add(watsonTestControl);

                PropertyGroup userprofileGroup = new PropertyGroup() { Id = "UserProfileFields", LabelText = "User Profile Fields" };

                //Stored as a querystring in the format Name=STRING&Name=STRING
                userprofileGroup.Properties.Add(new Property
                {
                    Id = "fields",
                    LabelText = "Fields",
                    DataType = "custom",
                    Template = "core_v2_userProfileFields",
                    Options = new NameValueCollection
                    {
                        { "singleSelect", "false" }
                    }
                });

                return new PropertyGroup[] { optionsGroup, urlsGroup, azureGroup, watsonGroup, userprofileGroup };
            }
        }

        private void DoAzureTest(HttpContextBase httpContext)
        {
            try
            {
                var azureRegion = _configuration.GetString("azureRegion");
                var azuretextAnalyticsAPI = _configuration.GetString("azureTextAnalyticsAPI");

                if (!string.IsNullOrWhiteSpace(azureRegion) && !string.IsNullOrWhiteSpace(azuretextAnalyticsAPI))
                {
                    var azureLanguage = new AzureLanguage(azureRegion, azuretextAnalyticsAPI);
                    var testContent = _configuration.GetString("azureTestContent");
                    var keywords = azureLanguage.KeyPhrases(testContent);
                    if (keywords != null && keywords.Count > 0)
                    {
                        ReturnMessage(httpContext, $"Found {keywords.Count} keywords - [{String.Join(",", keywords)}].", true);
                    }
                    else
                    {
                        ReturnMessage(httpContext, "Failed to locate any keywords.", false);
                    }
                }
                else
                {
                    ReturnMessage(httpContext, "Please check that the interface is correctly configured.", false);
                }
            }
            catch (Exception ex)
            {
                ReturnMessage(httpContext, $"Error testing azure nlp - {ex.Message}.", false);
                new TCException("Error testing azure nlp", ex).Log();
            }
        }

        private void DoWatsonTest(HttpContextBase httpContext)
        {
            try
            {
                var watsonLanguageUrl = _configuration.GetString("watsonLanguageUrl");
                var watsontextAnalyticsAPI = _configuration.GetString("watsonTextAnalyticsAPI");

                if (!string.IsNullOrWhiteSpace(watsontextAnalyticsAPI) && !string.IsNullOrWhiteSpace(watsonLanguageUrl))
                {
                    var watsonLanguage = new WatsonLanguage(watsontextAnalyticsAPI, watsonLanguageUrl);
                    var testContent = _configuration.GetString("watsonTestContent");
                    var keywords = watsonLanguage.KeyPhrases(testContent);
                    if (keywords != null && keywords.Count > 0)
                    {
                        ReturnMessage(httpContext, $"Found {keywords.Count} keywords - [{String.Join(", ", keywords)}].", true);
                    }
                    else
                    {
                        ReturnMessage(httpContext, "Failed to locate any keywords.", false);
                    }
                }
                else
                {
                    ReturnMessage(httpContext, "Please check that the interface is correctly configured.", false);
                }
            }
            catch (Exception ex)
            {
                ReturnMessage(httpContext, $"Error testing watson nlp - {ex.Message}.", false);
                new TCException("Error testing watson nlp", ex).Log();
            }
        }

        private void ReturnMessage(HttpContextBase httpContext, string message, bool succesful)
        {
            if (httpContext != null && !string.IsNullOrWhiteSpace(message))
            {
                httpContext.Response.ContentType = "text/javascript";
                if (succesful)
                {
                    // return an ale
                    httpContext.Response.Write($"alert('{ Apis.Get<IHtml>().EnsureEncoded(message)}');");
                }
                else
                {
                    httpContext.Response.Write($"$.telligent.evolution.notifications.show('{ Apis.Get<IHtml>().EnsureEncoded(message)}', {{ type: 'error' }});");
                }                
            }
        }
    }
}
