using System;
using System.Collections.Generic;
using FourRoads.TelligentCommunity.Installer.Plugins;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ExtendedSearch
{
    public class SeaachSuggestionaPlugin : IPluginGroup
    {
        public void Initialize()
        {

        }

        string IPlugin.Name => "4 Roads - Search Suggestion Extensions";
        string IPlugin.Description => "Extends the SOLR search to support suggested query results";
        public IEnumerable<Type> Plugins => new[] {typeof(SearchExtensionSCriptedFragementExtenstion), typeof(InstallerCore), typeof(DefaultWidgetInstaller) };
    }
}
