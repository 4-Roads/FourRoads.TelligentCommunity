using System;

namespace FourRoads.TelligentCommunity.Mfa.Interfaces
{
    public interface ILock<T>
    {
        IDisposable Enter(T id);
    }
}