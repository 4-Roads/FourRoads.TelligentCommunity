using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.HubSpot.Models
{

    public class Properties
    {
        public List<Property> properties { get; set; }
    }

    public class Property
    {
        public string property { get; set; }
        public dynamic value { get; set; }
    }
}
