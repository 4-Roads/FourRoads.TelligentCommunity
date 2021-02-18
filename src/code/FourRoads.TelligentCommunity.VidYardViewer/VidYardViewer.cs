using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using Telligent.Evolution.Extensibility;

namespace FourRoads.TelligentCommunity.VidYardViewer
{
    public class VidYardViewer : VideoFileViewerBase
    {
        private Regex _urlPatternMatch;
        
        public override string SupportedUrlPattern
        {
            get
            {
                //https://play.vidyard.com/s1ioreEkbPU3C1SXdrSzqJ.jpg
                return @"http[s]?:\/\/play.vidyard.com\/(.*)$";
            }
        }

        private Regex UrlPatterMatch
        {
            get
            {
                if (_urlPatternMatch == null)
                {
                    _urlPatternMatch = new Regex(SupportedUrlPattern, RegexOptions.Compiled | RegexOptions.Multiline| RegexOptions.IgnoreCase);
                }
                
                return _urlPatternMatch;
            }
        }

        protected override string ViewerName
        {
            get { return "VidYard"; }
        }

        public override string GetPreviewImageUrl(Uri url)
        {
            return url.AbsolutePath;
        }

        public override string CreateRenderedViewerMarkup(Uri url, int maxWidth, int maxHeight)
        {
            StringBuilder retval = new StringBuilder();

            var match = UrlPatterMatch.Match(url.ToString());

            if (match.Groups.Count == 2)
            {
                var code = Path.GetFileNameWithoutExtension(match.Groups[1].Value);
                
                retval.AppendLine($@"<img
                    style=""width:100%;max-width: {maxWidth}px;max-height:{maxHeight}px; margin: auto; display: block;""
                    class=""vidyard-player-embed""
                    src=""{url}""
                    data-uuid=""{code}""
                    data-v=""4""
                    data-type=""inline""
                    data-width=""{maxWidth}""
                    data-width=""{maxHeight}""
                />");

                retval.AppendLine(
                    @"<script type=""text/javascript"" async src=""https://play.vidyard.com/embed/v4.js""></script>");

                return retval.ToString();
            }

            return null;
        }
    }
}
