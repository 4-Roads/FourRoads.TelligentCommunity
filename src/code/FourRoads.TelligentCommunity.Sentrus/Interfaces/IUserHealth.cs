namespace FourRoads.TelligentCommunity.Sentrus.Interfaces
{
    using FourRoads.TelligentCommunity.Sentrus.Entities;
    using System.Collections.Generic;
    using Telligent.Evolution.Extensibility.Api.Entities.Version1;

    public interface IUserHealth
    {
        IEnumerable<User> GetInactiveUsers(int accountAge, bool includeIgnored = false);

        LastLoginDetails GetLastLoginDetails(System.Guid guid);

        void CreateUpdateLastLoginDetails(LastLoginDetails lastLoginData);
    }
}