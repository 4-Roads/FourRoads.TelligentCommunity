using FourRoads.Common.Interfaces;
using FourRoads.TelligentCommunity.InstagramFeed.Interfaces;
using FourRoads.TelligentCommunity.InstagramFeed.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Urls.Routing;

namespace FourRoads.TelligentCommunity.InstagramFeed.Logic
{
    public class MediaSearch : IMediaSearch
    {
        private readonly ICache _cache;
        private readonly IUsers _usersService;

        private string InstagramGraphApiBaseUrl = "https://graph.facebook.com/v7.0";
        private static readonly string _pageName = "instagram-feed-setup";
        private static readonly string _defaultPageLayout = "<contentFragmentPage pageName=\"instagram-feed-setup\" isCustom=\"false\" layout=\"Content\">\r\n<regions>\r\n<region regionName=\"Content\">\r\n<contentFragments>\r\n<contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::e88147a7f5fb4cb1b3f1cbda84355a26\" showHeader=\"False\" cssClassAddition=\"no-wrapper responsive-1\" isLocked=\"False\"  configuration=\"\"/><contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::d49a2ec4e16e49dd956c242b4a66213f\" showHeader=\"False\" cssClassAddition=\"no-wrapper with-spacing responsive-1\" isLocked=\"False\" configuration=\"\" />\r\n<contentFragment type=\"Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment, Telligent.Evolution.Platform::a4f3ddf41bbe46fc9e502bd2c7f7a599\" showHeader=\"False\" cssClassAddition=\"no-wrapper with-spacing responsive-1\" isLocked=\"False\" configuration=\"\" /></contentFragments>\r\n</region>\r\n</regions>\r\n<contentFragmentTabs />\r\n</contentFragmentPage>";

        public MediaSearch(IUsers usersService, ICache cache)
        {
            _usersService = usersService;
            _cache = cache;
        }

        public void Initialize()
        {

        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_pageName, _pageName, new SiteRootRouteConstraint(), null, _pageName, new PageDefinitionOptions
            {
                DefaultPageXml = _defaultPageLayout,
                Validate = (context, accessController) =>
                {
                    if (_usersService.AccessingUser != null)
                    {
                        if (_usersService.AnonymousUserName == _usersService.AccessingUser.Username)
                        {
                            accessController.AccessDenied("This page is not available to you", false);
                        }
                    }
                }
            });
        }

        public List<Media> GetMediaByHashtag(HashtagSearchRequest request)
        {
            if (request.HashtagSearchValid())
            {
                var account = GetInstagramBusinessAccountFromCache(request.PageId, request.AccessToken);

                if (!string.IsNullOrWhiteSpace(account))
                {
                    var hashTagNodeId = GetHashTagNodeFromCache(account, request.AccessToken, request.Query);

                    if (!string.IsNullOrWhiteSpace(hashTagNodeId))
                    {
                        var edge = request.MostRecent ? "recent_media" : "top_media";

                        var req = $"/{hashTagNodeId}/{edge}?user_id={account}&access_token={request.AccessToken}&fields=caption,comments_count,id,like_count,media_type,media_url,timestamp,permalink";

                        var response = GetByMediaType(req, "IMAGE", request.Limit, request.MostRecent);

                        return response;
                    }
                }
            }

            return null;
        }

        public List<Media> GetUserMedia(BasePageSearchRequest pageRequest)
        {
            if (pageRequest.Valid())
            {
                var account = GetInstagramBusinessAccountFromCache(pageRequest.PageId, pageRequest.AccessToken);

                if (!string.IsNullOrWhiteSpace(account))
                {
                    var req = $"/{account}/media?user_id={account}&access_token={pageRequest.AccessToken}&fields=caption,comments_count,id,like_count,media_type,media_url,shortcode,username,timestamp,permalink";

                    var response = GetByMediaType(req, "IMAGE", pageRequest.Limit, true);
                    
                    return response;
                }
            }

            return null;
        }

