using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.PowerBI.Analytics.Language;
using FourRoads.TelligentCommunity.PowerBI.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Version2;

namespace FourRoads.TelligentCommunity.PowerBI
{
    public class Client
    {
        private static string token = string.Empty;
        private static string groupId = string.Empty;
        private static string datasetId = string.Empty;

        private static string Username;
        private static string Password;
        private static string AuthorityUrl;
        private static string ResourceUrl;
        private static string ClientId;
        private static string ApiUrl;

        private static string GroupName;
        private static string DataSetName;
        private static string TableName;

        private AzureLanguage azureLanguage = null;
        private WatsonLanguage watsonLanguage = null;

        public Client(IPluginConfiguration configuration)
        {
            Username = configuration.GetString("userName");
            Password = configuration.GetString("password");
            AuthorityUrl = configuration.GetString("authorityUrl");
            ResourceUrl = configuration.GetString("resourceUrl");
            ClientId = configuration.GetString("clientId");
            ApiUrl = configuration.GetString("apiUrl");
            GroupName = configuration.GetString("groupName");
            DataSetName = configuration.GetString("datasetName");
            TableName = configuration.GetString("tableName");

            string azureRegion = configuration.GetString("azureRegion");
            string azuretextAnalyticsAPI = configuration.GetString("azureTextAnalyticsAPI");

            if (!string.IsNullOrWhiteSpace(azureRegion) && !string.IsNullOrWhiteSpace(azuretextAnalyticsAPI))
            {
                azureLanguage = new AzureLanguage(azureRegion, azuretextAnalyticsAPI);
            }

            string watsonLanguageUrl = configuration.GetString("watsonLanguageUrl");
            string watsontextAnalyticsAPI = configuration.GetString("watsonTextAnalyticsAPI");

            if (!string.IsNullOrWhiteSpace(watsontextAnalyticsAPI) && !string.IsNullOrWhiteSpace(watsonLanguageUrl))
            {
                watsonLanguage = new WatsonLanguage(watsontextAnalyticsAPI, watsonLanguageUrl);
            }

        }

