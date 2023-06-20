using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using TelligentConfiguration = Telligent.Evolution.Extensibility.Configuration.Version1;

namespace FourRoads.TelligentCommunity.GPTZero
{
    public class GPTZeroPlugin : IAbuseDetector, IConfigurablePlugin
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
            if (_abuseController.SupportsAbuse(e.ContentTypeId))
            {
                try
                {
                    userService.RunAsUser(
                        userService.ServiceUserName,
                        () =>
                        {

                            HttpClient client = new HttpClient();

                            var content = new StringContent($"{{\"document\": \"{JsonEncodedText.Encode(e.HtmlDescription(("text")).ToString())}\"}}",Encoding.UTF8,
                                "application/json");

                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            client.DefaultRequestHeaders.Add("x-api-key", _configuration.GetString("apiKey"));

                            var result = client.PostAsync("https://api.gptzero.me/v2/predict/text", content).Result;

                            result.EnsureSuccessStatusCode();

                            var resultString = result.Content.ReadAsStringAsync().Result;

                            dynamic resultJson = JsonConvert.DeserializeObject<dynamic>(resultString);

                            if (resultJson.documents[0].completely_generated_prob > _configuration.GetDouble("probability"))
                            {
                                _abuseController.IdentifyAsAbusive(e.ContentId, e.ContentTypeId);
                            }
                        });
                }
                catch (Exception ex)
                {
                    Apis.Get<IEventLog>().Write("Abuse checking for GPTZero Plugin Failed: " + ex, new EventLogEntryWriteOptions() {EventType = "Error"});
                }
            }
        }
        public string Name => "4 Roads - GPTZero Checker";
        public string Description => "Tests body content against GPTZero";

        public string GetAbuseExplanation(Guid contentId, Guid contentTypeId)
        {
            return "This content was detected to possibly be ";
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
                    Id = "apiKey",
                    LabelText = "API Key",
                    DataType = "string",
                    OrderNumber = 0,
                    DefaultValue = string.Empty
                });

                optionsGroup.Properties.Add(new TelligentConfiguration.Property
                {
                    Id = "probability",
                    LabelText = "Probability Threshold",
                    DataType = "double",
                    OrderNumber = 0,
                    DefaultValue = "0.8"
                });


                return groupArray;
            }
        }
    }
}
