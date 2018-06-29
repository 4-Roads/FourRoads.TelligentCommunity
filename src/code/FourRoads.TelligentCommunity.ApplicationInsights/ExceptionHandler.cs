using System;
using System.Collections.Generic;
using System.Web;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ApplicationInsights
{
    public class ExceptionHandler : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.Error += ContextOnError;
        }

        private void ContextOnError(object sender, EventArgs eventArgs)
        {
            if (HttpContext.Current != null)
            {
                Exception ex = HttpContext.Current.Server.GetLastError();

                var plugin = PluginManager.GetSingleton<ApplicationInsightsPlugin>();

                if (plugin != null && ex != null)
                {
                    var context = Apis.Get<IUrl>().CurrentContext;

                    if (context != null)
                    {
                        plugin.TelemetryClient?.TrackException(
                            ex,
                            new Dictionary<string, string>
                            {
                                {"UserId", Apis.Get<IUrl>().CurrentContext.UserId.ToString()}
                            });
                    }
                }
            }
        }

        public void Dispose()
        {

        }
    }
}