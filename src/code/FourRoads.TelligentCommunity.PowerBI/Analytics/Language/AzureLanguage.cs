using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FourRoads.TelligentCommunity.PowerBI.Analytics.Language
{
    public class AzureLanguage
    {

        private ITextAnalyticsAPI client;

        /// <summary>
        /// Container for subscription credentials. Make sure to enter your valid key.
        /// </summary>
        private class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public ApiKeyServiceClientCredentials(string apikey)
            {
                this.apikey = apikey;
            }

            private string apikey;
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", apikey);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }

        public AzureLanguage(string azureRegion , string apiKey)
        {
            client = new TextAnalyticsAPI(new ApiKeyServiceClientCredentials(apiKey));

            client.AzureRegion = Enum.GetValues(typeof(AzureRegions)).OfType<AzureRegions>().FirstOrDefault(x => x.ToString() == azureRegion);
        }

        public string KeyPhrasesCSV(string text)
        {
            return String.Join(",", KeyPhrases(text).Select(x => x.ToString()).ToArray());
        }

        public IList<string> KeyPhrases(string text)
        {
            List<string> keyPhrases = new List<string>();

            KeyPhraseBatchResult result = client.KeyPhrasesAsync(
                new MultiLanguageBatchInput(
                    new List<MultiLanguageInput>()
                    {
                        new MultiLanguageInput("en", "1", text.Substring(0, Math.Min(5000, text.Length)).Replace('"', ' '))
                    })).Result;

            if (result != null && result.Documents.Any()) {
                foreach (var document in result.Documents)
                {
                    keyPhrases.AddRange(document.KeyPhrases);
                }
            }
            return keyPhrases;
        }

    }
}
