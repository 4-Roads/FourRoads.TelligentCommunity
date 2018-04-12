namespace FourRoads.TelligentCommunity.PowerBI.Models
{
    public class Relationship
    {
        public string name { get; set; }
        public string fromTable { get; set; }
        public string fromColumn { get; set; }
        public string toTable { get; set; }
        public string toColumn { get; set; }
        public string crossFilteringBehavior { get; set; }
    }
}
