using System;
using System.Text.RegularExpressions;
using System.Web;

namespace FourRoads.TelligentCommunity.Links
{
    public class LinkModifyer
    {
        private bool _ensureLocalLinksMatchUriScheme = true;
        private bool _makeExternalUrlsTragetBlank = true;
        private bool _ensureLocalLinksLowercase = false;
        private static Regex _elementMatcher = new Regex("((?i)<a([^>]+)>(.+?)</a>)|((?i)<img([^>]+)/>)|((?i)<script([^>]+)/>)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex _anchorLinkMatcher = new Regex("(<a)((.*(href\\s*=\\s*(?<uri>\"([^\"]*\")|'[^']*'|([^>\\s]+))))((?!target)[^>])+?>.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex _srcLinkMatcher = new Regex("\\s*(?i)src\\s*=\\s*(\\\"([^\"]*\\\")|'[^']*'|([^'\">\\s]+))", RegexOptions.Compiled | RegexOptions.Multiline);

        public LinkModifyer(bool ensureLocalLinksMatchUriScheme, bool makeExternalUrlsTragetBlank, bool ensureLocalLinksLowercase)
        {
            _ensureLocalLinksMatchUriScheme = ensureLocalLinksMatchUriScheme;
            _makeExternalUrlsTragetBlank = makeExternalUrlsTragetBlank;
            _ensureLocalLinksLowercase = ensureLocalLinksLowercase;
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
                    result = _srcLinkMatcher.Replace(result, delegate(Match uriMatch)
                    {
                        return EnsureLocalLinksMatchUriScheme(uriMatch);
                    });
                }

                if (_makeExternalUrlsTragetBlank || _ensureLocalLinksLowercase)
                {
                    result = _anchorLinkMatcher.Replace(result, delegate(Match uriMatch)
                    {
                        var value = uriMatch.Value;
                        
                        if (_makeExternalUrlsTragetBlank)
                        {
                            value = SetTargetBlankForExternalLinks(value, uriMatch);
                        }

                        if (_ensureLocalLinksLowercase)
                        {
                            value = EnsureLocalLinksLowercase(value, uriMatch);
                        }

                        return value;
                    });
                }
            }            

            return result;
        }

        private static string EnsureLocalLinksMatchUriScheme(Match uriMatch)
        {
            if (uriMatch.Success)
            {
                //Is the URI absolute 
                string trimmed = uriMatch.Value.ToLower().Replace("src=", "").Trim(new[] { '"', '\'', ' ' });

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
        }

        private static string SetTargetBlankForExternalLinks(string original, Match uriMatch)
        {
            if (uriMatch.Success && uriMatch.Groups.Count > 4)
            {
                if (HttpContext.Current != null)
                {
                    var url = HttpContext.Current.Request.Url;

                    if (url.IsAbsoluteUri)
                    {
                        Uri thisUri;

                        if (uriMatch.Groups["uri"] != null && Uri.TryCreate(uriMatch.Groups["uri"].Value.Trim(new[] { '"', '\'' }), UriKind.RelativeOrAbsolute, out thisUri))
                        {
                            if (thisUri.IsAbsoluteUri && !thisUri.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped).Equals(url.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped), StringComparison.OrdinalIgnoreCase))
                            {
                                return uriMatch.Groups[1].Value + "  target='_blank' " + uriMatch.Groups[2].Value;
                            }
                        }
                    }
                }
            }

            return original;
        }

        private static string EnsureLocalLinksLowercase(string original, Match uriMatch)
        {
            if (uriMatch.Success && uriMatch.Groups.Count > 4 && uriMatch.Groups["uri"] != null)
            {
                var urlString = uriMatch.Groups["uri"].Value.Trim(new[] { '"', '\'' });

                // Don't lower case query strings as they could be case sensitive
                urlString = urlString.IndexOf('?') > 0 ?
                    urlString.Substring(0, urlString.IndexOf('?')) :
                    urlString;

                // Don't lower case anchor values as they could be case sensitive
                urlString = urlString.IndexOf('#') > 0 ?
                    urlString.Substring(0, urlString.IndexOf('#')) :
                    urlString;

                if (HttpContext.Current != null)
                {
                    var url = HttpContext.Current.Request.Url;

                    if (url.IsAbsoluteUri)
                    {
                        Uri thisUri;

                        if (Uri.TryCreate(urlString, UriKind.Absolute, out thisUri))
                        {
                            if (thisUri.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped).Equals(url.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped), StringComparison.OrdinalIgnoreCase))
                            {
                                return original.Replace(urlString, urlString.ToLower());
                            }
                        }
                        else if (urlString.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                        {
                            return original.Replace(urlString, urlString.ToLower());
                        }
                    }
                }
            }

            return original;
        }
    }
}