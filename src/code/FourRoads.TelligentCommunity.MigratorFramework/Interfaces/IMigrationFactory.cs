using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationFactory
    {
        IEnumerable<string> GetOrderObjectHandlers();
        IMigrationObjectHandler GetHandler(string objectType);
    }
}