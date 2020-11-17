using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IPermissions = Telligent.Evolution.Extensibility.Api.Version2.IPermissions;

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
        private static Dictionary<string,string> _locks = new Dictionary<string, string>();
        private static object _lock = new object();

        public Migrator()
        {
            _repository = new MigrationRepository();
        }

        private static int _processingCounter;

        public void SafeRunAs(string userName, Action action)
        {
            if (!_locks.ContainsKey(userName))
                _locks.Add(userName, userName);

            lock (_locks[userName])
            {
                Apis.Get<IUsers>().RunAsUser(userName, action);
            }
        }

        private void Start(bool updateIfExistsInDestination, bool checkForDeletions)
        {
            using (var formerMember = new FormerMember(_repository))
            {
                try
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
                                    if (_userHandlers.Contains(k.ObjectType))
                                    {
                                        if (_factory.GetHandler(k.ObjectType).MigratedObjectExists(k))
                                        {
                                            _existingData.Add(k.ObjectType + k.SourceKey, k);
                                        }
                                    }
                                },
                                (e,k) =>
                                {
                                    _repository.FailedItem(k.ObjectType, k.SourceKey, e.ToString());
                                }
                                );
                        }

                        _repository.CreateLogEntry("Starting Migration", EventLogEntryType.Information);
                        _processingCounter = 0;

                        foreach (var objectType in objectTypes)
                        {
                            if (IsCanceled())
                            {
                                break;
                            }

                            if (_userHandlers.Contains(objectType))
                            {
                                var handler = _factory.GetHandler(objectType);

                                List<string> retryList = new List<string>();

                                void ProcessItem(string k)
                                {
                                    long start = DateTime.Now.Ticks;

                                    var result = handler.MigrateObject(k, this, updateIfExistsInDestination);
                                   
                                    if (result != null)
                                    {
                                        double proccessingTime = 0;
                                        if (_processingCounter % 100 == 0)
                                        {
                                            long end = DateTime.Now.Ticks;

                                            proccessingTime += end - start;
                                        }

                                        Interlocked.Increment(ref _processingCounter);

                                        _lastKnownContext = _repository.CreateUpdate(
                                            new MigratedData()
                                            {
                                                ObjectType = objectType,
                                                SourceKey = k,
                                                ResultKey = result
                                            },
                                            _processingCounter,
                                            proccessingTime);
                                    }
                                    else
                                    {
                                        _repository.FailedItem(objectType, k, "Item skipped");
                                    }

                                }

                                EnumerateAll(
                                    handler.ListObjectKeys,
                                    v =>
                                    {
                                        ProcessItem(v);
                                    },
                                    (e,k) =>
                                    {
                                        _repository.FailedItem(objectType, k, e.ToString());
                                        retryList.Add(k);
                                    }
                                );

                                foreach (var k in retryList)
                                {
                                    try
                                    {
                                        ProcessItem(k);
                                    }
                                    catch (Exception ex)
                                    {
                                        _repository.FailedItem(objectType, k, "RETRY:" + ex.ToString());
                                    }
                                }
                            }
                        }

                        _factory.SignalMigrationFinshing();

                        _repository.CreateLogEntry("Migration Finished", EventLogEntryType.Information);
                    }
                }
                finally
                {
                    _repository.SetState(MigrationState.Finished);
                }
            }
        }

        private void EnumerateAll<T>(Func<int, int, Interfaces.IPagedList<T>> list, Action<T> func, Action<Exception,T> exceptionHandler )
        {
            int pageIndex = 0;
            int pageSize = 5000;
            var pagedItems = list( pageSize, pageIndex);

            if (IsCanceled())
                return;

            while (pagedItems != null)
            {
                var items = pagedItems.ToArray();

                var rangePartitioner = Partitioner.Create(0, items.Length , 500);

                Parallel.ForEach(rangePartitioner , (range, loopState) =>
                {
                    var context = Telligent.Evolution.Components.CSContext.Create() ;
                    context.User = Telligent.Evolution.Users.GetUser(Apis.Get<IUsers>().ServiceUserName);

                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        try
                        {

                            func(items[i]);
                        }
                        catch(Exception ex)
                        {
                            exceptionHandler(ex , items[i]);
                        }

                        if (IsCanceled())
                            return;
                    }
                });

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
            lock (_lock)
            {
                string key = objectType + sourceKey;

                if (_existingData.ContainsKey(key))
                    return _existingData[key];

                //Try and get it from the database
                var result = _repository.GetMigratedData(objectType, sourceKey);

                if (result != null)
                    _existingData.Add(key, result);

                return result;
            }
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
            if (string.Compare(@group.GroupType, "joinless", StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (!author.IsSystemAccount.GetValueOrDefault(false))
                {
                    lock (_lock)
                    {
                        var groupUser = Apis.Get<IEffectiveGroupMembers>().List(
                            @group.Id.Value,
                            new EffectiveGroupMembersListOptions()
                            {
                                UserNameFilter = author.Username,
                                PageIndex = 0,
                                PageSize = 1
                            }).FirstOrDefault();

                        if (groupUser == null)
                        {
                            //Bug in telligent if the user is an owner then IEffectiveGroupMembers List does not work
                            groupUser = Apis.Get<IGroupUserMembers>().Get(@group.Id.Value, new GroupUserMembersGetOptions() { Username = author.Username });

                            if (groupUser == null)
                            {
                                groupUser = Apis.Get<IGroupUserMembers>().Create(@group.Id.Value, author.Id.Value, new GroupUserMembersCreateOptions() { GroupMembershipType = "Member" });
                            }
                        }

                        groupUser.ThrowErrors();
                    }
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
        }

        public void EnsureBlogAuthor(Blog blog, User user)
        {
            lock (_lock)
            {
                blog = Apis.Get<IBlogs>().Get(blog.ApplicationId);

                if (blog.Authors.All(a => a.Username != user.Username))
                {
                    List<string> authors = new List<string>(blog.Authors.Select(a => a.Username));

                    authors.Add(user.Username);

                    Apis.Get<IBlogs>().Update(
                        blog.Id.Value,
                        new BlogsUpdateOptions()
                        {
                            Authors = string.Join(",", authors.Distinct())
                        }).ThrowErrors();
                }
            }
        }

        public void EnsureUploadPermissions(Gallery gallery)
        {
            int roleId = 2; // Registered Users
            IEnumerable<Guid> permissionIds = new List<Guid>() {

                Guid.Parse("3115a602-82af-41ab-ae71-b56c568b6d7b"), // Create media
                Guid.Parse("8ddbfc6f-083e-4642-8c9a-72c022a69ceb"), // Upload files
            };

            foreach (Guid permissionId in permissionIds)
            {
                Apis.Get<IPermissions>().Set(true, roleId, permissionId,
                    new Telligent.Evolution.Extensibility.Api.Version2.PermissionSetOptions()
                    {
                        ApplicationId = gallery.ApplicationId,
                        GroupId = gallery.Group.Id
                    });
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