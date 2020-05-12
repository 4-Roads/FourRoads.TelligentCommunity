using Newtonsoft.Json;

namespace FourRoads.TelligentCommunity.InstagramFeed.Models
{
    public class BasePageSearchResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
