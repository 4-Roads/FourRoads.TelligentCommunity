using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using PluginManager = Telligent.Evolution.Components.PluginManager;

namespace FourRoads.TelligentCommunity.MigratorFramework
{
    public class PluginDisabler : IDisposable
    {
        List<IPlugin> _disabledPlugins = new List<IPlugin>();

        public PluginDisabler()
        {
            //Moderation
            var mgr = Services.Get<IPluginManager>();
            IEnumerable<IPlugin> plugins = mgr.GetAll();
            List<IPlugin> pluginsFiltered = new List<IPlugin>();

            foreach (var plugin in plugins)
            {
                if (mgr.IsEnabled(plugin))
                {
                    if (plugin is IAbuseDetector ||
                        plugin is INotificationType ||
                        plugin is IActivityStoryType ||
                        plugin is INotificationDistributionType)
                    {
                        _disabledPlugins.Add(plugin);
                    }
                    else
                    {
                        pluginsFiltered.Add(plugin);
                    }
                }
            }

            mgr.SetEnabled(pluginsFiltered);
        }

        public void Dispose()
        {
            var mgr = Services.Get<IPluginManager>();
            IEnumerable<IPlugin> plugins = mgr.GetAll();

            List<IPlugin> revertList = new List<IPlugin>(plugins.Where(p => mgr.IsEnabled(p)));

            revertList.AddRange(_disabledPlugins);

            mgr.SetEnabled(revertList);
        }
    }
}