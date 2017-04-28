using System;
using System.Collections.Generic;
using FourRoads.TelligentCommunity.ForumThreadViews.Entities;

namespace FourRoads.TelligentCommunity.ForumThreadViews.Interfaces
{
    public interface IThreadViewDataProvider
    {
        void Create(Guid appicationId, Guid contentId, int userId, DateTime viewDate, int status = 1);
        List<ThreadViewInfo> GetNewList(int threshold);
    }
}