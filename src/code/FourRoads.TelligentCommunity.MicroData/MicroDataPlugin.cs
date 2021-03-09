using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.RenderingHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Version1;

using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;

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

        public override IEnumerable<Type> Plugins
        {
            get
            {
                return new[]
                           {
                            typeof (RenderingObserverPlugin),
                            typeof (MicroDataGrid),
                            typeof (DependencyInjectionPlugin)
                           };
            }
        }

        public override string Name
        {
            get { return "4 Roads - MicroData Extensions"; }
        }

        public void Update(IPluginConfiguration configuration)
        {
            _microDataProcessor =
                new MicroDataProcessor(JsonConvert.DeserializeObject<MicroDataEntry[]>(configuration.GetCustom("markupConfiguration")));

            Initialize();
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup[] groupArray = new PropertyGroup[1];
                PropertyGroup optionsGroup = new PropertyGroup() {Id="options", LabelText = "Options"};
                groupArray[0] = optionsGroup;

                var defaultValue = JsonConvert.SerializeObject(MicroDataDefaultData.Entries);

                var markupConfiguration = new Property
                {
                    Id = "markupConfiguration",
                    LabelText = "",
                    DescriptionText = "",
                    DataType = "custom",
                    Template = "microdata_grid",
                    OrderNumber = 1,
                    DefaultValue = defaultValue
                };

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
