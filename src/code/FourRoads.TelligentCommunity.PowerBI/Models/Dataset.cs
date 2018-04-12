using Newtonsoft.Json;
using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.PowerBI.Models
{
    public class Dataset
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string id { get; set; }
        public string name { get; set; }
        public List<Table> tables { get; set; }
        public List<Relationship> relationships { get; set; }
    }
}