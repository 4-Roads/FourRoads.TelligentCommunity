using System;

namespace FourRoads.TelligentCommunity.Mfa.Model
{
    public class OneTimeCode
    {
        public OneTimeCode()
        {
            Id = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
        }
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string PlainTextCode { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? RedeemedOnUtc { get; set; }
    }
}
