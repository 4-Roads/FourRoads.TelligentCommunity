using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationObjectHandler
    {
        Task<IPagedList<string>> ListObjectKeys(int pageSize, int pageIndex);

        Task<string> MigrateObject(string key, IMigrationVisitor migrationVisitor);

        Task<bool> MigratedObjectExists(MigratedData data);

        string ObjectType { get; }
    }
}