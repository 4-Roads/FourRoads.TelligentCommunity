using System;
using AngleSharp.Dom.Html;

namespace FourRoads.TelligentCommunity.RenderingHelper
{
    public class RenderingObserverPlugin : IRenderingObserverPlugin
    {
        private readonly RenderingSubject _renderingObserverSubject = new RenderingSubject();

        public IObservable<IHtmlDocument> RenderObservable
        {
            get { return _renderingObserverSubject; }
        }

        public void NotifyObservers(IHtmlDocument document)
        {
            _renderingObserverSubject.Notify(document);
        }

        public void Initialize()
        {
        }

        public string Name
        {
            get { return "4 Roads - Rendering Observer Plugin"; }
        }

        public string Description
        {
            get { return "Allows plugins access to the rendering pipeline after all content is rendered"; }
        }
    }
}