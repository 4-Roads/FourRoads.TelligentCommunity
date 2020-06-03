using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationObjectHandler
    {
        IPagedList<string> ListObjectKeys(int pageSize, int pageIndex);

        string MigrateObject(string key, IMigrationVisitor migrationVisitor, bool updateIfExistsInDestination);

        bool MigratedObjectExists(MigratedData data);

        string ObjectType { get; }
    }
}