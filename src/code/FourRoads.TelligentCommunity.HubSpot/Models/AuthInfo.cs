using System;

namespace FourRoads.TelligentCommunity.HubSpot.Models
{
    public class AuthInfo
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiryUtc { get; set; }
        public bool Expired => DateTime.UtcNow > ExpiryUtc;
    }
}