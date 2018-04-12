using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourRoads.TelligentCommunity.PowerBI.Analytics.Language
{
    public class AzureLanguage
    {

        private ITextAnalyticsAPI client;

        public AzureLanguage(string azureRegion , string apiKey)
        {
            client = new TextAnalyticsAPI();

            client.AzureRegion = Enum.GetValues(typeof(AzureRegions)).OfType<AzureRegions>().FirstOrDefault(x => x.ToString() == azureRegion);
            client.SubscriptionKey = apiKey;
        }

        public string KeyPhrasesCSV(string text)
        {
            return String.Join(",", KeyPhrases(text).Select(x => x.ToString()).ToArray());
        }

        public IList<string> KeyPhrases(string text)
        {
            List<string> keyPhrases = new List<string>();

            KeyPhraseBatchResult result = client.KeyPhrases(
                new MultiLanguageBatchInput(
                    new List<MultiLanguageInput>()
                    {
                        new MultiLanguageInput("en", "1", text.Substring(0, Math.Min(5000, text.Length)).Replace('"', ' '))
                    }));

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
