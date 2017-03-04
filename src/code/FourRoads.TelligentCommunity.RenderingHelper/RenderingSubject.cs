using AngleSharp.Dom.Html;

using System;
using System.Collections.Generic;
using System.Threading;

namespace FourRoads.TelligentCommunity.RenderingHelper
{
    public class RenderingSubject : IObservable<IHtmlDocument>
    {
        private static ReaderWriterLockSlim _observerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private List<IObserver<IHtmlDocument>> _observers = new List<IObserver<IHtmlDocument>>();
        public void Notify(IHtmlDocument document)
        {
            _observerLock.EnterReadLock();

            try
            {
                foreach (IObserver<IHtmlDocument> o in _observers)
                {
                    try
                    {
                        o.OnNext(document);
                    }
                    catch (Exception ex)
                    {
                        o.OnError(ex);
                    }

                    o.OnCompleted();
                }
            }
            finally
            {
                _observerLock.ExitReadLock();
            }
        }

        public void Unsubscribe(IObserver<IHtmlDocument> observer)
        {
            _observerLock.EnterWriteLock();

            try
            {
                _observers.Remove(observer);
            }
            finally
            {
                _observerLock.ExitWriteLock();
            }
        }

        public IDisposable Subscribe(IObserver<IHtmlDocument> observer)
        {
            _observerLock.EnterWriteLock();

            try
            {
                _observers.Add(observer);

                return new RenderingObserved(this, observer);
            }
            finally
            {
                _observerLock.ExitWriteLock();
            }

        }
    }
}