        private List<Media> GetByMediaType(string endpoint, string mediaType, int? limit, bool sortByDate)
        {
            var response = SendGetRequest<MediaResponse<Media>>(endpoint);

            if (response != null)
            {
                var filteredResponse = response.Data.Where(d => d.MediaType.Equals(mediaType, StringComparison.InvariantCultureIgnoreCase));

                if (limit.HasValue)
                {
                    filteredResponse = filteredResponse.Take(limit.Value);
                }

                if (sortByDate)
                {
                    return filteredResponse.OrderByDescending(d => d.Date).ToList();
                }

                return filteredResponse.ToList();
            }

            return null;
        }

        private string GetHashTagNodeFromCache(string businessAccountId, string accessToken, string query)
        {
            string cacheKey = GetCacheKey($"{businessAccountId}-QUERY", query);

            var cachedId = _cache.Get<string>(cacheKey);

            if (!string.IsNullOrWhiteSpace(cachedId))
            {
                return cachedId;
            }

            var hashtagId = GetHashTagNode(businessAccountId, accessToken, query);

            if (!string.IsNullOrWhiteSpace(hashtagId))
            {
                _cache.Insert(GetCacheKey($"{businessAccountId}-QUERY", query), hashtagId);
            }

            return hashtagId;
        }

        private string GetHashTagNode(string businessAccountId, string accessToken, string query)
        {
            var request = $"/ig_hashtag_search?user_id={businessAccountId}&access_token={accessToken}&q={HttpUtility.UrlEncode(query)}";
            var response = SendGetRequest<MediaResponse<BasePageSearchResponse>>(request);

            return response?.Data?.FirstOrDefault()?.Id;
        }

        private string GetInstagramBusinessAccountFromCache(string pageId, string accessToken)
        {
            string cacheKey = GetCacheKey("PAGE", pageId);

            var cachedId = _cache.Get<string>(cacheKey);

            if (!string.IsNullOrWhiteSpace(cachedId))
            {
                return cachedId;
            }
            
            var accountId = GetInstagramBusinessAccount(pageId, accessToken);

            if (!string.IsNullOrWhiteSpace(accountId))
            {
                _cache.Insert(GetCacheKey("PAGE", pageId), accountId);
            }

            return accountId;
        }

        private string GetInstagramBusinessAccount(string pageId, string accessToken)
        {
            var request = $"/{pageId}/?fields=instagram_business_account&access_token={accessToken}";
            var response = SendGetRequest<InstagramBusinessAccountResponse>(request);
            
            return response?.Account.Id;
        }
        
        private string GetCacheKey(string key, string value)
        {
            return $"IG:{key}:{value}";
        }

        private T SendGetRequest<T>(string endpoint)
        {
            try
            {
                using (var req = new HttpClient())
                {
                    var task = req.GetAsync(InstagramGraphApiBaseUrl + endpoint);

                    task.Wait();

                    if (task.Result.Content != null)
                    {
                        var resultTask = task.Result.Content.ReadAsStringAsync();

                        resultTask.Wait();

                        string content = resultTask.Result;

                        if (!task.Result.IsSuccessStatusCode)
                        {
                            Apis.Get<IEventLog>().Write($"Instagram Graph API Issue: http status code {task.Result.StatusCode} - {content}", new EventLogEntryWriteOptions());

                            if (task.Result.StatusCode == HttpStatusCode.NotFound)
                            {
                                return default(T);
                            }
                            throw new Exception($"Instagram Graph API HttpClient Failed : {task.Result.StatusCode}", new Exception(content));
                        }

                        return JsonConvert.DeserializeObject<T>(content);
                    }
                }
            }
            catch(Exception ex)
            {
                Apis.Get<IEventLog>().Write($"Instagram Graph API HttpClient Failed : {ex.Message}, {ex.StackTrace}", new EventLogEntryWriteOptions());
                
                throw new Exception($"Instagram Graph API HttpClient Failed : {ex.Message}", ex);
            }

            throw new Exception("HttpClient error");
        }
    }
}
