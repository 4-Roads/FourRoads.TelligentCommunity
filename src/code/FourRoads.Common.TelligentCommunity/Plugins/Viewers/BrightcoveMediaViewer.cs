// //------------------------------------------------------------------------------
// // <copyright company="4 Roads LTD">
// //     Copyright (c) 4 Roads LTD 2019.  All rights reserved.
// // </copyright>
// //------------------------------------------------------------------------------

#region

using FourRoads.Common.TelligentCommunity.Plugins.Base;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Telligent.Evolution.Caching.Services;
using Telligent.Evolution.Components;
using TelligentServices = Telligent.Common.Services;

#endregion

namespace FourRoads.Common.TelligentCommunity.Plugins.Viewers
{
    public class BrightcoveMediaViewer : VideoFileViewerBase
  {

    public override string SupportedUrlPattern
    {
      get { return @"(http[s]?://bcove\.me|players\.brightcove\.net)"; }
    }

    protected override string ViewerName
    {
        get { return "Brightcove"; }
    }


    private static Regex BcRegex = new Regex("^http(?<https>[s]?):\\/\\/players\\.brightcove\\.net\\/(?<accountId>\\d+)\\/(?<player>\\w*)_(?<embed>\\w*).*videoId=(?<videoId>\\d+).*$", RegexOptions.Compiled);

   //https://players.brightcove.net/689254975001/default_default/index.html?videoId=5541774265001

    public override string CreateRenderedViewerMarkup(Uri url, int maxWidth, int maxHeight)
    {
      string videoUrl = new BcWebClient().ResolveMinifiedUrl(url);

      //WORKAROUND for telligent bug
      videoUrl = videoUrl.Replace("&amp;", "&");

      Match match = BcRegex.Match(videoUrl);

        if (match.Success)
        {
            int height = 450;
            int width = 480;

            //bool isHttps = !string.IsNullOrEmpty(match.Groups["https"].Value);
            string accountId = match.Groups["accountId"].Value;
            string player = match.Groups["player"].Value;
            string videoId = match.Groups["videoId"].Value;
            string embed = match.Groups["embed"].Value;

            Globals.ScaleUpDown(ref width, ref height, maxWidth, maxHeight);

            var wrapper = new StringBuilder();

            wrapper.Append($"<video data-video-id=\"{videoId}\" data-account=\"{accountId}\" data-player=\"{player}\" data-embed=\"{embed}\"");
            wrapper.Append($" data-application-id class=\"video-js\" controls></video><script> src=\"//players.brightcove.net/{accountId}/{player}_{embed}/index.min.js\"></script>");

            return wrapper.ToString();
        }
        return videoUrl;
    }

    class BcWebClient : WebClient
    {
      public string ResolveMinifiedUrl(Uri url)
      {
        if (url.Host != "bcove.me")
        {
          //no point resolving
          return url.ToString();
        }
        string cacheKey = url.ToString();
        var cacheService = TelligentServices.Get<ICacheService>();

        object cachedObj;
        if (cacheService.TryGet(cacheKey, CacheScope.All, out cachedObj))
        {
          return cachedObj.ToString();
        }
        
        DownloadData(url);

        if (_responseUri != null)
        {
          string resolved = _responseUri.ToString();
          cacheService.Put(cacheKey, resolved, CacheScope.All);
          return resolved;
        }
        //if no luck resolving, return the original url
        return cacheKey;
      }

      Uri _responseUri;

      protected override WebResponse GetWebResponse(WebRequest request)
      {
        WebResponse response = base.GetWebResponse(request);
        if (response != null)
        {
          _responseUri = response.ResponseUri;
          return response;
        }
        return null;
      }
    }
  }
}