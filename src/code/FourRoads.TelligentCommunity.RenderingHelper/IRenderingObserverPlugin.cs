using System;

using Telligent.Evolution.Extensibility.Version1;
using AngleSharp.Dom.Html;

namespace FourRoads.TelligentCommunity.RenderingHelper
{
    public interface IRenderingObserverPlugin : ISingletonPlugin
    {
        IObservable<IHtmlDocument> RenderObservable { get; }
        void NotifyObservers(IHtmlDocument document);
    }
}