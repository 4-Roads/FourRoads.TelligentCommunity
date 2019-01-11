using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.UI.Version1;
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
        public IEnumerable<Type> Plugins => new[] {typeof(SearchExtensionSCriptedFragementExtenstion), typeof(FactoryDefaultWidgetProviderInstaller) };
    }
}
