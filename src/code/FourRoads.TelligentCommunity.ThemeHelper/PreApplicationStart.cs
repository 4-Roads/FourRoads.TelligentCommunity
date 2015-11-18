using System.Web;
using FourRoads.TelligentCommunity.ThemeHelper;
using FourRoads.TelligentCommunity.ThemeHelper.Modules;

[assembly: PreApplicationStartMethod(typeof(PreApplicationStart), "Start")]
namespace FourRoads.TelligentCommunity.ThemeHelper
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