using System;
using CsQuery;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Interfaces
{
	public interface ICQObserverPlugin:  IObserver<CQ>, IPluginGroup, IDisposable 
	{
	}
}