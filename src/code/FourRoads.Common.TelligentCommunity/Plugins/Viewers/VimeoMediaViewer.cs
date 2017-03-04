
// //------------------------------------------------------------------------------
// // <copyright company="Four Roads LLC">
// //     Copyright (c) Four Roads LLC.  All rights reserved.
// // </copyright>
// //------------------------------------------------------------------------------

#region

using FourRoads.Common.TelligentCommunity.Plugins.Base;
using System;
using System.Text;
using System.Web;
using Telligent.Evolution.Components;
using System.Xml;

#endregion

namespace FourRoads.Common.TelligentCommunity.Plugins.Viewers
{
    public class VimeoMediaViewer : VideoFileViewerBase
    {
        public override string SupportedUrlPattern
        {
            get
            {
                return @"http[s]?://(www|player)?\.?vimeo\.com";
            }
        }


        protected override string ViewerName
        {
            get { return "Vimeo"; }
        }

        public override string GetPreviewImageUrl(Uri url)
        {
            String retval = "~/utility/images/video-preview.gif";

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("http://vimeo.com/api/oembed.xml?url=" + HttpUtility.UrlEncode(url.ToString()));
            XmlNode xmlNode = xmlDocument.SelectSingleNode("/oembed/thumbnail_url/text()");
            if(xmlNode != null && !String.IsNullOrEmpty(xmlNode.Value))
            {
                retval = xmlNode.Value;
            }
            return retval;
        }

        public override string CreateRenderedViewerMarkup(Uri url, int maxWidth, int maxHeight)
        {
            StringBuilder retval = new StringBuilder();

            int width = 400;
            int height = 225;

            String videoId = "";
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("http://vimeo.com/api/oembed.xml?url=" + HttpUtility.UrlEncode(url.ToString()));
            XmlNode xmlNode = xmlDocument.SelectSingleNode("/oembed/video_id/text()");
            if (xmlNode != null && !String.IsNullOrEmpty(xmlNode.Value))
            {
                videoId = xmlNode.Value;
            }

            if (!String.IsNullOrEmpty(videoId))
            {
                Globals.ScaleUpDown(ref width, ref height, maxWidth, maxHeight);
                retval.Append("<iframe src=\"");
                retval.Append("https://player.vimeo.com/video/").Append(videoId).Append("\" width=\"").Append(width).Append("\" height=\"").Append(height);
                retval.Append("\" frameborder=\"0\" webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>");
            }

            return retval.ToString();
        }
    }
}