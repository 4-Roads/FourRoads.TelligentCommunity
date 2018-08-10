using Microsoft.Web.Infrastructure.DynamicModuleHelper;

namespace FourRoads.TelligentCommunity.ApplicationInsights
{
    public class ExceptionHandlerStart
    {
        /// <summary>
        /// Dynamically registers HTTP Module
        /// </summary>
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(ExceptionHandler));
        }
    }
}