using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Api.Plugins.Administration;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Urls.Routing;

namespace FourRoads.TelligentCommunity.StopForumSpam
{
    public class StopForumSpamPlugin : IAbuseDetector, IConfigurablePlugin
    {
        private IAbuseController _abuseController;
        private IPluginConfiguration _configuration;

        public void Initialize()
        {
            Apis.Get<IContents>().Events.AfterCreate += EventsOnAfterCreate;
        }

        private void EventsOnAfterCreate(ContentAfterCreateEventArgs e)
        {
            var userService = Apis.Get<IUsers>();
            // Ensure the content can be moderated and that the content is a comment.
            if (_abuseController.SupportsAbuse(e.ContentTypeId) && e.ContentTypeId == userService.ContentTypeId)
            {
                try
                {
                    var moderatedUser = userService.Get(new UsersGetOptions() {ContentId = e.ContentId});

                    List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();

                    postData.Add(new KeyValuePair<string, string>("json" , "{''}"));

                    if (_configuration.GetBool("useIP"))
                        postData.Add(new KeyValuePair<string, string>("ip", CSContext.Current.UserHostAddress));

                    if (_configuration.GetBool("useEmail"))
                        postData.Add(new KeyValuePair<string, string>("email", moderatedUser.PrivateEmail));

                    if (_configuration.GetBool("useUserName"))
                        postData.Add(new KeyValuePair<string, string>("username", moderatedUser.Username));

                    HttpClient client = new HttpClient();

                    var content = new FormUrlEncodedContent(postData.ToArray());

                    var result = client.PostAsync(_configuration.GetString("apiUrl") +"?confidence", content).Result;

                    var resultString =  result.Content.ReadAsStringAsync().Result;

                    dynamic resultJson = JsonConvert.DeserializeObject<dynamic>(resultString);

                    double confidence = 0;

                    if (resultJson.ip != null)
                    {
                        double confInt;
                        double.TryParse(resultJson.ip.confidence?.ToString(), out confInt);
                        confidence += confInt;
                    }

                    if (resultJson.username != null)
                    {
                        double confInt;
                        double.TryParse(resultJson.username.confidence?.ToString(), out confInt);
                        confidence += confInt;
                    }

                    if (resultJson.email != null)
                    {
                        double confInt;
                        double.TryParse(resultJson.email.confidence?.ToString(), out confInt);
                        confidence += confInt;
                    }

                    Apis.Get<IEventLog>().Write($"Stop Forum Spam User Tested {moderatedUser.PrivateEmail} confidence:{confidence}", new EventLogEntryWriteOptions() { EventType = "Information" });

                    if (confidence > _configuration.GetInt("threashold"))
                        _abuseController.Moderate(e.ContentId, e.ContentTypeId);
                }
                catch (Exception ex)
                {
                    Apis.Get<IEventLog>().Write("Absue checking for StopForumSpam Plugin Failed: " + ex, new EventLogEntryWriteOptions() {EventType = "Error"});
                }
            }
        }
        public string Name => "4 Roads - StopForumSpam Checker";
        public string Description => "Tests user creation against StopForumSpam";

        public string GetAbuseExplanation(Guid contentId, Guid contentTypeId)
        {
            return "This content required moderation";
        }

        public void Register(IAbuseController controller)
        {
            _abuseController = controller;
        }

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup[] groupArray = new PropertyGroup[1];
                PropertyGroup optionsGroup = new PropertyGroup("options", "Options", 0);
                groupArray[0] = optionsGroup;

                optionsGroup.Properties.Add(new Property("useIP", "Send IP for Testing", PropertyType.Bool, 1, bool.TrueString));
                optionsGroup.Properties.Add(new Property("useEmail", "Send Email for Testing", PropertyType.Bool, 1, bool.TrueString));
                optionsGroup.Properties.Add(new Property("useUserName", "Send UserName for Testing", PropertyType.Bool, 1, bool.TrueString));
                optionsGroup.Properties.Add(new Property("threashold", "Score Threashold", PropertyType.Int, 1,"50"));
                optionsGroup.Properties.Add(new Property("apiUrl", "API Url", PropertyType.String, 1, "http://api.stopforumspam.org/api"));

                return groupArray;
            }
        }
    }
}
