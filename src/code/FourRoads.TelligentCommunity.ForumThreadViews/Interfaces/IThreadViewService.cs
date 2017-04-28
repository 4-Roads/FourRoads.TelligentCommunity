using System;
using FourRoads.TelligentCommunity.ForumThreadViews.Events;

namespace FourRoads.TelligentCommunity.ForumThreadViews.Interfaces
{
    public interface IThreadViewService
    {
        IThreadViewEvents Events { get; }

        bool Create(Guid contentid, DateTime created);

        bool CheckforViewTriggers(int threshold);
    }
}