// //  ------------------------------------------------------------------------------
// //  <copyright company="Four Roads LLC">
// //  Copyright (c) Four Roads LLC.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using IContent = Telligent.Evolution.Extensibility.Content.Version1.IContent;

namespace FourRoads.Common.TelligentCommunity.Components.Logic
{
    public class ContentLogic : IContentLogic
    {
        private static string _imageRegEx = @"<img[^>]*src=(?:(""|')(?<url>[^\1]*?)\1|(?<url>[^\s|""|'|>]+))";
        private static Regex _regex = new Regex(_imageRegEx, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // our plugins embed "source" attribute which has the original url 
        private static string _videoRegEx = @"<iframe[^>]*source=(?:(""|')(?<url>[^\1]*?)\1|(?<url>[^\s|""|'|>]+))";
        private static Regex _videoRegex = new Regex(_videoRegEx, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ContentLogic(IContents contentsService , IUrl urlService)
        {
            _contentsService = contentsService;
            _urlService = urlService;
        }


        private IContents _contentsService;
        private IUrl _urlService;

        #region IContentLogic Members

        public string GetBestImageUrl(Guid contentId, Guid contentTypeId)
        {
            return GetAllImageUrls(contentId, contentTypeId).FirstOrDefault();
        }

        public IEnumerable<string> GetAllImageUrls(Guid contentId , Guid contentTypeId)
        {
            List<string> images = new List<string>();

            IContent content = _contentsService.Get(contentId, contentTypeId);

            if (content != null)
            {
                ////Now does the content contain any images
                images.AddRange(ParseImagesFromContent(content.HtmlDescription("")));

                ////Container Image
                images.Add(_urlService.Absolute(content.Application.Container.AvatarUrl));
            }

            return images.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetAllVideoUrls(Guid contentId, Guid contentTypeId)
        {
            List<string> images = new List<string>();

            IContent content = _contentsService.Get(contentId, contentTypeId);

            if (content != null)
            {
                ////Now does the content contain any images
                images.AddRange(ParseVideosFromContent(content.HtmlDescription("")));

            }

            return images.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public string GetFirstVideoUrl(Guid contentId, Guid contentTypeId)
        {
            return GetAllVideoUrls(contentId, contentTypeId).FirstOrDefault();
        }

        #endregion

        public List<string> ParseImagesFromContent(string content)
        {
            List<string> results = new List<string>();
            if (content != null)
            {
                Regex regex = _regex;

                MatchCollection matches = regex.Matches(content);

                foreach (Match match in matches)
                {
                    string url = match.Groups["url"].Value;
 
                    var file = CentralizedFileStorage.GetCentralizedFileByUrl(url);

                    if (file != null)
                    {
                        results.Add(file.GetDownloadUrl());
                    }
                    else
                    {
                        results.Add(url);
                    }
                }
            }

            return results;
        }

        public List<string> ParseVideosFromContent(string content)
        {
            var results = new List<string>();
            if (content != null)
            {
                Regex regex = _videoRegex;

                MatchCollection matches = regex.Matches(content);

                foreach (Match match in matches)
                {
                    string videoUrl = match.Groups["url"].Value.StartsWith("http")
                        ? match.Groups["url"].Value
                        : string.Format("http:{0}", match.Groups["url"].Value);
                    Uri result;
                    if (Uri.TryCreate(_urlService.Absolute(videoUrl), UriKind.Absolute, out result))
                    {
                        results.Add(result.AbsoluteUri);
                    }
                }
            }

            return results;
        } 
    }
}