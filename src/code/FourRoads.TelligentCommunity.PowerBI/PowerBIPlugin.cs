using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using System.Collections.Specialized;

namespace FourRoads.TelligentCommunity.PowerBI
{
    public class PowerBIPlugin : IConfigurablePlugin, ISingletonPlugin, IPluginGroup
    {
        private IPluginConfiguration _configuration;
        private Client _client;
        private static object _lockObj = new object();
        private List<string> _fields = new List<string>();
        private static int _maxdocs = 10000;

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

        public IEnumerable<Type> Plugins => new Type[] { typeof(PowerBIUserJob) };

        public void Update(IPluginConfiguration configuration)
        {
            lock (_lockObj)
            {
                _configuration = configuration;
            }
        }


        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup optionsGroup = new PropertyGroup() {Id="PowerBI", LabelText = "PowerBI"};

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

                PropertyGroup urlsGroup = new PropertyGroup() {Id="URLS", LabelText="Urls"};

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

                PropertyGroup azureGroup = new PropertyGroup() {Id="AzureAnalytics", LabelText = "Azure Analytics"};

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

                //todo - rewrite as template
                //var azureTestControl = new TelligentProperty("Test", "Test Integration", PropertyType.Custom, 0, string.Empty);
                //azureTestControl.ControlType = typeof(AzureTestControl);
                //azureGroup.Properties.Add(azureTestControl);
                
                PropertyGroup watsonGroup = new PropertyGroup() {Id="WatsonAnalytics", LabelText = "Watson Analytics"};

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

                //todo - rewrite as template
                //var watsonTestControl = new TelligentProperty("Test", "Test Integration", PropertyType.Custom, 0, string.Empty);
                //watsonTestControl.ControlType = typeof(WatsonTestControl);
                //watsonGroup.Properties.Add(watsonTestControl);

                PropertyGroup userprofileGroup = new PropertyGroup() {Id="UserProfileFields", LabelText = "User Profile Fields"};

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

    }
}
