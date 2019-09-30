// //  ------------------------------------------------------------------------------
// //  <copyright company="4 Roads LTD">
// //  Copyright (c) 4 Roads LTD 2019.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
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
            ContentLogic = contentLogic;
        }

        protected IContentLogic ContentLogic { get; private set; }

        public string GetBestImageUrl(Guid contentId , Guid contentTypeId)
        {
            return ContentLogic.GetBestImageUrl(contentId , contentTypeId);
        }

        public ApiList<string> GetAllImageUrls(Guid contentId, Guid contentTypeId)
        {
            return new ApiList<string> (ContentLogic.GetAllImageUrls(contentId, contentTypeId));
        }

        public ApiList<string> GetVideoUrls(Guid contentId, Guid contentTypeId)
        {
            return new ApiList<string>(ContentLogic.GetAllVideoUrls(contentId, contentTypeId));
        }

        public string GetFirstVideoUrl(Guid contentId, Guid contentTypeId)
        {
            return ContentLogic.GetFirstVideoUrl(contentId, contentTypeId);
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