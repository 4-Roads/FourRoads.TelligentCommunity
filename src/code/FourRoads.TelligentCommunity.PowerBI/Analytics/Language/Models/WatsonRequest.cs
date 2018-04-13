namespace FourRoads.TelligentCommunity.PowerBI.Analytics.Language.Models
{
    public class WatsonRequest
    {
        public string text { get; set; }
        public Features features { get; set; }
    }

    public class Features
    {
        public Keywords keywords { get; set; }
    }

    public class Keywords
    {
        public int limit { get; set; }
    }
}

