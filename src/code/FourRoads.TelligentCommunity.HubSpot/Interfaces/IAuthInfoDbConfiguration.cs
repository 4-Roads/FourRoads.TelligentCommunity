using System;
using FourRoads.TelligentCommunity.HubSpot.Models;

namespace FourRoads.TelligentCommunity.HubSpot.Interfaces
{
    public interface IAuthInfoDbConfiguration
    {
        AuthInfo Get(string clientId);
        void Clear(string clientId);
        AuthInfo Update(string clientId, string accessToken, string refreshToken, DateTime expiryUtc);
    }
}