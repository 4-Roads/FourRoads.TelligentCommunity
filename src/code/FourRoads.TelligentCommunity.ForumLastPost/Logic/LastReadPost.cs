using System;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.ForumLastPost.Interfaces;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.CoreServices.WebContext.Services;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using CacheScope = Telligent.Caching.CacheScope;

namespace FourRoads.TelligentCommunity.ForumLastPost.Logic
{
    public class LastReadPost : ILastReadPostLogic
    {
        private ICacheService _cacheService;
        private ILastReadPostDataProvider _lastReadPostDataProvider;
        private IUsers _users;
        private IForumReplies _forumReplies;
        private IForumThreads _forumThreads;

        public LastReadPost(ICacheService cacheService , ILastReadPostDataProvider lastReadPostDataProvider, IUsers users , IForumReplies forumReplies, IForumThreads forumThreads)
        {
            _lastReadPostDataProvider = lastReadPostDataProvider;
            _cacheService = cacheService;
            _users = users;
            _forumReplies = forumReplies;
            _forumThreads = forumThreads;
        }

        public void UpdateLastReadPost(Guid appicationId, int userId, int threadId, int forumId, int replyId, Guid lastReadContentId, DateTime postDateTime)
        {
            ForumThread thread = _forumThreads.Get(threadId, new ForumThreadsGetOptions() {ForumId = forumId});

            if (thread != null)
            {
                LastReadPostInfo lastReadPost = GetLastReadPost(appicationId, thread.ContentId, userId);

                if (lastReadPost.ContentId.GetValueOrDefault(Guid.Empty) != lastReadContentId)
                {
                    //Get this posts post date
                    if (postDateTime > lastReadPost.PostDate)
                    {
                        //Work out the reply count all posts previous to this date
                        int pageIndex = _forumReplies.GetPageIndex(threadId, replyId, new ForumRepliesGetPageIndexOptions() {IncludeThreadStarter = true, PageSize = 10});
                        int replyCount = pageIndex * 10;

                        var replies = _forumReplies.List(threadId, new ForumRepliesListOptions() {PageIndex = pageIndex, PageSize = 10, IncludeThreadStarter = true});

                        foreach (ForumReply forumReply in replies)
                        {
                            if (forumReply.Id != replyId)
                            {
                                replyCount++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        _lastReadPostDataProvider.UpdateLastReadPost(appicationId, thread.ContentId, userId, lastReadContentId, replyCount, postDateTime);

                        lastReadPost.PostDate = postDateTime;
                        lastReadPost.ContentId = lastReadContentId;
                        lastReadPost.ReplyCount = replyCount;

                        string key = CreateCacheKey(appicationId, thread.ContentId, userId);

                        _cacheService.Remove(key, CacheScope.Process);
                        _cacheService.Put(key, lastReadPost, CacheScope.Process);
                    }
                }
            }
        }

        public LastReadPostInfo GetLastReadPost(Guid appicationId, Guid contentId, int userId)
        {
            string key = CreateCacheKey(appicationId, contentId, userId);

            LastReadPostInfo? lastReadPost = _cacheService.Get(key, CacheScope.Process) as LastReadPostInfo?;

            if (lastReadPost == null || lastReadPost.Value.ContentId.GetValueOrDefault(Guid.Empty) == Guid.Empty)
            {
                lastReadPost = _lastReadPostDataProvider.GetLastReadPost(appicationId, contentId, userId);

                _cacheService.Put(key, lastReadPost, CacheScope.Process);
            }

            return lastReadPost.Value;
        }

        public void SetLastReadPost(Guid appicationId, Guid contentId)
        {
            var user = _users.AccessingUser;

            if (!user.IsSystemAccount.GetValueOrDefault(false) && user.Id.HasValue)
            {
                var forumReply = _forumReplies.Get(contentId);

                forumReply.ThrowErrors();

                UpdateLastReadPost(forumReply.Application.ApplicationId, user.Id.Value, forumReply.ThreadId.GetValueOrDefault(0),
                    forumReply.ForumId.GetValueOrDefault(0), forumReply.Id.GetValueOrDefault(0), forumReply.ContentId, forumReply.Date.GetValueOrDefault(DateTime.MinValue));
            }
        }

        private string CreateCacheKey(Guid appicationId, Guid contentId, int userId)
        {
            return string.Format("LastPost:A({0}):C({1}):U({2})", appicationId, contentId, userId);
        }
    }
}
