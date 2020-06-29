using System;
using System.Collections.Concurrent;
using System.Threading;
using FourRoads.TelligentCommunity.Mfa.Interfaces;

namespace FourRoads.TelligentCommunity.Mfa.Logic
{
    public class NamedItemLockSpin<T> : ILock<T>
    {

        private readonly ConcurrentDictionary<T, object> locks = new ConcurrentDictionary<T, object>();

        private readonly int spinWait;

        public NamedItemLockSpin(int spinWait)
        {
            this.spinWait = spinWait;
        }

        public IDisposable Enter(T id)
        {
            while (!locks.TryAdd(id, new object()))
            {
                Thread.SpinWait(spinWait);
            }

            return new ActionDisposable(() => exit(id));
        }

        private void exit(T id)
        {
            object obj;
            locks.TryRemove(id, out obj);
        }
    }
}