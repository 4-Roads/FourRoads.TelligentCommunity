// //------------------------------------------------------------------------------
// // <copyright company="Four Roads LLC">
// //     Copyright (c) Four Roads LLC.  All rights reserved.
// // </copyright>
// //------------------------------------------------------------------------------

#region

using FourRoads.Common.TelligentCommunity.Plugins.Base;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

#endregion

namespace FourRoads.Common.TelligentCommunity.Plugins.Viewers
{
    public class DailyMotionMediaViewer : VideoFileViewerBase
    {
        public override string SupportedUrlPattern
        {
            get
            {
                return @"http[s]?://(?:www\.)dailymotion\.com";
            }
        }

        protected override string ViewerName
        {
            get { return "DailyMotion"; }
        }

        public override string CreateRenderedViewerMarkup(Uri url, int maxWidth, int maxHeight)
        {
            Match match = CSRegex.DailyMotionViewerIdRegex().Match(url.OriginalString);
            if (!match.Success)
            {
                return string.Empty;
            }
            int width = 420;
            int height = 0x14b;
            Globals.ScaleUpDown(ref width, ref height, maxWidth, maxHeight);
            string playerHtml = string.Format("<iframe source=\"{3}\" frameborder=\"0\" width=\"{1}\" height=\"{2}\" src=\"http://www.dailymotion.com/embed/video/{0}\" allowfullscreen></iframe>", 
                Globals.EnsureHtmlEncoded(match.Groups[1].Value), width, height, HttpUtility.HtmlAttributeEncode(url.ToString()));
            CSContext current = CSContext.Current;
            Page handler = null;
            if (current.Context != null)
            {
                handler = current.Context.Handler as Page;
            }
            if (handler != null)
            {
                string videoId = "video_" + Guid.NewGuid().ToString();
                StringBuilder embedHtml = new StringBuilder();
                embedHtml.Append("<script type=\"text/javascript\" src=\"");
                embedHtml.Append(Globals.FullPath(handler.ClientScript.GetWebResourceUrl(typeof(DailyMotionMediaViewer), "FourRoads.Common.TelligentCommunity.Plugins.insertmarkup.js")));
                embedHtml.Append("\"></script>");
                embedHtml.AppendFormat("<div id=\"{0}\"><noscript>{1}</noscript></div>", videoId, playerHtml);
                embedHtml.Append("<script type=\"text/javascript\">\n");
                embedHtml.Append("cs_setInnerHtml('");
                embedHtml.Append(videoId);
                embedHtml.Append("','");
                embedHtml.Append(JavaScript.Encode(playerHtml));
                embedHtml.Append("');");
                embedHtml.Append("\n</script>");
                return embedHtml.ToString();
            }
            return playerHtml;
        }

    }
}