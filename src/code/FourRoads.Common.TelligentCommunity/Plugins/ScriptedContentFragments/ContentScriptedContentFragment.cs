// //  ------------------------------------------------------------------------------
// //  <copyright company="Four Roads LLC">
// //  Copyright (c) Four Roads LLC.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.ScriptedContentFragments
{
    public class ContentScriptedContentFragment
    {
        public ContentScriptedContentFragment(IContentLogic contentLogic)
        {
            if (contentLogic == null)
            {
                throw new ArgumentNullException("contentLogic");
            }

            ContentLogic = contentLogic;
        }

        protected IContentLogic ContentLogic { get; private set; }


        public string GetBestImageUrl(string contentId)
        {
            return GetBestImageUrl(new Guid(contentId));
        }

        public ApiList<string> GetAllImageUrls(string contentId)
        {
            return GetAllImageUrls(new Guid(contentId));
        }

        public string GetBestImageUrl(Guid contentId)
        {
            return ContentLogic.GetBestImageUrl(contentId);
        }

        public ApiList<string> GetAllImageUrls(Guid contentId)
        {
            return new ApiList<string> (ContentLogic.GetAllImageUrls(contentId));
        }

        public ApiList<string> GetVideoUrls(Guid contentId)
        {
            return new ApiList<string>(ContentLogic.GetAllVideoUrls(contentId));
        }

        public string GetFirstVideoUrl(Guid contentId)
        {
            return ContentLogic.GetFirstVideoUrl(contentId);
        }

        public string GetVideoPlayerHtml(string videoUrl, int maxWidth, int maxHeight)
        {
            var fvs = Services.Get<IFileViewerService>();
            if (fvs == null)
            {
                return null;
            }
            string html = fvs.Render(new Uri(videoUrl), FileViewerViewType.View, maxWidth, maxHeight, false, true);

            return html;
        }
    }
}