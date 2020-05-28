using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MigratorFramework
{
    public class Migrator : IMigrationVisitor , IEvolutionJob
    {
        private readonly IMigrationRepository _repository;
        private IMigrationFactory _factory;
        private MigrationContext _lastKnownContext;
        private CancellationToken _cancel;

        public Migrator()
        {
            _repository = new MigrationRepository();
        }

        private async void Start()
        {
            //Disable plugins and store in database
            using (new PluginDisabler())
            {
                _repository.SetState(MigrationState.Running);

                var objectTypes = _factory.GetOrderObjectHandlers();

                //Enumerate over the entire tree to ensure we know how much work there is to do
                int totalProcessing = 0;
                foreach (var objectType in objectTypes)
                {
                    var handler = _factory.GetHandler(objectType);

                    totalProcessing += (await handler.ListObjectKeys(1, 0)).Total;
                }

                _repository.SetTotalRecords(totalProcessing);

                //Handle Deletions First
                EnumerateAll(_repository.List, async
                    k =>
                {
                    try
                    {
                        await _factory.GetHandler(k.ObjectType).MigratedObjectExists(k);
                    }
                    catch (Exception ex)
                    {
                        _repository.FailedItem(k.ObjectType, k.SourceKey, ex.ToString());
                    }
                });

                foreach (var objectType in objectTypes)
                {
                    var handler = _factory.GetHandler(objectType);

                    double processingTimeTotal = 0;
                    int counter = 0;

                    EnumerateAll(
                        handler.ListObjectKeys, async
                        k =>
                        {
                            long start = DateTime.Now.Ticks;
                            counter++;

                            try
                            {
                                var result = await handler.MigrateObject(k, this);

                                if (result != null)
                                {
                                    _lastKnownContext = await _repository.CreateUpdate(
                                        new MigratedData()
                                        {
                                            ObjectType = objectType,
                                            SourceKey = k,
                                            ResultKey = result
                                        }, 
                                        processingTimeTotal / counter);
                                }
                                else
                                {
                                    _repository.FailedItem(objectType,k, "MigrateObject returned null");
                                }
                            }
                            catch (Exception ex)
                            {
                                _repository.FailedItem(objectType,k, ex.ToString());
                            }

                            long end = DateTime.Now.Ticks;

                            processingTimeTotal += end - start;
                        });
                }

                _repository.SetState(MigrationState.Finished);
            }
        }

        private async void EnumerateAll<T>(Func<int, int, Task<IPagedList<T>>> list, Action<T> func)
        {
            int pageIndex = 0;
            int pageSize = 500;
            var pagedItems = await list( pageSize, pageIndex);

            if (IsCanceled())
                return;

            while (pagedItems != null)
            {
                foreach (var item in pagedItems)
                {
                    func(item);
                }

                if (HasMoreItems(pageIndex , pageSize , pagedItems.Count() , pagedItems.Total))
                {
                    pageIndex += 1;
                    pagedItems = await list(pageSize, pageIndex);
                }
                else
                {
                    pagedItems = null;
                }

                if (IsCanceled())
                    return;
            }
        }

        private bool IsCanceled()
        {
            return _cancel.IsCancellationRequested || _lastKnownContext?.State == MigrationState.Cancelling;
        }

        private static bool HasMoreItems(int pageSize , int pageIndex , int currentNumber ,int total)
        {
            return pageIndex * pageSize + currentNumber < total;
        }

        public void AddUrlRedirect(string source, string destination)
        {
            _repository.CreateUrlRedirect(source , destination);
        }

        public MigratedData GetMigratedData(string objectType, string sourceKey)
        {
            return Task.Run(()=> _repository.GetMigratedData(objectType, sourceKey)).Result;
        }

        public void Execute(JobData jobData)
        {
            var migrator = PluginManager.GetSingleton<IMigratorProvider>();

            _factory = migrator.GetFactory();
            _cancel =jobData.CancellationToken;

            Task.Run(()=> Start(), _cancel);
        }
    }
}