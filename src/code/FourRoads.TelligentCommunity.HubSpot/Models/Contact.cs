namespace FourRoads.TelligentCommunity.HubSpot.Models
{

    public class ContactProperty
    {
        public string name { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        public string groupName { get; set; }
        public string type { get; set; }
        public string fieldType { get; set; }
        public bool? hidden { get; set; }
        public int? displayOrder { get; set; }
        public Option[] options { get; set; }
        public bool? formField { get; set; }
    }

    public class Option
    {
        public string label { get; set; }
        public string value { get; set; }
    }
}
