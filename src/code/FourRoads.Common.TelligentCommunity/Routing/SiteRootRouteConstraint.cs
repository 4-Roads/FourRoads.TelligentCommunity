using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using Telligent.Evolution.Extensibility.Urls.Version1;


namespace FourRoads.Common.TelligentCommunity.Routing
{
    public class SiteRootRouteConstraint : IRouteConstraint, IComparableRouteConstraint
    {
        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (routeDirection != RouteDirection.IncomingRequest)
                return true;

            if (httpContext.Request.AppRelativeCurrentExecutionFilePath != "~/")
                return httpContext.Request.AppRelativeCurrentExecutionFilePath.ToLower().StartsWith("~/tags");

            return true;
        }


        public bool IsEqual(IComparableRouteConstraint constraint)
        {
            return GetType() == constraint.GetType();
        }
    }
}
