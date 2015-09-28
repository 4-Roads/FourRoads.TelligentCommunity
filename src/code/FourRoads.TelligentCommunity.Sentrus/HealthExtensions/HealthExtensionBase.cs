namespace FourRoads.TelligentCommunity.Sentrus.HealthExtensions
{
    using Interfaces;
    using Telligent.DynamicConfiguration.Components;
    using Telligent.Evolution.Extensibility.Version1;

    public abstract class HealthExtensionBase : IConfigurablePlugin
    {
        protected IPluginConfiguration Configuration;
        protected abstract string HealthName { get; }
        public abstract void Initialize();

        public string Name
        {
            get { return HealthName; }
        }

        public abstract string Description { get; }

        public void Update(IPluginConfiguration configuration)
        {
            Configuration = configuration;
            InternalUpdate(configuration);
        }

        public abstract PropertyGroup[] ConfigurationOptions { get; }

        public void ExecuteJob()
        {
            if (PluginManager.IsEnabled(PluginManager.GetSingleton<IHealthPlugin>()))
            {
                InternalExecute();
            }
        }

        public bool IsEnabled
        {
            get { return Configuration.GetBool("Enabled"); }
        }

        public abstract void InternalExecute();
        public abstract void InternalUpdate(IPluginConfiguration configuration);
        protected abstract PropertyGroup GetRootGroup();

        public virtual PropertyGroup GetConfiguration()
        {
            PropertyGroup group = GetRootGroup();

            group.Properties.Add(new Property("Enabled", "Enabled", PropertyType.Bool, 0, true.ToString()));

            return group;
        }
    }
}