using System;
using System.Text.RegularExpressions;
using System.Web;

namespace FourRoads.TelligentCommunity.Links
{
    public class LinkModifyer
    {
        private bool _ensureLocalLinksMatchUriScheme = true;
        private bool _makeExternalUrlsTragetBlank = true;
        private static Regex _elementMatcher = new Regex("((?i)<a([^>]+)>(.+?)</a>)|((?i)<img([^>]+)/>)|((?i)<script([^>]+)/>)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex _anchorLinkMatcher = new Regex("(<a)((.*(href\\s*=\\s*(?<uri>\"([^\"]*\")|'[^']*'|([^>\\s]+))))((?!target)[^>])+?>.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex _srcLinkMatcher = new Regex("\\s*(?i)src\\s*=\\s*(\\\"([^\"]*\\\")|'[^']*'|([^'\">\\s]+))", RegexOptions.Compiled | RegexOptions.Multiline);

        public LinkModifyer(bool ensureLocalLinksMatchUriScheme, bool makeExternalUrlsTragetBlank)
        {
            _ensureLocalLinksMatchUriScheme = ensureLocalLinksMatchUriScheme;
            _makeExternalUrlsTragetBlank = makeExternalUrlsTragetBlank;
        }

        public string UpdateHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            return _elementMatcher.Replace(html, MatchElement);
        }


        private string MatchElement(Match match)
        {
            string result = match.Value;

            if (match.Groups.Count > 1)
            {
                if (_ensureLocalLinksMatchUriScheme)
                {
                    //extract the src or href
                    result = _srcLinkMatcher.Replace(result, delegate(Match uriMatch)
                    {
                        if (uriMatch.Success)
                        {
                            //Is the URI absolute 
                            string trimmed = uriMatch.Value.ToLower().Replace("src=", "").Trim(new[] {'"', '\'', ' '});

                            //does this match the servers domain
                            if (HttpContext.Current != null)
                            {
                                var url = HttpContext.Current.Request.Url;

                                if (url.IsAbsoluteUri)
                                {
                                    Uri thisUri;

                                    if (Uri.TryCreate(trimmed, UriKind.Absolute, out thisUri))
                                    {
                                        if (thisUri.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped).Equals(url.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped), StringComparison.OrdinalIgnoreCase))
                                        {
                                            string scheme = url.GetComponents(UriComponents.Scheme, UriFormat.SafeUnescaped) + "://";

                                            if (!trimmed.StartsWith(scheme))
                                            {
                                                trimmed = trimmed.Substring(0, trimmed.IndexOf("://") + 3);

                                                return uriMatch.Value.Replace(trimmed, scheme);
                                            }
                                        }
                                    }
                                }
                            }

                        }
                        return uriMatch.Value;

                    });

                }

                if (_makeExternalUrlsTragetBlank)
                {
                    //extract the src or href
                    result = _anchorLinkMatcher.Replace(result, delegate(Match uriMatch)
                    {
                        if (uriMatch.Success && uriMatch.Groups.Count > 4)
                        {
                            if (HttpContext.Current != null)
                            {
                                var url = HttpContext.Current.Request.Url;

                                if (url.IsAbsoluteUri)
                                {
                                    Uri thisUri;

                                    if (uriMatch.Groups["uri"] != null && Uri.TryCreate(uriMatch.Groups["uri"].Value.Trim(new []{'"' , '\''}), UriKind.RelativeOrAbsolute, out thisUri))
                                    {
                                        if (thisUri.IsAbsoluteUri && !thisUri.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped).Equals(url.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped), StringComparison.OrdinalIgnoreCase))
                                        {
                                            return uriMatch.Groups[1].Value + "  target='_blank' " + uriMatch.Groups[2].Value;
                                        }
                                    }
                                }
                            }
                        }

                        return uriMatch.Value;
                    });
                }
            }

            return result;
        }

    }
}