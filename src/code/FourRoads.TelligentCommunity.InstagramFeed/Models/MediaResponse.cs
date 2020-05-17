using Newtonsoft.Json;
using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.InstagramFeed.Models
{
    public class MediaResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; }

        [JsonProperty("paging")]
        public Pagination Paging { get; set; }
    }

    public class Pagination
    {
        [JsonProperty("next")]
        public string Next { get; set; }
    }
}
