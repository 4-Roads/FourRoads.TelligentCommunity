using System;
using System.Collections.Generic;
using System.Linq;

using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using Telligent.Common;
using Telligent.Evolution.Components;
using AngleSharp.Dom.Html;

namespace FourRoads.TelligentCommunity.RenderingHelper
{
    public interface ICQProcessor 
    {
        void Process(IHtmlDocument document);
    }

	public abstract class CQObserverPluginBase : ICQObserverPlugin
	{
		private IPluginManager _manager;
		protected IDisposable Observer;

		protected IPluginManager PluginManager
		{
			get { return _manager ?? (_manager = Services.Get<IPluginManager>()); }
		}

		#region IObserver<CQ> Members
         
		public virtual void OnCompleted()
		{

		}

		public virtual void OnError(Exception error)
		{
		}

		public void OnNext(IHtmlDocument value)
		{
            try
            {
                if (PluginManager.IsEnabled(this))
                {
                    //Decouple the processing of the document from the plugin
                    GetProcessor().Process(value);
                }
            }
            catch(Exception ex)
            {
                new TCException(CSExceptionType.UnknownError, "Rendering observer plugin error", ex).Log();
            }
		}

        protected abstract ICQProcessor GetProcessor();

		#endregion

		#region IPluginGroup Members

		public virtual IEnumerable<Type> Plugins
		{
			get
			{
				return new[]
				       	{
				       		typeof (RenderingObserverPlugin),
                            typeof (DependencyInjectionPlugin)
				       	};
			}
		}

		#endregion

		#region IPlugin Members

		public abstract string Description { get; }

		public virtual void Initialize()
		{
            PluginManager.AfterInitialization += (sender, args) =>
            {
                if (Observer != null)
                {
                    Observer.Dispose();
                    Observer = null;
                }

                IRenderingObserverPlugin renderingObserverPlugin = PluginManager.Get<IRenderingObserverPlugin>().FirstOrDefault();

                if (renderingObserverPlugin != null)
                {
                    Observer = renderingObserverPlugin.RenderObservable.Subscribe(this);
                }
            };
		}

		public abstract string Name { get; }

		#endregion

		#region IDisposable Members

		public virtual void Dispose()
		{
			if (Observer != null)
				Observer.Dispose();
		}

		#endregion
	}
}