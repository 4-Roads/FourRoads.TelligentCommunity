// //  ------------------------------------------------------------------------------
// //  <copyright company="Four Roads LLC">
// //  Copyright (c) Four Roads LLC.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.Common.TelligentCommunity.Components.Interfaces
{
    public interface IContentLogic
    {
        string GetBestImageUrl(Guid contentId);
        IEnumerable<string> GetAllImageUrls(Guid contentId);
        IEnumerable<string> GetAllVideoUrls(Guid contentId);
        string GetFirstVideoUrl(Guid contentId);
    }
}