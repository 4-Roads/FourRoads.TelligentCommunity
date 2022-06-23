using System;
using Google.Authenticator;

namespace FourRoads.TelligentCommunity.Mfa.Logic
{
    class FourRoadsTwoFactorAuthenticator : TwoFactorAuthenticator
    {
        public FourRoadsTwoFactorAuthenticator()
        {
            DefaultClockDriftTolerance = TimeSpan.FromSeconds(60);
        }
    }
}