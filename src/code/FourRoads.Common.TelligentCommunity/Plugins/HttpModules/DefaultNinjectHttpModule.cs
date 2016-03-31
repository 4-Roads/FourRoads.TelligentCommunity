using System.Web;

namespace FourRoads.Common.TelligentCommunity.Plugins.HttpModules
{
	public class DefaultNinjectHttpModule : InjectorHttpModule<DefaultIocHandler>
	{
		/// <summary>
		/// Dynamically registers HTTP Module
		/// </summary>
		public static void Start()
		{
			HttpApplication.RegisterModule(typeof(DefaultNinjectHttpModule));
		}
	}
}