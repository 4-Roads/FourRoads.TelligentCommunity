using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;

namespace FourRoads.TelligentCommunity.Sentrus.HealthExtensions
{
    using Interfaces;
    using Telligent.Evolution.Extensibility.Configuration.Version1;
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
            get { return Configuration.GetBool("Enabled").HasValue ? Configuration.GetBool("Enabled").Value : true; }
        }

        public abstract void InternalExecute();
        public abstract void InternalUpdate(IPluginConfiguration configuration);
        protected abstract PropertyGroup GetRootGroup();

        public virtual PropertyGroup GetConfiguration()
        {
            PropertyGroup group = GetRootGroup();

            group.Properties.Add(new Property
            {
                Id = "Enabled",
                LabelText = "Enabled",
                DataType = "bool",
                Template = "bool",
                OrderNumber = 0,
                DefaultValue = bool.TrueString
            });

            return group;
        }
    }
}