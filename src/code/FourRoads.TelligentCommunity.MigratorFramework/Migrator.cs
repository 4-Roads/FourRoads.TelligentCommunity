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
using Telligent.Evolution.Extensibility.Ideation.Api;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IPermissions = Telligent.Evolution.Extensibility.Api.Version2.IPermissions;
using PermissionSetOptions = Telligent.Evolution.Extensibility.Api.Version2.PermissionSetOptions;

namespace FourRoads.TelligentCommunity.MigratorFramework
{
    public class Migrator : IMigrationVisitor, IEvolutionJob
    {
        private readonly IMigrationRepository _repository;
        private IMigrationFactory _factory;
        private MigrationContext _lastKnownContext;
        private CancellationToken _cancel;
        private ConcurrentDictionary<string, MigratedData> _existingData = new ConcurrentDictionary<string, MigratedData>();
        private string[] _userHandlers;
        public const string IGNORE_RESULT = "ignore";
        private ConcurrentDictionary<int, object> _groupLock = new ConcurrentDictionary<int, object>();
        private ConcurrentDictionary<int, object> _blogLock = new ConcurrentDictionary<int, object>();
        private ConcurrentDictionary<int, object> _mediaLock = new ConcurrentDictionary<int, object>();
        private ConcurrentDictionary<Guid, object> _challengeLock = new ConcurrentDictionary<Guid, object>();
        private List<Tuple<string, string>> _retryList;

        public Migrator()
        {
            _repository = new MigrationRepository();
        }

        private static int _processingCounter;

