using System;

using Telligent.Evolution.Extensibility.Version1;
using AngleSharp.Dom.Html;

namespace FourRoads.Common.TelligentCommunity.Plugins.Interfaces
{
	public interface ICQObserverPlugin:  IObserver<IHtmlDocument>, IPluginGroup, IDisposable 
	{
	}
}