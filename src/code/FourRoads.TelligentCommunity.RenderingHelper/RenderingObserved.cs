using System;
using AngleSharp.Html.Dom;

namespace FourRoads.TelligentCommunity.RenderingHelper
{

    public class RenderingObserved : IDisposable
    {
        RenderingSubject _subject;
        IObserver<IHtmlDocument> _observer;

        public RenderingObserved(RenderingSubject subject, IObserver<IHtmlDocument> observer)
        {
            _subject = subject;
            _observer = observer;
        }

        public void Dispose()
        {
            _subject.Unsubscribe(_observer);
        }
    }

}
