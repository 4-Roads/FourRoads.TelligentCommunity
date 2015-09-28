using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.DeveloperTools.Plugins
{
    public interface IThemeUtilities :  IPluginGroup , ISingletonPlugin
    {
        bool EnableSourceMap { get; set; }
        bool EnableFileSystemWatcher { get; set; }
        bool EnableThemePageControls { get; set; }
    }
}
