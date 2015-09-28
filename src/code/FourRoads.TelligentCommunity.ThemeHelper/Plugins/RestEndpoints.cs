using System;
using FourRoads.TelligentCommunity.DeveloperTools.Api.Rest;
using Telligent.Evolution.Extensibility.Rest.Version2;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.DeveloperTools.Plugins
{
    public class RestEndpoints: ISingletonPlugin, IRestEndpoints
    {
        #region IPlugin Members

        public string Description
        {
            get { return "Adds support for theme utility REST endpoints."; }
        }

        public void Initialize()
        {
        }

        public string Name
        {
            get { return "4 Roads - Theme utility REST endpoints"; }
        }

        #endregion

        #region IRestEndpoints Members

        public void Register(IRestEndpointController restRoutes)
        {
            IThemeUtilities utilites = PluginManager.GetSingleton<IThemeUtilities>();

            if (utilites == null || !utilites.EnableThemePageControls)
                return;

            restRoutes.Add(2, "themeutility/reset", HttpMethod.Post, request =>
            {
                var response = new RestResponse();

                try
                {
                    string action = request.Request.QueryString["action"];

                    if (string.IsNullOrEmpty(action))
                        throw new ArgumentException("Action parameter is required ('theme' or 'cache').");

                    ThemeUtility themeUtility = PluginManager.GetSingleton<ThemeUtility>();

                    if (themeUtility != null)
                    {
                        if(action.Equals("theme", StringComparison.OrdinalIgnoreCase))
                            themeUtility.RevertTheme();

                        if (action.Equals("cache", StringComparison.OrdinalIgnoreCase))
                            themeUtility.ResetCache();

                    }
                }
                catch (Exception ex)
                {
                    response.Errors = new [] { ex.Message };
                }

                return response;
            });
        }

        #endregion
    }
}