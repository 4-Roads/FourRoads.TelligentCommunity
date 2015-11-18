using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ThemeHelper.Plugins
{
    public interface IThemeUtilities :  IPluginGroup, IConfigurablePlugin, ISingletonPlugin
    {
        bool EnableSourceMap { get; set; }
        bool EnableFileSystemWatcher { get; set; }
        bool EnableThemePageControls { get; set; }
    }
}