        private void Start(bool updateIfExistsInDestination, bool checkForDeletions, int cutoffDays, int maxThreads)
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
                        long start = 0;

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
                                            _existingData.TryAdd(k.ObjectType + k.SourceKey, k);
                                        }
                                    }
                                },
                                (e, k) =>
                                {
                                    _repository.FailedItem(k.ObjectType, k.SourceKey, e.ToString());
                                },
                                maxThreads
                                );
                        }

                        _repository.CreateLogEntry("Starting Migration", EventLogEntryType.Information);
                        _processingCounter = 0;
                        _retryList = new List<Tuple<string, string>>();
                        foreach (var objectType in objectTypes)
                        {
                            if (IsCanceled())
                            {
                                break;
                            }

                            _repository.SetCurrentObjectType(objectType);

                            if (_userHandlers.Contains(objectType))
                            {
                                void ProcessItem(IMigrationObjectHandler migrationHandler, string k, string objType)
                                {
                                    start = DateTime.Now.Ticks;

                                    var result = migrationHandler.MigrateObject(k, this, updateIfExistsInDestination , cutoffDays);

                                    if (result != IGNORE_RESULT)
                                    {
                                        // Account for messages that have been migrated to alternative applications
                                        string migratedKey = result;
                                        string migratedType = objType;

                                        if (result.IndexOf(":") >= 0)
                                        {
                                            var parts = result.Split(':');

                                            if(parts.Length == 2)
                                            {
                                                migratedKey = parts[1];
                                                migratedType = parts[0];
                                            }
                                        }

                                        _repository.CreateUpdate(
                                            new MigratedData()
                                            {
                                                ObjectType = migratedType,
                                                SourceKey = k,
                                                ResultKey = migratedKey
                                            });
                                    }

                                    if (Interlocked.Increment(ref _processingCounter) % 100 == 0)
                                    {
                                        _lastKnownContext = _repository.SetProcessingMetrics(_processingCounter, DateTime.Now.Ticks - start);
                                    }
                                }

                                var handler = _factory.GetHandler(objectType);

                                handler.PreMigration(this);
                                EnumerateAll(
                                    handler.ListObjectKeys,
                                    v =>
                                    {
                                        ProcessItem(handler, v, objectType);
                                    },
                                    (e, k) =>
                                    {
                                        _repository.FailedItem(objectType, k, e.ToString());
                                    },
                                    maxThreads
                                );


                                //if the retry list is greater than 1000 then there are serious other errors
                                if (_retryList.Count < 1000)
                                {
                                    _repository.CreateLogEntry($"Retrying {_retryList.Count} records", EventLogEntryType.Information);

                                    var workingList = new List<Tuple<string, string>>(_retryList);
                                    _retryList.Clear();

                                    //do these single threaded
                                    foreach (var item in workingList)
                                    {
                                        try
                                        {
                                            ProcessItem(_factory.GetHandler(item.Item1), item.Item2, item.Item1);
                                        }
                                        catch (Exception ex)
                                        {
                                            _repository.FailedItem(item.Item1, item.Item2, ex.ToString());
                                        }

                                        if (IsCanceled())
                                        {
                                            break;
                                        }
                                    }

                                    _repository.CreateLogEntry($"Finished Retrying", EventLogEntryType.Information);
                                    foreach (var item in _retryList)
                                    {
                                        _repository.CreateLogEntry($"Second Retry of: {item.Item1}:{item.Item2} failed", EventLogEntryType.Information);
                                    }
                                }
                                else
                                {
                                    _repository.CreateLogEntry($"<span style=\"red\">{_retryList.Count} items failed! Too many to retry, aborting. Re-run migration.</span>", EventLogEntryType.Warning);
                                }

                                handler.PostMigration(this);
                            }
                        }

                        if (start > 0)
                        {
                            _lastKnownContext = _repository.SetProcessingMetrics(_processingCounter, DateTime.Now.Ticks - start);
                        }

                        _factory.SignalMigrationFinshing();
                        _repository.CreateLogEntry($"Migration finished, processed {_processingCounter} records", EventLogEntryType.Information);
                    }
                }
                finally
                {
                    _repository.SetState(MigrationState.Finished);
                }
            }
        }

        public void ScheduleRetry(string objectType, string sourceKey)
        {
            _retryList.Add(new Tuple<string, string>(objectType, sourceKey));
        }

        private void EnumerateAll<T>(Func<int, int, Interfaces.IPagedList<T>> list, Action<T> func, Action<Exception, T> exceptionHandler, int allowedThreads)
        {
            int pageIndex = 0;
            int pageSize = 5000;
            var pagedItems = list(pageSize, pageIndex);
            int totalItems = pagedItems.Total;
            var threads = new ConcurrentBag<int>();
            int maxThreads = 0, processed = 0, failed = 0;
            object key = new object();

            if (IsCanceled())
            {
                ShowSummary(processed, failed, maxThreads);

                return;
            }

            while (pagedItems != null)
            {
                var items = pagedItems.ToArray();

                if (items.Length > 0)
                {
                    var rangePartitioner = Partitioner.Create(0, items.Length, 500);

                    Parallel.ForEach(rangePartitioner, new ParallelOptions { MaxDegreeOfParallelism = allowedThreads }, (range, loopState) =>
                   {
                       var context = Telligent.Evolution.Components.CSContext.Create();
                       context.User = Telligent.Evolution.Users.GetUser(Apis.Get<IUsers>().ServiceUserName);
                       var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                       threads.Add(currentThreadId);
                       maxThreads = Math.Max(maxThreads, threads.Count);

                       for (int i = range.Item1; i < range.Item2; i++)
                       {
                           try
                           {
                               func(items[i]);

                               lock (key)
                               {
                                   processed++;
                               }
                           }
                           catch (Exception ex)
                           {
                               lock (key)
                               {
                                   failed++;
                               }
                               
                               exceptionHandler(ex, items[i]);
                           }

                           if (IsCanceled())
                           {
                               threads.TryTake(out currentThreadId);
                               ShowSummary(processed, failed, maxThreads);

                               return;
                           }
                       }
                       threads.TryTake(out currentThreadId);
                   });
                }
                
                if (pagedItems.Count() > 0 && processed < totalItems)
                {
                    if (HasMoreItems(pageIndex, pageSize, pagedItems.Count(), totalItems))
                    {
                        pageIndex += 1;
                        pagedItems = list(pageSize, pageIndex);
                    }
                }
                else
                {
                    pagedItems = null;
                }

                if (IsCanceled())
                {
                    ShowSummary(processed, failed, maxThreads);

                    return;
                }
            }

            ShowSummary(processed, failed, maxThreads);
        }

        private void ShowSummary(int processed, int failed, int threads)
        {
            _repository.CreateLogEntry($"{processed} migrated, {failed} failed, {threads} threads spawned", EventLogEntryType.Information);
        }

        private bool IsCanceled()
        {
            return _cancel.IsCancellationRequested || _lastKnownContext?.State == MigrationState.Cancelling;
        }

        private bool HasMoreItems(int pageSize, int pageIndex, int currentNumber, int total)
        {
            return pageIndex * pageSize + currentNumber < total;
        }

        public void AddUrlRedirect(string source, string destination)
        {
            _repository.CreateUrlRedirect(source, destination);
        }

        public MigratedData GetMigratedData(string objectType, string sourceKey)
        {
            string key = objectType + sourceKey;

            return _existingData.AddOrUpdate(key, k =>
            {
                return _repository.GetMigratedData(objectType, sourceKey);
            },
            (k, v) =>
            {
                if (v == null)
                {
                    return _repository.GetMigratedData(objectType, sourceKey);
                }
                return v;
            });
        }

        public void CreateLogEntry(string message, EventLogEntryType information)
        {
            _repository.CreateLogEntry(message, information);
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
                    var key = _groupLock.GetOrAdd(@group.Id.Value, new object());
                    lock (key)
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
            var key = _blogLock.GetOrAdd(blog.Id.Value, new object());
            lock (key)
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
            int roleId = Apis.Get<IRoles>().Find("Registered Users").FirstOrDefault().Id.Value; // Registered Users
            IEnumerable<Guid> permissionIds = new List<Guid>() {
                Apis.Get<IMediaPermissions>().CreatePost, // Create media
                Apis.Get<IMediaPermissions>().AttachFileLocal, // Upload files
                Apis.Get<IMediaPermissions>().OverrideValidation, // Upload files
            };
            PermissionSetOptions options = new PermissionSetOptions()
            {
                ApplicationId = gallery.ApplicationId,
                GroupId = gallery.Group.Id
            };
            var key = _mediaLock.GetOrAdd(gallery.Id.Value, new object());

            SetRolePermissions(key, roleId, permissionIds, options);
        }

        public void EnsureIdeationPermissions(Challenge challenge)
        {
            int roleId = Apis.Get<IRoles>().Find("Registered Users").FirstOrDefault().Id.Value; // Registered Users
            IEnumerable<Guid> permissionIds = new List<Guid>() {
                Apis.Get<IIdeaPermissions>().ManageIdeaStatus, // Manage idea status
            };
            PermissionSetOptions options = new PermissionSetOptions()
            {
                ApplicationId = challenge.ApplicationId,
                GroupId = challenge.Group.Id
            };
            var key = _challengeLock.GetOrAdd(challenge.Id, new object());

            SetRolePermissions(key, roleId, permissionIds, options);
        }

        private void SetRolePermissions(object key, int roleId, IEnumerable<Guid> permissionIds, PermissionSetOptions options)
        {
            lock (key)
            {
                foreach (Guid permissionId in permissionIds)
                {
                    Apis.Get<IPermissions>().Set(true, roleId, permissionId, options);
                }
            }
        }

        public void Execute(JobData jobData)
        {
            var migrator = PluginManager.GetSingleton<IMigratorProvider>();

            _factory = migrator.GetFactory();
            _cancel = jobData.CancellationToken;

            _userHandlers = (jobData.Data["objectHandlers"]).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            Start(bool.Parse((string)jobData.Data["updateIfExistsInDestination"]), bool.Parse((string)jobData.Data["checkForDeletions"]),int.Parse((string)jobData.Data["cutoffDays"]), int.Parse((string)jobData.Data["maxThreads"]));
        }
    }
}