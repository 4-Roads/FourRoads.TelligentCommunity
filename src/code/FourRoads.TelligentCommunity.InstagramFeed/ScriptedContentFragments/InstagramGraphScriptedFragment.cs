using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.InstagramFeed.Interfaces;
using FourRoads.TelligentCommunity.InstagramFeed.Models;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.InstagramFeed.ScriptedContentFragments
{
    public class InstagramGraphScriptedFragment
    {
        /// <summary>
        /// Search by hashtag
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="accessToken"></param>
        /// <param name="query"></param>
        /// <param name="recentFirst"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [Documentation("Search by single hashtag")]
        public List<Media> GetMediaByHashtag(
            [Documentation("Facebook Page Id", Name = "pageId", Description = "Instagram Account connected FB Page Id", Type = typeof(string))] string pageId,
            [Documentation("Access Token", Name = "accessToken", Description = "Permanent Page Access Token", Type = typeof(string))] string accessToken,
            [Documentation("Tag to query", Name = "query", Description = "Single string tag without the hash", Type = typeof(string))] string query,
            [Documentation("Display Most Recent", Name = "recentFirst", Description = "Result will be sorted by date if true, otherwise, will be unsorted", Type = typeof(bool))] bool recentFirst,
            [Documentation("Number of nedia to display", Name = "limit", Description = "Limit can be up to 50 per page", Type = typeof(int))] int limit)
        {
            var request = new HashtagSearchRequest()
            {
                PageId = pageId, 
                AccessToken = accessToken,
                Query = query,
                MostRecent = recentFirst
            };

            if (limit > 0)
            {
                request.Limit = limit;
            }

            return Injector.Get<IMediaSearch>().GetMediaByHashtag(request);
        }

        /// <summary>
        /// Returns instagram user media
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="accessToken"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [Documentation("Returns instagram user media")]
        public List<Media> GetMedia(
            [Documentation("Facebook Page Id", Name = "pageId", Description = "Instagram Account connected FB Page Id", Type = typeof(string))] string pageId,
            [Documentation("Access Token", Name = "accessToken", Description = "Permanent Page Access Token", Type = typeof(string))] string accessToken,
            [Documentation("Number of nedia to display", Name = "limit", Description = "Limit can be up to 50 per page", Type = typeof(int))] int limit)
        {
            var request = new BasePageSearchRequest()
            {
                PageId = pageId,
                AccessToken = accessToken,
            };

            if (limit > 0)
            {
                request.Limit = limit;
            }

            return Injector.Get<IMediaSearch>().GetUserMedia(request);
        }

        [Documentation("Returns plugin configured app id")]
        public string GetAppId()
        {
            var plugin = PluginManager.GetSingleton<IInstagramFeedPlugin>();

            return plugin.AppId;
        }
    }
}
