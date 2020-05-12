using Newtonsoft.Json;

namespace FourRoads.TelligentCommunity.InstagramFeed.Models
{
    public class Media : BasePageSearchResponse
    {
        [JsonProperty("caption")]
        public string Caption { get; set; }

        [JsonProperty("media_type")]
        public string MediaType { get; set; }

        [JsonProperty("media_url")]
        public string MediaUrl { get; set; }

        [JsonProperty("shortcode")]
        public string ShortCode { get; set; }

        [JsonProperty("comments_count")]
        public int CommentsCount { get; set; }

        [JsonProperty("like_count")]
        public int LikeCount { get; set; }
    }
}
