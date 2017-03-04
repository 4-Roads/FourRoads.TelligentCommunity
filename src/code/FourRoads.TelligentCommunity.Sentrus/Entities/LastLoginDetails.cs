using System;

namespace FourRoads.TelligentCommunity.Sentrus.Entities
{
    public class LastLoginDetails
    {
        public Guid MembershipId { get; set; }

        public int EmailCountSent { get; set; }

        public DateTime? FirstEmailSentAt { get; set; }

        public DateTime LastLogonDate { get; set; }

        public bool IgnoredUser {get;set;}

        internal static string CacheKey(Guid contentId)
        {
            return "LastLoginDetails:" + contentId.ToString(); 
        }
    }
}
