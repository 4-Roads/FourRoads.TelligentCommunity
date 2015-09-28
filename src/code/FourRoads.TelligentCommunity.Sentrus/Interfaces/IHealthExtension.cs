namespace FourRoads.TelligentCommunity.Sentrus.Interfaces
{
    using Telligent.DynamicConfiguration.Components;
    using Telligent.Evolution.Extensibility.Version1;

    public interface IHealthExtension : IConfigurablePlugin
    {
        /// <summary>
        ///     Called by the task service on the configured frequency
        /// </summary>
        void ExecuteJob();

        /// <summary>
        ///     Returns the configuration information for this plugin
        /// </summary>
        /// <returns></returns>
        PropertyGroup GetConfiguration();
    }
}