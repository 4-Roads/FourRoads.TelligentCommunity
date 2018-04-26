// //  ------------------------------------------------------------------------------
// //  <copyright company="Four Roads LLC">
// //  Copyright (c) Four Roads LLC.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace FourRoads.Common.TelligentCommunity.Components.Interfaces
{
    public interface IContentLogic
    {
        string GetBestImageUrl(Guid contentId, Guid contentTypeId);
        IEnumerable<string> GetAllImageUrls(Guid contentId, Guid contentTypeId);
        IEnumerable<string> GetAllVideoUrls(Guid contentId, Guid contentTypeId);
        string GetFirstVideoUrl(Guid contentId, Guid contentTypeId);
    }
}