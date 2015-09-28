using CsQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FourRoads.TelligentCommunity.RenderingHelper
{
    public class RenderingSubject : IObservable<CQ>
    {
        private static ReaderWriterLockSlim _observerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private List<IObserver<CQ>> _observers = new List<IObserver<CQ>>();
        public void Notify(CQ document)
        {
            _observerLock.EnterReadLock();

            try
            {
                foreach (IObserver<CQ> o in _observers)
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

        public void Unsubscribe(IObserver<CQ> observer)
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

        public IDisposable Subscribe(IObserver<CQ> observer)
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
