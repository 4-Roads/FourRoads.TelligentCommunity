// //------------------------------------------------------------------------------
// // <copyright company="Four Roads LLC">
// //     Copyright (c) Four Roads LLC.  All rights reserved.
// // </copyright>
// //------------------------------------------------------------------------------

#region

using FourRoads.Common.TelligentCommunity.Plugins.Base;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using Telligent.Caching;
using Telligent.Common;
using Telligent.Evolution.Components;

#endregion

namespace FourRoads.Common.TelligentCommunity.Plugins.Viewers
{
    public class BrightcoveMediaViewer : VideoFileViewerBase
  {

    public override string SupportedUrlPattern
    {
      get { return @"(http[s]?://bcove\.me|link\.brightcove\.com)"; }
    }

    protected override string ViewerName
    {
        get { return "Brightcove"; }
    }


    /// <summary>
    ///  http://link.brightcove.com/services/player/bcpid2667922512001?bckey=AQ~~,AAAACofXClE~,cNM8jhH8p6CVg_4mtWfm7SAMyoXAfMIx&bctid=86967644001
    /// </summary>
    /// <param name="?"></param>
    /// <returns></returns>
    private static Regex BcRegex =
      new Regex("^http(?<https>[s]?)://link\\.brightcove\\.com/services/player/bcpid(?<playerId>\\d+)\\?bckey=(?<playerKey>[^&].+)&bctid=(?<videoId>\\d+)$",
        RegexOptions.Compiled);


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

        bool isHttps = !string.IsNullOrEmpty(match.Groups["https"].Value);
        string playerId = match.Groups["playerId"].Value;
        string playerKey = match.Groups["playerKey"].Value;
        string videoId = match.Groups["videoId"].Value;

        Globals.ScaleUpDown(ref width, ref height, maxWidth, maxHeight);

        string playerHtml = string.Format(PlayerHtmlTemplateBody, playerId, videoId, playerKey, width, height, isHttps?PLayerHtmlHttpsModifier:string.Empty);

        CSContext context = CSContext.Current;
        Page page = null;
        if (context.Context != null)
          page = context.Context.Handler as Page;

        if (page != null)
        {
          string id = "video_" + Guid.NewGuid();

          var wrapper = new StringBuilder();

          wrapper.Append("<script type=\"text/javascript\" src=\"");
          wrapper.Append(
              Globals.FullPath(page.ClientScript.GetWebResourceUrl(typeof(BrightcoveMediaViewer),
                  "FourRoads.Common.TelligentCommunity.Plugins.insertmarkup.js")));
          wrapper.Append("\"></script>");

          wrapper.AppendFormat("<div id=\"{0}\"><noscript>{1}</noscript></div>", id, playerHtml);
          wrapper.Append(string.Format(PlayerHtmlTemplateHead, isHttps ? "s":string.Empty));
          wrapper.Append("<script type=\"text/javascript\">\n");
          wrapper.Append("cs_setInnerHtml('");
          wrapper.Append(id);
          wrapper.Append("','");
          wrapper.Append(JavaScript.Encode(playerHtml));
          wrapper.Append("');");
          wrapper.Append("\n</script>");
          wrapper.Append(PlayerHtmlTemplateTail);

          return wrapper.ToString();
        }
        return playerHtml;
      }
      return string.Empty;
    }

    private const string PlayerHtmlTemplateHead = @"<script language=""JavaScript"" type=""text/javascript"" src=""http{0}://{0}admin.brightcove.com/js/BrightcoveExperiences.js""></script>";
    private const string PlayerHtmlTemplateTail = @"<script type=""text/javascript"">brightcove.createExperiences();</script>";
    private const string PlayerHtmlTemplateBody = @"<object id=""myExperience"" class=""BrightcoveExperience"">
  <param name=""bgcolor"" value=""#FFFFFF"" />
  <param name=""width"" value=""{3}"" />
  <param name=""height"" value=""{4}"" />
  <param name=""playerID"" value=""{0}"" />
  <param name=""videoID"" value=""{1}"">
  <param name=""playerKey"" value=""{2}"" />
  <param name=""isVid"" value=""true"" />
  <param name=""isUI"" value=""true"" />
  <param name=""dynamicStreaming"" value=""true"" />
  {5}
</object>";
    private const string PLayerHtmlHttpsModifier = @"<param name=""secureConnections"" value=""true"" />
<param name=""secureHTMLConnections"" value=""true"" />";

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
        var cacheService = Services.Get<ICacheService>();
        object cachedObj = cacheService.Get(cacheKey, CacheScope.All);
        if (cachedObj !=null)
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