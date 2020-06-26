using System;

namespace FourRoads.TelligentCommunity.GoogleMfa.Interfaces
{
    public interface ILock<T>
    {
        IDisposable Enter(T id);
    }
}