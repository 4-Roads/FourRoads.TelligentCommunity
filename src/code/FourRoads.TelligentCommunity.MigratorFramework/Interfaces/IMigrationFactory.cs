using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationFactory
    {
        void SignalMigrationStarting();
        IEnumerable<string> GetOrderObjectHandlers();
        IMigrationObjectHandler GetHandler(string objectType);
        void SignalMigrationFinshing();
    }
}