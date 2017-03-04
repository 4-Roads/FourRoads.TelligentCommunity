using System;
using FourRoads.TelligentCommunity.ForumLastPost.Interfaces;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.CoreServices.WebContext.Services;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using CacheScope = Telligent.Caching.CacheScope;

namespace FourRoads.TelligentCommunity.ForumLastPost.Logic
{
    public class LastReadPost : ILastReadPostLogic
    {
        private ICacheService _cacheService;
        private ILastReadPostDataProvider _lastReadPostDataProvider;

        public LastReadPost(ICacheService cacheService , ILastReadPostDataProvider lastReadPostDataProvider )
        {
            _lastReadPostDataProvider = lastReadPostDataProvider;
            _cacheService = cacheService;
        }

        public void UpdateLastReadPost(Guid appicationId, int userId, int threadId , int forumId, int replyId,  Guid lastReadContentId , DateTime postDateTime)
        {
            //Not API safe but can't see a way to get the current context
            //
            var content = Services.Get<IWebContextService>().GetCurrentContent(Services.Get<IContextService>().GetHttpContext());

            if (content == null)
                return;

            if (PublicApi.ContentTypes.Get(content.ContentTypeId).Name.ToLower() != "forum thread")
                return;

            ForumReply post = PublicApi.ForumReplies.Get(replyId, new ForumRepliesGetOptions() { ForumId = forumId, ThreadId = threadId });

            if (post != null && post.ThreadId == threadId)
            {
                ForumThread thread = PublicApi.ForumThreads.Get(threadId, new ForumThreadsGetOptions() {ForumId = forumId});

                LastReadPostInfo lastReadPost = GetLastReadPost(appicationId, thread.ContentId, userId);

                if (lastReadPost.ContentId.GetValueOrDefault(Guid.Empty) != lastReadContentId)
                {
                    //Get this posts post date
                    if (postDateTime > lastReadPost.PostDate)
                    {
                        //Work out the reply count all posts previous to this date
                        int pageIndex = PublicApi.ForumReplies.GetPageIndex(threadId, replyId, new ForumRepliesGetPageIndexOptions() {IncludeThreadStarter = true, PageSize = 10});
                        int replyCount = pageIndex*10;

                        var replies = PublicApi.ForumReplies.List(threadId, new ForumRepliesListOptions() {PageIndex = pageIndex, PageSize = 10, IncludeThreadStarter = true});

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

        private string CreateCacheKey(Guid appicationId, Guid contentId, int userId)
        {
            return string.Format("LastPost:A({0}):C({1}):U({2})", appicationId, contentId, userId);
        }
    }
}
