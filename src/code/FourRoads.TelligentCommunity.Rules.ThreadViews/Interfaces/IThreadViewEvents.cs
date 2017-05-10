using System;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Events;

namespace FourRoads.TelligentCommunity.Rules.ThreadViews.Interfaces
{
    public interface IThreadViewEvents
    {
        void AfterView(Guid threadId);
        event EventAfterThreadViewHandler EventAfterView;
    }
}