using System;

namespace FourRoads.TelligentCommunity.GoogleMfa.Interfaces
{
    public interface IMfaDataProvider
    {
        void SetUserState(int userId, string sessionId, bool passed);
        bool GetUserState(int userId, string sessionId);
    }
}
