using Newtonsoft.Json;

namespace FourRoads.TelligentCommunity.InstagramFeed.Models
{
    public class InstagramBusinessAccountResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("instagram_business_account")]
        public InstagramBusinessAccount Account { get; set; }
    }

    public class InstagramBusinessAccount
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
