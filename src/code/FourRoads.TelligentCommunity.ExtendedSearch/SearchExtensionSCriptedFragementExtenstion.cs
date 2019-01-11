using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ExtendedSearch
{
    public class SearchExtensionSCriptedFragementExtenstion : IScriptedContentFragmentExtension
    {
        public void Initialize()
        {

        }

        string IPlugin.Name => "4 Roads - Search Scripted Fragment Content Extensions";
        string IPlugin.Description => "Exposes fr_search_ext extension to nVelocity";
        public string ExtensionName => "fr_search_ext";
        public object Extension => new SearchExtensions();
    }
}