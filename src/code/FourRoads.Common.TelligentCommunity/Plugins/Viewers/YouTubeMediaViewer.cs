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
using System.Web;
using System.Web.UI;
using Telligent.Evolution.Components;
using System.Xml;
using System.Xml.Linq;

#endregion

namespace FourRoads.Common.TelligentCommunity.Plugins.Viewers
{
    public class YouTubeMediaViewer : VideoFileViewerBase
    {
        public override string SupportedUrlPattern
        {
            get { return @"http[s]?://(?:www\.)?youtu(?:be\.com|\.be)"; }
        }

        protected override string ViewerName
        {
            get { return "YouTube"; }
        }

        public override string GetPreviewImageUrl(Uri url)
        {
            String retval = "~/utility/images/youtube.gif";
            XmlDocument xmlDocument = new XmlDocument();
            StringBuilder oembed = new StringBuilder("https://www.youtube.com/oembed?url=").Append(HttpUtility.UrlEncode(url.ToString())).Append("&format=xml");
            xmlDocument.Load(oembed.ToString());
            XmlNode xmlNode = xmlDocument.SelectSingleNode("/oembed/thumbnail_url/text()");
            if (xmlNode != null)
            {
                retval = xmlNode.Value;
            }
            return retval;
        }

        public override string CreateRenderedViewerMarkup(Uri url, int maxWidth, int maxHeight)
        {
            StringBuilder retval = new StringBuilder();
            int height = 315;
            int width = 560;

            Globals.ScaleUpDown(ref width, ref height, maxWidth, maxHeight);

            String s = url.ToString();
            if (s.IndexOf("embed") != -1)
            {
                retval.Append("<iframe src=\"").Append(url).Append("\" width=\"").Append(width).Append("\" height=\"").Append(height).Append("\" frameborder=\"0\" allowfullscreen></iframe>");
            }
            else
            {
                XmlDocument xmlDocument = new XmlDocument();
                StringBuilder oembed = new StringBuilder("https://www.youtube.com/oembed?scheme=https&url=").Append(HttpUtility.UrlEncode(url.ToString())).Append("&format=xml").Append("&maxwidth=").Append(width).Append("&maxheight=").Append(height);
                xmlDocument.Load(oembed.ToString());
                XmlNode xmlNode = xmlDocument.SelectSingleNode("/oembed/html/text()");
                if (xmlNode != null)
                {
                    retval.Append(xmlNode.Value);
                }
            }
            return retval.ToString();
        }
    }
}