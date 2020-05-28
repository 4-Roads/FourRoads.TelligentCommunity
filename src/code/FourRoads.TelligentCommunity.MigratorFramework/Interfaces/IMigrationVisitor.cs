using FourRoads.TelligentCommunity.MigratorFramework.Entities;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationVisitor
    {
        void AddUrlRedirect(string source, string destination);

        MigratedData GetMigratedData(string objectType , string sourceKey);
    }
}