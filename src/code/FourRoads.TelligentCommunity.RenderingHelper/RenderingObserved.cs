using CsQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FourRoads.TelligentCommunity.RenderingHelper
{

    public class RenderingObserved : IDisposable
    {
        RenderingSubject _subject;
        IObserver<CQ> _observer;

        public RenderingObserved(RenderingSubject subject, IObserver<CQ> observer)
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
