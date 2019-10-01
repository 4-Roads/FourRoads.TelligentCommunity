using System;
using FourRoads.TelligentCommunity.GoogleMfa.Model;

namespace FourRoads.TelligentCommunity.GoogleMfa.Interfaces
{
    public interface IMfaDataProvider
    {
        void SetUserState(int userId, string sessionId, bool passed);

        bool GetUserState(int userId, string sessionId);

        /// <summary>
        /// Returns true if code was valid. Marks the code as used to prevent reuse.
        /// </summary>
        /// <param name="userId">user Id</param>
        /// <param name="encryptedCode">One tim code hash value</param>
        /// <returns></returns>
        bool RedeemCode(int userId, string encryptedCode);

        void ClearCodes(int userId);

        OneTimeCode CreateCode(int userId, string encryptedCode);
        int CountCodesLeft(int userId);

        void SetUserKey(int userId, Guid key);
        Guid GetUserKey(int userId);
        void ClearUserKey(int value);
    }
}
