// //  ------------------------------------------------------------------------------
// //  <copyright company="4 Roads LTD">
// //  Copyright (c) 4 Roads LTD 2019.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------


//todo - kb - maybe delete ICallerPathVistor22? 

using System;
using System.Collections.Generic;

namespace FourRoads.Common.TelligentCommunity.Components.Interfaces
{
    public interface ICallerPathVistor
    {
        string GetPath();
    }

    public interface IContentLogic
    {
        string GetBestImageUrl(Guid contentId, Guid contentTypeId);
        IEnumerable<string> GetAllImageUrls(Guid contentId, Guid contentTypeId);
        IEnumerable<string> GetAllVideoUrls(Guid contentId, Guid contentTypeId);
        string GetFirstVideoUrl(Guid contentId, Guid contentTypeId);
    }
}