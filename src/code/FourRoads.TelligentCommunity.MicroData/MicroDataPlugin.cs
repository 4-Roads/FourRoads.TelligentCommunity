using FourRoads.TelligentCommunity.RenderingHelper;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MicroData
{
    public class MicroDataPlugin : CQObserverPluginBase, IMicroDataPlugin, IConfigurablePlugin, IPluginGroup
    {
        private MicroDataProcessor _microDataProcessor;

        public override string Description
        {
            get
            {
                return
                    "Provides extensions to allow the web pages to include schema.org micro data elements in the web page markup";
            }
        }

        public override string Name
        {
            get { return "4 Roads - MicroData Extensions"; }
        }

        public void Update(IPluginConfiguration configuration)
        {
            _microDataProcessor =
                new MicroDataProcessor(MicroDataSerializer.Deserialize(configuration.GetCustom("markupConfiguration")));

            Initialize();
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup[] groupArray = new PropertyGroup[1];
                PropertyGroup optionsGroup = new PropertyGroup("options", "Options", 0);
                groupArray[0] = optionsGroup;

                Property markupConfiguration = new Property("markupConfiguration", "", PropertyType.Custom, 0,
                    MicroDataSerializer.Serialize(MicroDataDefaultData.Entries))
                {ControlType = typeof (MicroDataGrid)};


                optionsGroup.Properties.Add(markupConfiguration);

                return groupArray;
            }
        }

        protected override ICQProcessor GetProcessor()
        {
            return _microDataProcessor;
        }
    }
}
