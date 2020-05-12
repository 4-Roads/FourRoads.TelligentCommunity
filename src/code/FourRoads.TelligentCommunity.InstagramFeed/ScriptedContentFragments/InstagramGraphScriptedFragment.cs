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
        [Documentation("Search by hashtag")]
        public List<Media> GetMediaByHashtag(string pageId, string accessToken, string query, bool recentFirst, int limit)
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
        public List<Media> GetMedia(string pageId, string accessToken, int limit)
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
