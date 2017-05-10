using System;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Events;

namespace FourRoads.TelligentCommunity.Rules.ThreadViews.Interfaces
{
    public interface IThreadViewService
    {
        IThreadViewEvents Events { get; }

        bool Create(Guid contentid, DateTime created);

        bool CheckforViewTriggers(int threshold);
    }
}