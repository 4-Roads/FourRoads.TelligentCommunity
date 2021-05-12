using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using FourRoads.TelligentCommunity.Mfa.Interfaces;

namespace FourRoads.TelligentCommunity.Mfa.Logic
{
    public class NamedItemLockSpin<T> : ILock<T>
    {
        private readonly ConcurrentDictionary<T, object> _locks = new ConcurrentDictionary<T, object>();

        private readonly int _spinWait;
        private readonly object _dummy = new object();

        public NamedItemLockSpin(int spinWait)
        {
            _spinWait = spinWait;
        }

        public IDisposable Enter(T id)
        {
            while (!_locks.TryAdd(id, _dummy))
            {
                Thread.SpinWait(_spinWait);
            }

            return new ActionDisposable(() => { _locks.TryRemove(id, out _); });
        }
    }
}