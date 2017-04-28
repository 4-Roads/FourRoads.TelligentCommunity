using System;
using FourRoads.TelligentCommunity.ForumThreadViews.Interfaces;
using Telligent.Evolution.Extensibility.Events.Version1;

namespace FourRoads.TelligentCommunity.ForumThreadViews.Events
{
    public class ThreadViewEventsArgs
    {
        public Guid ForumThreadId { get; set; }
        public int Views { get; set; }
    }

    public delegate void EventAfterThreadViewHandler(ThreadViewEventsArgs e);

    public class ThreadViewEvents : EventsBase, IThreadViewEvents
    {
        private readonly object _afterView = new object();

        public void AfterView(Guid threadId)
        {
            Get<EventAfterThreadViewHandler>(_afterView)?.Invoke(new ThreadViewEventsArgs() { ForumThreadId = threadId });
        }

        public event EventAfterThreadViewHandler EventAfterView
        {
            add { Add(_afterView, value); }
            remove { Remove(_afterView, value); }
        }
    }
}
