using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using Newtonsoft.Json;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using TelligentConfiguration = Telligent.Evolution.Extensibility.Configuration.Version1;

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
                    userService.RunAsUser(
                        userService.ServiceUserName,
                        () =>
                        {
                            var moderatedUser = userService.Get(new UsersGetOptions() {ContentId = e.ContentId});

                            List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();

                            postData.Add(new KeyValuePair<string, string>("json", "{''}"));

                            if (_configuration.GetBool("useIP").HasValue && _configuration.GetBool("useIP").Value)
                                postData.Add(new KeyValuePair<string, string>("ip", CSContext.Current.UserHostAddress));

                            if (_configuration.GetBool("useEmail").HasValue && _configuration.GetBool("useEmail").Value)
                                postData.Add(new KeyValuePair<string, string>("email", moderatedUser.PrivateEmail));

                            if (_configuration.GetBool("useUserName").HasValue && _configuration.GetBool("useUserName").Value)
                                postData.Add(new KeyValuePair<string, string>("username", moderatedUser.Username));

                            HttpClient client = new HttpClient();

                            var content = new FormUrlEncodedContent(postData.ToArray());

                            var result = client.PostAsync(_configuration.GetString("apiUrl") + "?confidence", content).Result;

                            var resultString = result.Content.ReadAsStringAsync().Result;

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

                            Apis.Get<IEventLog>().Write($"Stop Forum Spam User Tested {moderatedUser.PrivateEmail} confidence:{confidence}", new EventLogEntryWriteOptions() {EventType = "Information"});


                            if (confidence > _configuration.GetInt("threashold"))
                            {
                                _abuseController.IdentifyAsAbusive(e.ContentId, e.ContentTypeId);


                                userService.Update(new UsersUpdateOptions() {Id = moderatedUser.Id, AccountStatus = "ApprovalPending", ForceLogin = true});
                            }
                        });
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

        public TelligentConfiguration.PropertyGroup[] ConfigurationOptions
        {
            get
            {
                TelligentConfiguration.PropertyGroup[] groupArray = new TelligentConfiguration.PropertyGroup[1];
                TelligentConfiguration.PropertyGroup optionsGroup = new TelligentConfiguration.PropertyGroup() {Id="options", LabelText = "Options"};
                groupArray[0] = optionsGroup;

                optionsGroup.Properties.Add(new TelligentConfiguration.Property
                {
                    Id = "useIP",
                    LabelText = "Send IP for Testing",
                    DataType = "bool",
                    Template = "bool",
                    OrderNumber = 0,
                    DefaultValue = bool.TrueString
                });

                optionsGroup.Properties.Add(new TelligentConfiguration.Property
                {
                    Id = "useEmail",
                    LabelText = "Send Email for Testing",
                    DataType = "bool",
                    Template = "bool",
                    OrderNumber = 0,
                    DefaultValue = bool.TrueString
                });

                optionsGroup.Properties.Add(new TelligentConfiguration.Property
                {
                    Id = "useUserName",
                    LabelText = "Send UserName for Testing",
                    DataType = "bool",
                    Template = "bool",
                    OrderNumber = 0,
                    DefaultValue = bool.TrueString
                });

                optionsGroup.Properties.Add(new TelligentConfiguration.Property
                {
                    Id = "threashold",
                    LabelText = "Score Threashold, above this threshold users are automatically banned",
                    DataType = "int",
                    Template = "int",
                    DefaultValue = "50",
                    Options = new NameValueCollection
                    {
                        { "presentationDivisor", "1" },
                        { "inputType", "number" },
                    }
                });

                optionsGroup.Properties.Add(new TelligentConfiguration.Property
                {
                    Id = "apiUrl",
                    LabelText = "API Url",
                    DataType = "url",
                    Template = "url",
                    OrderNumber = 0,
                    DefaultValue = "http://api.stopforumspam.org/api"
                });

                return groupArray;
            }
        }
    }
}