        public bool Init()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    new TCException($"Power BI Client - Missing Username - please check configuration").Log();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    new TCException($"Power BI Client - Missing Password - please check configuration").Log();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AuthorityUrl))
                {
                    new TCException($"Power BI Client - Missing AuthorityUrl - please check configuration").Log();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ResourceUrl))
                {
                    new TCException($"Power BI Client - Missing ResourceUrl - please check configuration").Log();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ClientId))
                {
                    new TCException($"Power BI Client - Missing ClientId - please check configuration").Log();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ApiUrl))
                {
                    new TCException($"Power BI Client - Missing ApiUrl - please check configuration").Log();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(DataSetName))
                {
                    new TCException($"Power BI Client - Missing DataSetName - please check configuration").Log();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(TableName))
                {
                    new TCException($"Power BI Client - Missing TableName - please check configuration").Log();
                    return false;
                }

                bool status = false;

                // Silently get an app auth token
                token = GetAppAuthToken();

                if (string.IsNullOrWhiteSpace(token))
                {
                    new TCException($"Power BI Client - auth token failed - please check configuration").Log();
                    return false;
                }

                groupId = string.Empty;

                // If group name is blank then revert to 'my' workspace
                if (!string.IsNullOrWhiteSpace(GroupName))
                {
                    List<Models.Group> groups = GetGroups();
                    if (groups != null && groups.Any())
                    {
                        groupId = groups.FirstOrDefault(d => d.Name == GroupName).Id;
                    }
                    if (string.IsNullOrWhiteSpace(groupId))
                    {
                        new TCException($"Power BI Client - Group '{GroupName}' not found - please check configuration").Log();
                        return false;
                    }
                }

                //Get the datasets and if it doesn't exist create it 
                List<Dataset> datasets = GetDatasets(groupId);
                if (datasets == null || !datasets.Any() || datasets.FirstOrDefault(d => d.name == DataSetName) == null)
                {
                    //Create a dataset and table in Power BI
                    CreateDataset(groupId, DataSetName, TableName);
                    datasets = GetDatasets(groupId);
                }

                if (datasets != null && datasets.Any() && datasets.FirstOrDefault(d => d.name == DataSetName) != null)
                {
                    datasetId = datasets.FirstOrDefault(d => d.name == DataSetName).id;

                    if (!string.IsNullOrWhiteSpace(datasetId))
                    {
                        List<Table> tables = GetTables(groupId, datasetId);
                        if (tables != null && !tables.Any(t => t.name == TableName))
                        {
                            new TCException($"Power BI Client - Table '{TableName}' not found - please check configuration").Log();
                            return false;
                        }

                        status = true;
                    }
                    else
                    {
                        new TCException($"Power BI Client - Could not determine id for dataset '{DataSetName}' - please check configuration").Log();
                    }
                }
                else
                {
                    new TCException($"Power BI Client - Could not locate dataset '{DataSetName}' - please check configuration").Log();
                }
                return status;
            }
            catch (Exception e)
            {
                new TCException($"Power BI Client - Init - Exception", e).Log();
                return false;
            }
        }

        public bool GetToken()
        {
            try
            {
                // Silently get an app auth token
                token = GetAppAuthToken();

                if (string.IsNullOrWhiteSpace(token))
                {
                    new TCException($"Power BI Client - auth token failed - please check configuration").Log();
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                new TCException($"Power BI Client - Token - Exception", e).Log();
                return false;
            }
        }

        public bool Upload(List<SearchIndexDocument> docs)
        {
            try
            {
                if (docs != null && docs.Count > 0)
                {

                    return AddRows(groupId, datasetId, TableName, docs);
                }
                else
                {
                    return false;
                }
            }

            catch (Exception e)
            {
                new TCException($"Power BI Client - Upload - Exception", e).Log();
                return false;
            }
        }


        public bool UploadUserProfiles(PagedList<User> users, List<string> fields)
        {
            try
            {
                if (users != null && users.Count > 0)
                {
                    return AddUserProfiles(users, fields, groupId, datasetId);
                }
                else
                {
                    return false;
                }
            }

            catch (Exception e)
            {
                new TCException($"Power BI Client - UploadProfile - Exception", e).Log();
                return false;
            }
        }

        public bool DeleteTableRows(string tableName = "UserProfiles")
        {
            try
            {
                return DeleteRows(groupId, datasetId, tableName);
            }

            catch (Exception e)
            {
                new TCException($"Power BI Client - UploadProfile - Exception", e).Log();
                return false;
            }
        }


        private static string GetAppAuthToken()
        {
            /* issue when user does not have access to the application
             * 
             * Message=AADSTS65001: The user or administrator has not consented to use the application with ID 'xxxxxxxxxxxxxxxxxxx' named 'yyyyyyyyyyyyyyyyyyyyy'. 
             * Send an interactive authorization request for this user and resource.
             * https://community.powerbi.com/t5/Developer/Authorization-error-when-trying-to-Embed-using-sample-for-Non/m-p/196958
             * 
             * In summary
             * 
             * As you are not logging in interactively you need to grant the permissions via the azure portal
             * 
             * Login to azure portal as the power bi app user you created in azure ad and used to create the app in power bi
             * 
             * Find your app in "Azure Active Directory" -> "App registrations" (you may need to choose all apps rather than my apps)
             * Click and choose "settings" -> "required permissions"
             * Choose "Power BI Service" -> click on the required permissions (all of them)
             * Save
             * Finally - make sure you click on the "grant permissions" link in the "Required Permissions" panel to the left.......
            */

            // Create a user password cradentials.
            var credential = new UserPasswordCredential(Username, Password);

            // Authenticate using created credentials
            var authenticationContext = new AuthenticationContext(AuthorityUrl);
            var authenticationResult = authenticationContext.AcquireTokenAsync(ResourceUrl, ClientId, credential).GetAwaiter().GetResult();

            if (authenticationResult != null && !string.IsNullOrWhiteSpace(authenticationResult.AccessToken))
            {
                return authenticationResult.AccessToken;
            }

            return string.Empty;
        }

        /// <summary>
        //{
        //	"name": "Community", 
        //	"tables": [
        //		{"name": "Posts", "columns": [
        //				{ "name": "Id", "dataType": "string"},
        //				{ "name": "Name", "dataType": "string"},
        //				{ "name": "Category", "dataType": "string"},
        //				{ "name": "IsCompete", "dataType": "bool"},
        //				{ "name": "ManufacturedOn", "dataType": "DateTime"}
        //			]
        //		}
        //	]
        //}
        /// </summary>
        #region Create a dataset in Power BI
        private bool CreateDataset(string groupId, string datasetName, string tableName)
        {
            string powerBIDatasetsApiUrl = $"{ApiUrl}v1.0/myorg/datasets";
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                //To create a Dataset in a group, use the Groups uri
                powerBIDatasetsApiUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets";
            }

            Dataset dataset = new Dataset()
            {
                name = datasetName,
                tables = new List<Table>()
                {
                    new Table()
                    {
                        name = tableName,
                        columns = new List<Column>()
                        {
                            new Column() { name = "Id", dataType = "string" },
                            new Column() { name = "DocumentType", dataType = "string" },
                            new Column() { name = "Author", dataType = "string" },
                            new Column() { name = "Title", dataType = "string" },
                            new Column() { name = "Content", dataType = "string" },
                            new Column() { name = "RawContent", dataType = "string" },
                            new Column() { name = "Tags", dataType = "string" },
                            new Column() { name = "KeyPhrases1", dataType = "string" },
                            new Column() { name = "KeyPhrases2", dataType = "string" },
                            new Column() { name = "KeyPhrases3", dataType = "string" },
                            new Column() { name = "KeyPhrases4", dataType = "string" },
                            new Column() { name = "KeyPhrases5", dataType = "string" },
                            new Column() { name = "CreatedOn", dataType = "DateTime" },
                            new Column() { name = "UpdatedOn", dataType = "DateTime" }
                        }
                    },
                    new Table()
                    {
                        name = "UserProfiles",
                        columns = new List<Column>()
                        {
                            new Column() { name = "Id", dataType = "string" },
                            new Column() { name = "UserName", dataType = "string" },
                            new Column() { name = "JoinedOn", dataType = "DateTime" },
                            new Column() { name = "LastVisitedOn", dataType = "DateTime" },
                            new Column() { name = "UpdatedOn", dataType = "DateTime" },
                            new Column() { name = "TotalPosts", dataType = "Int64" },
                            new Column() { name = "AgeGroup", dataType = "string" }
                        }
                    },
                    new Table()
                    {
                        name = "Keywords",
                        columns = new List<Column>()
                        {
                            new Column() { name = "Id", dataType = "string" },
                            new Column() { name = "Keyword", dataType = "string" },
                            new Column() { name = "CreatedOn", dataType = "DateTime" },
                            new Column() { name = "Source", dataType = "string" }
                        }
                    }
                },
                relationships = new List<Relationship>()
                {
                    new Relationship()
                    {
                        name = Guid.NewGuid().ToString(),
                        fromTable = tableName,
                        toTable = "UserProfiles",
                        fromColumn = "Author",
                        toColumn = "UserName",
                        crossFilteringBehavior = "bothDirections"
                    },
                    new Relationship()
                    {
                        name = Guid.NewGuid().ToString(),
                        fromTable = tableName,
                        toTable = "Keywords",
                        fromColumn = "Id",
                        toColumn = "Id",
                        crossFilteringBehavior = "bothDirections"
                    }

                }
            };

            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dataset, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            //POST web request
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBIDatasetsApiUrl, "POST", byteArray, ref response);
            }

            return success;

        }
        #endregion

        #region Get a list of the available groups/workspaces for the current user
        public List<Models.Group> GetGroups()
        {
            string powerBIApiUrl = $"{ApiUrl}v1.0/myorg/groups";

            //GET web request
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBIApiUrl, "GET", null, ref response);
            }

            if (success)
            {
                JavaScriptSerializer json = new JavaScriptSerializer();
                Models.Groups groups = (Models.Groups)json.Deserialize(response, typeof(Models.Groups));

                return groups.value;

            }
            else
            {
                return null;
            }
        }
        #endregion


        #region Get a list of the available datasets in the current group
        private List<Dataset> GetDatasets(string groupId = null)
        {
            //To get a datasets in a 'my' group 
            string powerBIDatasetsApiUrl = $"{ApiUrl}v1.0/myorg/datasets";

            if (!string.IsNullOrWhiteSpace(groupId))
            {
                //To get a datasets in a group, use the Groups uri
                powerBIDatasetsApiUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets";
            }

            //GET web request to list all datasets.  
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBIDatasetsApiUrl, "GET", null, ref response);
            }

            if (success)
            {
                JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                Datasets datasets = (Datasets)jsonSerializer.Deserialize(response, typeof(Datasets));

                return datasets.value;

            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Get a list of the available tables in a datasets
        private List<Table> GetTables(string groupId, string datasetId)
        {
            //To get a list of tables in a dataset in a 'my' group 
            string powerBITablesApiUrl = $"{ApiUrl}v1.0/myorg/datasets/{datasetId}/tables";

            if (!string.IsNullOrWhiteSpace(groupId))
            {
                //To get a list of tables in a dataset in a group, use the Groups uri
                powerBITablesApiUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets/{datasetId}/tables";
            }

            //GET web request to list all tables.  
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBITablesApiUrl, "GET", null, ref response);
            }

            if (success)
            {
                JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                Tables tables = (Tables)jsonSerializer.Deserialize(response, typeof(Tables));

                return tables.value;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Update the user profile table schema
        public bool UpdateUserProfileSchema(List<string> fields, string tableName = "UserProfiles")
        {
            //To create a new table in dataset 'my' group
            string powerBIApiAddTableUrl = $"{ApiUrl}v1.0/myorg/datasets/{datasetId}/tables/{tableName}";
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                //To create a new table in a dataset in a group, use the Groups uri
                powerBIApiAddTableUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets/{datasetId}/tables/{tableName}";
            }

            Table table = new Table()
            {
                name = tableName,
                columns = new List<Column>()
                {
                    new Column() { name = "Id", dataType = "string" },
                    new Column() { name = "UserName", dataType = "string" },
                    new Column() { name = "JoinedOn", dataType = "DateTime" },
                    new Column() { name = "LastVisitedOn", dataType = "DateTime" },
                    new Column() { name = "UpdatedOn", dataType = "DateTime" },
                    new Column() { name = "TotalPosts", dataType = "Int64" },
                    new Column() { name = "AgeGroup", dataType = "string" }
                }
            };

            foreach (var field in Helpers.UserProfile.GetUserProfileFields().Where(k => fields.Contains(k.Name) || fields.Count == 0))
            {
                table.columns.Add(new Column() { name = field.Title, dataType = Helpers.UserProfile.GetDataType(field) });
            }

            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(table, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            //POST web request
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBIApiAddTableUrl, "PUT", byteArray, ref response);
            }

            return success;
        }
        #endregion

        #region Purge all rows to a Power BI table
        private bool DeleteRows(string groupId, string datasetId, string tableName)
        {
            //To add rows to a dataset in 'my' group
            string powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/datasets/{datasetId}/tables/{tableName}/rows";
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                //To add rows to a dataset in a group, use the Groups uri
                powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets/{datasetId}/tables/{tableName}/rows";
            }

            //POST web request
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBIApiRowsUrl, "DELETE", null, ref response);
            }

            return success;
        }
        #endregion


        #region Add rows to a Power BI table
        private bool AddRows(string groupId, string datasetId, string tableName, List<SearchIndexDocument> docs)
        {
            //To add rows to a dataset in 'my' group
            string powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/datasets/{datasetId}/tables/{tableName}/rows";
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                //To add rows to a dataset in a group, use the Groups uri
                powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets/{datasetId}/tables/{tableName}/rows";
            }

            string delimiter = ",";
            string rowdelimiter = "";

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"rows\":[");
            int keywords = 0;

            Rows rows = new Rows()
            {
                rows = new List<Row>()
            };

            foreach (var doc in docs)
            {
                string keyPhrases1 = string.Empty;
                string keyPhrases2 = string.Empty;
                IList<string> tags = new List<String>();
                foreach (var tag in doc.IndexFields.Where(a => a.FieldName == "tag"))
                {
                    tags.Add(tag.FieldValue);
                }

                if (azureLanguage != null || watsonLanguage != null)
                {
                    string allContent = doc.Title + " " + doc.Content + " " + String.Join(" ", tags.Select(x => x.ToString()).ToArray());

                    if (azureLanguage != null)
                    {
                        keyPhrases1 = azureLanguage.KeyPhrasesCSV(allContent);
                        if (!string.IsNullOrWhiteSpace(keyPhrases1))
                        {
                            foreach (var keyword in keyPhrases1.Split(','))
                            {
                                keywords++;
                                sb.Append(rowdelimiter);
                                sb.Append("{");
                                sb.Append($"\"Id\":\"{doc.Id}\"");
                                sb.Append($"{delimiter}\"Keyword\":\"{keyword}\"");
                                sb.Append($"{delimiter}\"Source\":\"Azure\"");
                                sb.Append($"{delimiter}\"CreatedOn\":\"{Helpers.UserProfile.FormatDate(GetIndexField(doc, "date"))}\"");
                                sb.Append("}");
                                rowdelimiter = ",";
                            }
                        }
                    }
                    if (watsonLanguage != null)
                    {
                        keyPhrases2 = watsonLanguage.KeyPhrasesCSV(allContent);
                        if (!string.IsNullOrWhiteSpace(keyPhrases2))
                        {
                            foreach (var keyword in keyPhrases2.Split(','))
                            {
                                keywords++;
                                sb.Append(rowdelimiter);
                                sb.Append("{");
                                sb.Append($"\"Id\":\"{doc.Id}\"");
                                sb.Append($"{delimiter}\"Keyword\":\"{keyword}\"");
                                sb.Append($"{delimiter}\"Source\":\"Watson\"");
                                sb.Append($"{delimiter}\"CreatedOn\":\"{Helpers.UserProfile.FormatDate(GetIndexField(doc, "date"))}\"");
                                sb.Append("}");
                                rowdelimiter = ",";
                            }
                        }
                    }
                }

                rows.rows.Add(new Row()
                {
                    Id = doc.Id,
                    DocumentType = doc.TypeName,
                    Title = doc.Title,
                    Content = doc.Content,
                    RawContent = GetIndexField(doc, "rawcontent"),
                    Author = GetIndexField(doc, "username"),
                    Tags = String.Join(",", tags.Select(x => x.ToString()).ToArray()),
                    KeyPhrases1 = keyPhrases1,
                    KeyPhrases2 = keyPhrases2,
                    KeyPhrases3 = string.Empty,
                    KeyPhrases4 = string.Empty,
                    KeyPhrases5 = string.Empty,
                    CreatedOn = Helpers.UserProfile.FormatDate(GetIndexField(doc, "date")),
                    UpdatedOn = Helpers.UserProfile.FormatDate()
                });
            }

            sb.Append("]}");

            //POST web request
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(rows, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            //POST web request
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBIApiRowsUrl, "POST", byteArray, ref response);
            }

            if (success && keywords > 0)
            {

                //To add rows to keywords dataset in 'my' group
                powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/datasets/{datasetId}/tables/Keywords/rows";
                if (!string.IsNullOrWhiteSpace(groupId))
                {
                    //To add rows to keywords dataset in a group, use the Groups uri
                    powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets/{datasetId}/tables/Keywords/rows";
                }

                //POST web request
                byteArray = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

                //POST web request
                attempt = 1;
                success = false;
                response = string.Empty;

                while (!success && attempt <= 3)
                {
                    attempt++;
                    success = SendRequest(powerBIApiRowsUrl, "POST", byteArray, ref response);
                }

            }

            return success;
        }
        #endregion

        #region Add rows to a Power BI UserProfile
        private bool AddUserProfiles(PagedList<User> users, List<string> fields, string groupId, string datasetId, string tableName = "UserProfiles")
        {
            //To add rows to a dataset in 'my' group
            string powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/datasets/{datasetId}/tables/{tableName}/rows";
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                //To add rows to a dataset in a group, use the Groups uri
                powerBIApiRowsUrl = $"{ApiUrl}v1.0/myorg/groups/{groupId}/datasets/{datasetId}/tables/{tableName}/rows";
            }

            string delimiter = ",";
            string rowdelimiter = "";

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"rows\":[");

            foreach (var user in users)
            {
                sb.Append(rowdelimiter);
                sb.Append("{");
                sb.Append($"\"Id\":\"{user.Id}\"");
                sb.Append($"{delimiter}\"UserName\":\"{user.Username}\"");
                sb.Append($"{delimiter}\"JoinedOn\":\"{Helpers.UserProfile.FormatDateTime(user.JoinDate)}\"");
                sb.Append($"{delimiter}\"LastVisitedOn\":\"{Helpers.UserProfile.FormatDateTime(user.LastVisitedDate)}\"");
                sb.Append($"{delimiter}\"UpdatedOn\":\"{Helpers.UserProfile.FormatDate()}\"");
                sb.Append($"{delimiter}\"TotalPosts\":{user.TotalPosts}");
                
                foreach (var field in Helpers.UserProfile.GetUserProfileFields().Where(k => fields.Contains(k.Name) || fields.Count == 0))
                {
                    sb.Append($"{delimiter}\"{field.Title}\":{Helpers.UserProfile.ExtractUserProfileValue(user, field)}");
                }
                sb.Append("}");

                rowdelimiter = ",";
            }
            sb.Append("]}");

            //POST web request
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

            //POST web request
            int attempt = 1;
            var success = false;
            string response = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(powerBIApiRowsUrl, "POST", byteArray, ref response);
            }

            return success;
        }
        #endregion

        private bool SendRequest(string url, string method, byte[] content, ref string response)
        {
            bool status = false;
            response = string.Empty;

            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

                request.Method = method;
                request.KeepAlive = true;
                request.ContentLength = 0;
                request.ContentType = "application/json";

                //Add token to the request header
                request.Headers.Add("Authorization", String.Format("Bearer {0}", token));

                if (content != null)
                {
                    request.ContentLength = content.Length;

                    //Write JSON byte[] into a Stream
                    using (Stream writer = request.GetRequestStream())
                    {
                        writer.Write(content, 0, content.Length);
                    }
                }

                using (HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse())
                {
                    //Get StreamReader that holds the response stream  
                    using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        response = reader.ReadToEnd();
                        status = true;
                    }
                }
            }
            catch (WebException e)
            {
                using (WebResponse errorResponse = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)errorResponse;

                    // invalid token
                    if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        // Silently get an app auth token
                        token = GetAppAuthToken();
                    }

                    // too many requests
                    if ((int)httpResponse.StatusCode == 429)
                    {
                        System.Threading.Thread.Sleep(1000);

                        // Silently get an app auth token
                        token = GetAppAuthToken();
                    }

                    using (Stream data = errorResponse.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        response = reader.ReadToEnd();
                        new TCException($"Power BI Client - Request failed - Status {httpResponse.StatusCode} - Response {response}").Log();
                    }

                }
            }
            return status;
        }

        private string GetIndexField(SearchIndexDocument doc, string name)
        {
            string value = string.Empty;

            var indexField = doc.IndexFields.FirstOrDefault(a => a.FieldName == name);
            if (indexField != null && !string.IsNullOrWhiteSpace(indexField.FieldValue))
            {
                value = indexField.FieldValue;
            }

            return value;
        }
    }
}

