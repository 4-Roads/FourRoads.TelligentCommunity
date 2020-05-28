using System;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationRepository
    {
        Task<IPagedList<MigratedData>> List(int pageSize, int pageIndex);
        Task<MigrationContext> CreateUpdate(MigratedData migratedData, double processingTimeTotal);
        void Install(Version lastInstalledVersion);
        void SetTotalRecords(int totalProcessing);
        void CreateUrlRedirect(string source, string destination);
        Task<MigratedData> GetMigratedData(string objectType, string sourceKey);
        void SetState(MigrationState state);
        void CreateNewContext();
        void ResetJob();
        void FailedItem(string objectType, string key, string error);
        Task<MigrationContext> GetMigrationContext();
    }
}