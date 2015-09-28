using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FourRoads.TelligentCommunity.Emoticons.CentralizedFileStore;
using FourRoads.TelligentCommunity.Emoticons.Interfaces;
using Telligent.Evolution;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.Emoticons.Logic
{
    public class EmoticonLogic : IEmoticonLogic
    {
        private Regex _emoticonReplacementMatch = null;
        private Dictionary<string, string> _emoticonReplacementLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string _cssToRender = string.Empty;
        private ICentralizedFileStorageProvider _emoticonStore;

        public EmoticonLogic()
        {
            Reset();
        }

        public ICentralizedFileStorageProvider EmoticonStore
        {
            get
            {

                if (_emoticonStore == null)
                {
                    _emoticonStore = CentralizedFileStorage.GetFileStore(EmoticonsStore.FILESTORE_KEY);
                }
                return _emoticonStore;
            }
        }

        public void Reset()
        {
            _emoticonReplacementMatch = null;
        }

        public string UpdateMarkup(string renderedHtml, int smileyWidth, int smileyHeight)
        {
            if (_emoticonReplacementMatch == null) 
            {
                InitializeRegularExpressionAndCss(smileyWidth, smileyHeight);
            }

            if (_emoticonReplacementMatch != null)
            {
                renderedHtml = _emoticonReplacementMatch.Replace(renderedHtml, Evaluator);
            }
        return renderedHtml;
        }

        public string GetFilestoreCssPath()
        {
            ICentralizedFile file = EmoticonStore.GetFile("css", "emoticons.css");

            if (file == null)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(_cssToRender)))
                {
                    file = EmoticonStore.AddUpdateFile("css", "emoticons.css", stream);
                }
            }

            return file.GetDownloadUrl();
        }

        private string Evaluator(Match match)
        {
            string replacement;
            if (_emoticonReplacementLookup.TryGetValue(match.Value, out replacement))
            {
                return string.Format(replacement, match.Value);
                }
            return match.ToString();
        }

        private static Regex _replaceCssErrors = new Regex("[^a-zA-Z0-9-]", RegexOptions.Compiled);

        private void InitializeRegularExpressionAndCss(int smileyWidth, int smileyHeight)
        {
            StringBuilder regExMatchString = new StringBuilder();
            StringBuilder cssToRender = new StringBuilder();

            cssToRender.Append("span.smiley-common span{display:none;} ");

            foreach (Smiley smily in Smilies.GetSmilies())
            {
                string smileyCode = smily.SmileyCode;

                if (!string.IsNullOrWhiteSpace(smily.SmileyText) && !string.IsNullOrWhiteSpace(smileyCode))
                {
                if (regExMatchString.Length > 0)
                {
                    regExMatchString.Append("|");
                }

                    if (smileyCode.Length == 1 || smileyCode.All(char.IsLetterOrDigit))
                {
                    smileyCode = "[" + smileyCode + "]";
                }

                    regExMatchString.Append(Regex.Escape(Transforms.Encode.HtmlEncode(smileyCode)).Replace("'", "(?:'|\\&\\#39;)"));

                    string cssClassName = "smiley-" + _replaceCssErrors.Replace(PublicApi.UI.MakeCssClassName(smily.SmileyText),""); //bug in telligents make css class

                CreateCss(cssToRender, cssClassName, smily.SmileyUrl, smileyWidth, smileyHeight);

                _emoticonReplacementLookup[smileyCode] = string.Format("<span class=\"smiley-common {0}\" title=\"{1}\"><span>{{0}}</span></span>", cssClassName, Transforms.Encode.HtmlEnsureEncoded(smily.SmileyText));

                if (smileyCode.Contains("'"))
                {
                    _emoticonReplacementLookup[smileyCode.Replace("'", "&#39;")] = _emoticonReplacementLookup[smileyCode];
                }
            }
            }
           
            if (regExMatchString.Length > 0)
            {
                _emoticonReplacementMatch = new Regex(@"(?<!<[^>]*)(?<!\<script\>.*)(" + regExMatchString + @")(?<![^>].*<)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                _cssToRender = cssToRender.ToString();
            }
        }

        private void CreateCss(StringBuilder cssToRender, string cssClassName , string url,int smileyWidth , int smileyHeight)
        {
            string emoticonRootPath = Globals.GetSiteUrls().Emoticon;

            cssToRender.AppendFormat(" span.{0}{{ display: inline-block;width: {3}px;height: {4}px;cursor: text;background: url('{1}{2}') no-repeat;text-align: left;background-size:{3}px {4}px}} ", cssClassName, emoticonRootPath, url, smileyWidth, smileyHeight);
        }
    }
}
