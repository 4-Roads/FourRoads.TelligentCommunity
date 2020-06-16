using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
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
        private Dictionary<string, MigratedData> _existingData = new Dictionary<string, MigratedData>();
        private string[] _userHandlers;

        public Migrator()
        {
            _repository = new MigrationRepository();
        }

        private void Start(bool updateIfExistsInDestination, bool checkForDeletions)
        {
            using (var formerMember = new FormerMember(_repository))
            {
                //Disable plugins and store in database
                using (new PluginDisabler(_repository))
                {
                    _factory.SignalMigrationStarting();

                    _repository.SetState(MigrationState.Running);

                    var objectTypes = _factory.GetOrderObjectHandlers();

                    //Enumerate over the entire tree to ensure we know how much work there is to do
                    _repository.CreateLogEntry("Attempting to calculate the number of items", EventLogEntryType.Information);

                    int totalProcessing = 0;
                    foreach (var objectType in objectTypes)
                    {
                        if (_userHandlers.Contains(objectType))
                        {
                            var handler = _factory.GetHandler(objectType);

                            totalProcessing += (handler.ListObjectKeys(1, 0)).Total;
                        }
                    }

                    _repository.SetTotalRecords(totalProcessing);

                    if (checkForDeletions)
                    {
                        _repository.CreateLogEntry("Checking for deleted items", EventLogEntryType.Information);
                        //Handle Deletions First
                        EnumerateAll(
                            _repository.List,
                            k =>
                            {
                                try
                                {
                                    if (_userHandlers.Contains(k.ObjectType))
                                    {
                                        if (_factory.GetHandler(k.ObjectType).MigratedObjectExists(k))
                                        {
                                            _existingData.Add(k.ObjectType + k.SourceKey, k);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _repository.FailedItem(k.ObjectType, k.SourceKey, ex.ToString());
                                }
                            });
                    }

                    _repository.CreateLogEntry("Starting Migration", EventLogEntryType.Information);

                    foreach (var objectType in objectTypes)
                    {
                        if (_userHandlers.Contains(objectType))
                        {
                            var handler = _factory.GetHandler(objectType);

                            double processingTimeTotal = 0;
                            int counter = 0;

                            EnumerateAll(
                                handler.ListObjectKeys,

                                k =>
                                {
                                    long start = DateTime.Now.Ticks;
                                    counter++;

                                    try
                                    {
                                        var result = handler.MigrateObject(k, this, updateIfExistsInDestination);

                                        if (result != null)
                                        {
                                            _lastKnownContext = _repository.CreateUpdate(
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
                                            _repository.FailedItem(objectType, k, "Item skipped");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _repository.FailedItem(objectType, k, ex.ToString());
                                    }

                                    long end = DateTime.Now.Ticks;

                                    processingTimeTotal += end - start;
                                });
                        }
                    }

                    _repository.SetState(MigrationState.Finished);

                    _factory.SignalMigrationFinshing();

                    _repository.CreateLogEntry("Migration Finished", EventLogEntryType.Information);
                }

            }
        }

        private void EnumerateAll<T>(Func<int, int, Interfaces.IPagedList<T>> list, Action<T> func)
        {
            int pageIndex = 0;
            int pageSize = 500;
            var pagedItems = list( pageSize, pageIndex);

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
                    pagedItems = list(pageSize, pageIndex);
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

        private bool HasMoreItems(int pageSize , int pageIndex , int currentNumber ,int total)
        {
            return pageIndex * pageSize + currentNumber < total;
        }

        public void AddUrlRedirect(string source, string destination)
        {
            _repository.CreateUrlRedirect(source , destination);
        }

        public MigratedData GetMigratedData(string objectType, string sourceKey)
        {
            string key = objectType + sourceKey;

            if (_existingData.ContainsKey(key))
                 return _existingData[key];

            //Try and get it from the database
            var result =_repository.GetMigratedData(objectType, sourceKey);

            if (result != null)
                _existingData.Add(key, result);

            return result;
        }

        public void CreateLogEntry(string message, EventLogEntryType information)
        {
            _repository.CreateLogEntry(message , information);
        }

        public User GetUserOrFormerMember(int? userId)
        {
            User author = null;

            if (userId == null)
            {
                //User deleted so use former member
                author = Apis.Get<IUsers>().Get(new UsersGetOptions() { Username = Apis.Get<IUsers>().FormerMemberName });
            }
            else
            {
                author = Apis.Get<IUsers>().Get(new UsersGetOptions() { Id = userId.Value });
            }

            author.ThrowErrors();

            return author;
        }

        public void EnsureGroupMember(Group @group, User author)
        {
            if (!author.IsSystemAccount.GetValueOrDefault(false))
            {
                var groupUser = Apis.Get<IEffectiveGroupMembers>().List(
                    @group.Id.Value,
                    new EffectiveGroupMembersListOptions()
                    {
                        UserNameFilter = author.Username, PageIndex = 0, PageSize = 1
                    }).FirstOrDefault();

                if (groupUser == null)
                {
                    groupUser = Apis.Get<IGroupUserMembers>().Create(@group.Id.Value, author.Id.Value, new GroupUserMembersCreateOptions() {GroupMembershipType = "Member"});
                }

                groupUser.ThrowErrors();
            }
            else
            {
                //FOrmer member is an administrator during migration
                if (author.Username != Apis.Get<IUsers>().FormerMemberName)
                {
                    throw new Exception("Trying to add content to a membership based group using a system account like Former Member is not allowed");
                }
            }
        }

        public void EnsureBlogAuthor(Blog blog, User user)
        {
            if (blog.Authors.All(a => a.Username != user.Username))
            {
                List<string> authors = new List<string>(blog.Authors.Select(a => a.Username));

                authors.Add(user.Username);

                Apis.Get<IBlogs>().Update(
                    blog.Id.Value,
                    new BlogsUpdateOptions()
                    {
                        Authors = string.Join(",", authors)
                    }).ThrowErrors();
            }
        }

        public void Execute(JobData jobData)
        {
            var migrator = PluginManager.GetSingleton<IMigratorProvider>();

            _factory = migrator.GetFactory();
            _cancel =jobData.CancellationToken;

            _userHandlers= (jobData.Data["objectHandlers"]).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

            Start(bool.Parse((string) jobData.Data["updateIfExistsInDestination"]), bool.Parse((string)jobData.Data["checkForDeletions"]));
        }
    }
}