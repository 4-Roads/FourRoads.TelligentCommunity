using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.UI;
using FourRoads.TelligentCommunity.DeveloperTools.Plugins;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;

namespace FourRoads.TelligentCommunity.DeveloperTools.Modules
{
    public class ThemeUtilityModule : IHttpModule
    {
        #region Fields

        public static bool ReloadPluginState = true;
        private static bool? _pluginEnabled = null;
        private ISecurityService _securityService;
        private IThemeUtilities _plugin;

        #endregion

        #region  Members

        public void Dispose()
        {


        }

        protected IThemeUtilities ThemeUtilityPlugin
        {
            get
            {
                if (_plugin == null)
                {
                    _plugin = Telligent.Evolution.Extensibility.Version1.PluginManager.GetSingleton<IThemeUtilities>();
                }

                return _plugin;
            }
        }

        private void PluginManagerOnAfterInitialization(object sender, EventArgs eventArgs)
        {
            ReloadPluginState = true;
        }

        protected bool PluginEnabled
        {
            get
            {
                if (ReloadPluginState || !_pluginEnabled.HasValue)
                {
                    _pluginEnabled = false;
                    if (ThemeUtilityPlugin != null)
                    {
                        _pluginEnabled = Telligent.Evolution.Extensibility.Version1.PluginManager.IsEnabled(ThemeUtilityPlugin) && ThemeUtilityPlugin.EnableThemePageControls;
                    }
                    ReloadPluginState = false;
                }

                return _pluginEnabled.Value;
            }
        }

        public void Init(HttpApplication context)
        {
            if (!context.Context.IsDebuggingEnabled)
                return;

            Telligent.Evolution.Extensibility.Version1.PluginManager.AfterInitialization += PluginManagerOnAfterInitialization;

            _securityService = Services.Get<ISecurityService>();
            context.PreRequestHandlerExecute += PreRequestHandlerExecute;
            context.PostMapRequestHandler += ContextPostMapRequestHandler;
        }

        #endregion

        private void PreRequestHandlerExecute(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication) sender;
            HttpContext context = application.Context;

            if (IsValid(context.Request))
            {
                NameValueCollection queryString = context.Request.QueryString;
                string clearCache = queryString["clearcache"] + queryString["cc"];
                const string cookieName = "FourRoads.TelligentCommunity.DeveloperTools.ThemeConsole";
                string disableCache = queryString["disablecache"] + queryString["dc"];

                if (string.IsNullOrEmpty(disableCache))
                {
                    HttpCookie cookie = context.Request.Cookies[cookieName];

                    if (cookie != null)
                    {
                        disableCache = cookie.Value;
                    }
                }
                else
                {
                    context.Response.Cookies.Set(new HttpCookie(cookieName)
                    {Value = IsTruthy(disableCache).ToString(CultureInfo.InvariantCulture).ToLower()});
                }

                if (IsTruthy(clearCache) || IsTruthy(disableCache))
                {
                    Telligent.Evolution.Extensibility.Version1.PluginManager.GetSingleton<ThemeUtility>().ResetCache();
                }
            }
        }

        private void ContextPostMapRequestHandler(object sender, EventArgs e)
        {
            Page page = HttpContext.Current.Handler as Page;
            User user = CSContext.Current.User;

            if (page != null && _securityService.For(Node.Root).Does(user).Have(SitePermission.ManageSettings))
                page.PreRender += PagePreRender;
        }



        private bool IsValid(HttpRequest request)
        {
            string extension = request.CurrentExecutionFilePathExtension ?? string.Empty;

            return (PluginEnabled &&
                    (string.IsNullOrEmpty(extension) || extension.Equals(".aspx", StringComparison.OrdinalIgnoreCase)) &&
                    !request.Url.GetLeftPart(UriPartial.Path).ToLower().Contains("/controlpanel"));
        }

        private void PagePreRender(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;

            if (IsValid(context.Request))
            {
                Page page = HttpContext.Current.Handler as Page;

                if (page == null)
                    return;

                // TODO: Get Javascript and CSS from plugin properties
                string js = EmbeddedResources.GetString(string.Format("{0}console.js", EmbeddedResources.BasePath));
                string css = EmbeddedResources.GetString(string.Format("{0}console.css", EmbeddedResources.BasePath));
                string url =
                    string.Format(
                        "/ControlPanel/Utility/RevertTheme.aspx?ThemeTypeID={0}&ThemeContextID={1}&ThemeName={2}",
                        SiteThemeContext.Instance().ThemeTypeID, Guid.Empty, CSContext.Current.SiteTheme);

                js += string.Format(@"
$(function () {{
    $.fourroads.plugins.themeConsole.register({{
        urls: {{
            modal: '{0}'
        }}
    }});
}})", url);
                Head.AddRawContent(string.Format(@"<style type=""text/css"">{0}</style>", css), HttpContext.Current);
                page.ClientScript.RegisterStartupScript(GetType(), "cacheconsole", js, true);
            }
        }

        private bool IsTruthy(string value)
        {
            return value != null && (value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                     value.Equals("yes", StringComparison.OrdinalIgnoreCase));
        }
    }
}