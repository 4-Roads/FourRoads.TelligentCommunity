using Telligent.Evolution.Extensibility.Administration.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigratorFramework: Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin, IInstallablePlugin, IAdministrationPanel, IPluginGroup, IScriptablePlugin, IHttpCallback
    {
        /// <summary>
        /// Indicates whether existing records will be updated or ignored
        /// </summary>
        bool UpdateExisting { get; }
    }
}
