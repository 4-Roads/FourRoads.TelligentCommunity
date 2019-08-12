using System;

namespace FourRoads.TelligentCommunity.GoogleMfa.Model
{
    public class OneTimeCodesStatus
    {
        public DateTime CodesGeneratedOn { get; set; }
        public int CodesLeft { get; set; }

        public int Version { get; set; }
    }
}
