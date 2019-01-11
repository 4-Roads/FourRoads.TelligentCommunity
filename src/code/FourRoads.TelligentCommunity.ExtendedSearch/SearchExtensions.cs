using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Xml;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.ExtendedSearch
{
    public class SearchExtensions
    {
        public IEnumerable<Suggestion> GetSuggestions(string query)
        {
            return GetSuggestions(query , int.MaxValue);
        }

        public IEnumerable<Suggestion> GetSuggestions(string query, int max)
        {
            var solrConnection = ConfigurationManager.ConnectionStrings["SearchContentUrl"].ConnectionString;

            var queryUrl = $"{solrConnection}select?q={query}";

            using (var client = new WebClient())
            {
                var content = client.DownloadString(queryUrl);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(content);

                List<Suggestion> suggestions = new List<Suggestion>();
                int counter = 0;

                foreach (XmlElement node in doc.SelectNodes("//lst[@name=\'suggestions\']/*"))
                {
                    string name = node.Attributes["name"]?.Value;
                    foreach (XmlText suggestion in node.SelectNodes($"//lst[@name=\'{name}\']/arr/str/text()"))
                    {
                        var queryReplace = query.Replace(name, suggestion.Value);

                        suggestions.Add(new Suggestion() {Replacing = name, Text = suggestion.Value, FullQuery= queryReplace,  Url = Apis.Get<ICoreUrls>().Search(new CoreUrlSearchOptions() {QueryString = $"q={queryReplace}" })});

                        counter++;

                        if (counter == max)
                            return suggestions;
                    }
                }

                return suggestions;
            }
        }
    }
}