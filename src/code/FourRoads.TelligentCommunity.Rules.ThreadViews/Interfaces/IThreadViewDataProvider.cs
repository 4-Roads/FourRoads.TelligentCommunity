using System;
using System.Collections.Generic;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Entities;

namespace FourRoads.TelligentCommunity.Rules.ThreadViews.Interfaces
{
    public interface IThreadViewDataProvider
    {
        void Create(Guid appicationId, Guid contentId, int userId, DateTime viewDate, int status = 1);
        List<ThreadViewInfo> GetNewList(int threshold);
    }
}