using System;
using CsQuery;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.RenderingHelper
{
    public interface IRenderingObserverPlugin : ISingletonPlugin
    {
        IObservable<CQ> RenderObservable { get; }
        void NotifyObservers(CQ document);
    }
}