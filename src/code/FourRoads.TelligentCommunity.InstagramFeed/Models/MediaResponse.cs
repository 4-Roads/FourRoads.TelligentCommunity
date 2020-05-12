using Newtonsoft.Json;
using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.InstagramFeed.Models
{
    public class MediaResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; }
    }
}
