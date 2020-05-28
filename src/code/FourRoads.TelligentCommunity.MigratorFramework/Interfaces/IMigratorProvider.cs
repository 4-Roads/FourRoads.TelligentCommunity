using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigratorProvider : ISingletonPlugin
    {
        IMigrationFactory GetFactory();
    }
}