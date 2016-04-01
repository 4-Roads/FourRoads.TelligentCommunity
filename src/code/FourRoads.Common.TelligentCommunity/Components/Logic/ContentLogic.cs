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
using Telligent.Evolution.Api.Content;
using Telligent.Evolution.Components;
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

        public ContentLogic(IContentService contentService, IAttachmentService attachmentService, ILegacyContentService legacyContentService, IFileViewerService fileViewerService)
        {
            if (contentService == null)
            {
                throw new ArgumentNullException("contentService");
            }

            if (attachmentService == null)
            {
                throw new ArgumentNullException("attachmentService");
            }

            if (fileViewerService == null)
            {
                throw new ArgumentNullException("fileViewerService");
            }

            if (legacyContentService == null)
            {
                throw new ArgumentNullException("legacyContentService");
            }

            ContentService = contentService;
            AttachmentService = attachmentService;
            LegacyContentService = legacyContentService;
            FileViewerService = fileViewerService;
        }

        protected IContentService ContentService { get; private set; }
        protected IAttachmentService AttachmentService { get; private set; }
        protected ILegacyContentService LegacyContentService { get; private set; }
        protected IFileViewerService FileViewerService { get; private set; }

        #region IContentLogic Members

        public string GetBestImageUrl(Guid contentId)
        {
            return GetAllImageUrls(contentId).FirstOrDefault();
        }

        public IEnumerable<string> GetAllImageUrls(Guid contentId)
        {
            List<string> images = new List<string>();
            IContent content = ContentService.Get(contentId);
            if (content != null)
            {
                LegacyContentIdentifier lContentId = LegacyContentService.GetLegacyIdentification(content);

                IViewableContent vContent = LegacyContentService.GetViewableContent(contentId);

                if (vContent != null && lContentId != null)
                {
                    int applicationId;

                    string[] ids = vContent.Container.UniqueID.Split(new[] {':'});

                    if (ids.Length == 2)
                    {
                        if (int.TryParse(ids[1], out applicationId))
                        {
                            IAttachment attachment = AttachmentService.Get(applicationId, (int) vContent.Container.ApplicationType, 0, lContentId.LegacyId);

                            if (attachment != null)
                            {
                                if (FileViewerService.GetMediaType(attachment.File, FileViewerViewType.Preview, false) == FileViewerMediaType.Image)
                                {
                                    images.Add(Globals.FullPath(attachment.Url));
                                }
                            }
                        }
                    }
                }

                //Now does the content contain any images
                images.AddRange(ParseImagesFromContent(content.HtmlDescription("")));

                //Container Image
                images.Add(Globals.FullPath(content.Application.Container.AvatarUrl));
            }

            return images.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetAllVideoUrls(Guid contentId)
        {
            List<string> images = new List<string>();
            IContent content = ContentService.Get(contentId);
            if (content != null)
            {
                LegacyContentIdentifier lContentId = LegacyContentService.GetLegacyIdentification(content);

                IViewableContent vContent = LegacyContentService.GetViewableContent(contentId);

                if (vContent != null && lContentId != null)
                {
                    int applicationId;

                    string[] ids = vContent.Container.UniqueID.Split(new[] { ':' });

                    if (ids.Length == 2)
                    {
                        if (int.TryParse(ids[1], out applicationId))
                        {
                            IAttachment attachment = AttachmentService.Get(applicationId, (int)vContent.Container.ApplicationType, 0, lContentId.LegacyId);

                            if (attachment != null)
                            {
                                if (FileViewerService.GetMediaType(attachment.File, FileViewerViewType.Preview, false) == FileViewerMediaType.Video)
                                {
                                    images.Add(Globals.FullPath(attachment.Url));
                                }
                            }
                        }
                    }
                }

                //Now does the content contain any images
                images.AddRange(ParseVideosFromContent(content.HtmlDescription("")));
            }

            return images.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public string GetFirstVideoUrl(Guid contentId)
        {
            return GetAllVideoUrls(contentId).FirstOrDefault();
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
                    Uri result;
                    if (Uri.TryCreate(Globals.FullPath(match.Groups["url"].Value), UriKind.Absolute, out result))
                    {
                        results.Add(result.AbsoluteUri);
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
                    if (Uri.TryCreate(Globals.FullPath(videoUrl), UriKind.Absolute, out result))
                    {
                        results.Add(result.AbsoluteUri);
                    }
                }
            }

            return results;
        } 
    }
}