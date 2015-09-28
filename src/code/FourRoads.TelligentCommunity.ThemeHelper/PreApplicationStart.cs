using System.Web;
using FourRoads.TelligentCommunity.DeveloperTools;
using FourRoads.TelligentCommunity.DeveloperTools.Modules;

[assembly: PreApplicationStartMethod(typeof(PreApplicationStart), "Start")]
namespace FourRoads.TelligentCommunity.DeveloperTools
{
	public class PreApplicationStart
	{
		/// <summary>
		/// Dynamically registers HTTP Module
		/// </summary>
		public static void Start()
		{
			HttpApplication.RegisterModule(typeof(ThemeUtilityModule));
		}
	}
}