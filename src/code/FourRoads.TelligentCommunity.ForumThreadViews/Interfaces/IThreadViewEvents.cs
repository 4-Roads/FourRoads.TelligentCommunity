using System;
using FourRoads.TelligentCommunity.ForumThreadViews.Events;

namespace FourRoads.TelligentCommunity.ForumThreadViews.Interfaces
{
    public interface IThreadViewEvents
    {
        void AfterView(Guid threadId);
        event EventAfterThreadViewHandler EventAfterView;
    }
}