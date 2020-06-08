using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MigratorFramework
{
    public class PluginDisabler : IDisposable
    {
        private readonly IMigrationRepository _repository;
        private List<IPlugin> _disabledPlugins = new List<IPlugin>();

        public PluginDisabler(IMigrationRepository repository)
        {
            _repository = repository;
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

            var pluginsNames = string.Join(", ", _disabledPlugins.Select(p => p.Name));
            _repository.CreateLogEntry($"The following plugins were disabled:{pluginsNames}" , EventLogEntryType.Information);

            mgr.SetEnabled(pluginsFiltered);
        }

        public void Dispose()
        {
            var mgr = Services.Get<IPluginManager>();
            IEnumerable<IPlugin> plugins = mgr.GetAll();

            List<IPlugin> revertList = new List<IPlugin>(plugins.Where(p => mgr.IsEnabled(p)));

            revertList.AddRange(_disabledPlugins);

            var pluginsNames = string.Join(", ", _disabledPlugins.Select(p => p.Name));
            _repository.CreateLogEntry($"The following plugins were enabled:{pluginsNames}", EventLogEntryType.Information);

            mgr.SetEnabled(revertList);
        }
    }
}