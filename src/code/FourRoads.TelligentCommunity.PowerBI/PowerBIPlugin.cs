using FourRoads.Common.TelligentCommunity.Controls;
using FourRoads.TelligentCommunity.PowerBI.Analytics.Language.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;
using TelligentProperty = Telligent.DynamicConfiguration.Components.Property;

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

                //Convert the string to  a list
                string[] fieldFilter = fieldList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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
                PropertyGroup optionsGroup = new PropertyGroup("PowerBI", "PowerBI", 0);

                optionsGroup.Properties.Add(new Property("userName", "Power BI User Name", PropertyType.String, 0, ""));
                AddPrivateProp("password", "Power BI User Password", optionsGroup);

                optionsGroup.Properties.Add(new Property("clientId", "App Client Id", PropertyType.String, 0, ""));
                optionsGroup.Properties.Add(new Property("groupName", "Group Name (optional)", PropertyType.String, 0, ""));
                optionsGroup.Properties.Add(new Property("datasetName", "Dataset Name", PropertyType.String, 0, "Community"));
                optionsGroup.Properties.Add(new Property("tableName", "Table Name", PropertyType.String, 0, "Posts"));

                PropertyGroup urlsGroup = new PropertyGroup("URLS", "Urls", 0);

                urlsGroup.Properties.Add(new Property("authorityUrl", "Azure AD authority Url", PropertyType.Url, 0, "https://login.windows.net/common/oauth2/authorize/"));
                urlsGroup.Properties.Add(new Property("resourceUrl", "Azure AD resource Url", PropertyType.Url, 0, "https://analysis.windows.net/powerbi/api"));
                urlsGroup.Properties.Add(new Property("apiUrl", "API Url", PropertyType.Url, 0, "https://api.powerbi.com/"));

                PropertyGroup azureGroup = new PropertyGroup("AzureAnalytics", "Azure Analytics", 0);

                azureGroup.Properties.Add(new Property("azureRegion", "Azure Analytics Region", PropertyType.String, 0, "Westeurope"));
                AddPrivateProp("azureTextAnalyticsAPI", "Text Analytics API Key", azureGroup);

                var azureTestControl = new TelligentProperty("Test", "Test Integration", PropertyType.Custom, 0, string.Empty);
                azureTestControl.ControlType = typeof(AzureTestControl);
                azureGroup.Properties.Add(azureTestControl);
                
                PropertyGroup watsonGroup = new PropertyGroup("WatsonAnalytics", "Watson Analytics", 0);

                watsonGroup.Properties.Add(new Property("watsonLanguageUrl", "NLP Url", PropertyType.Url, 0,
                    "https://gateway-fra.watsonplatform.net/natural-language-understanding/api/v1/analyze?version=2018-03-16"));
                AddPrivateProp("watsonTextAnalyticsAPI", "NLP API Key", watsonGroup);

                var watsonTestControl = new TelligentProperty("Test", "Test Integration", PropertyType.Custom, 0, string.Empty);
                watsonTestControl.ControlType = typeof(WatsonTestControl);
                watsonGroup.Properties.Add(watsonTestControl);

                PropertyGroup userprofileGroup = new PropertyGroup("UserProfileFields", "User Profile Fields", 0);

                Property availableFields = new Property("fields", "Fields", PropertyType.Custom, 0, "");

                availableFields.ControlType = typeof(CheckboxListControl);
                foreach (var field in Helpers.UserProfile.GetUserProfileFields())
                {
                    availableFields.SelectableValues.Add(new PropertyValue(field.Name, field.Title, 0) { });
                }

                userprofileGroup.Properties.Add(availableFields);

                return new PropertyGroup[] { optionsGroup, urlsGroup, azureGroup, watsonGroup, userprofileGroup };
            }
        }

        private void AddPrivateProp(string propName, string title, PropertyGroup pg)
        {
            var prop = new Telligent.DynamicConfiguration.Components.Property(propName, title, PropertyType.String, 0, string.Empty);
            prop.ControlType = typeof(PasswordPropertyControl);
            pg.Properties.Add(prop);
        }


    }
}
