// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Web.Optimization;
using FourRoads.TelligentCommunity.RenderingHelper;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Performance
{
    public class PerformancePlugin : CQObserverPluginBase, IConfigurablePlugin, INavigable,ISingletonPlugin
	{
		private IPluginConfiguration _configuration;
        private PerformanceRendering _performanceRendering;

		public override string Description
		{
			get
			{
				return "Improves rendering speed of pages by bundling and minifying CSS and Javascript references";
			}
		}

		public override void Initialize()
		{
			base.Initialize();
            Performance.Configuration.OptomizeGlobalCss = GlobalCSS;
            Performance.Configuration.OptomizeWidgetJs = WidgetJS;
            Performance.Configuration.OptomizeGlobalJs = GlobalJS;
            Performance.Configuration.BundleTimeout = new TimeSpan(3,0,0);

            _performanceRendering = new PerformanceRendering();

			ThemeConfigurationDatas.Refresh += ThemeConfigurationDatasOnRefresh;

            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Telligent.Evolution.Extensibility.Version1.PluginManager.AfterInitialization += PluginManager_AfterInitialization;
		}

        void PluginManager_AfterInitialization(object sender, EventArgs e)
        {
            _performanceRendering.PurgeBundles();
        }

		private void ThemeConfigurationDatasOnRefresh(object sender, EventArgs eventArgs)
		{
			_performanceRendering.PurgeBundles();
		}

		public override string Name
		{
			get { return "4 Roads - Rendering Performance Extensions"; }
		}

        protected override ICQProcessor GetProcessor()
		{
            return _performanceRendering;
        }

		public void Update(IPluginConfiguration configuration)
		{
			_configuration = configuration;

			Initialize();
		}

		private IPluginConfiguration Configuration
		{
			get { return _configuration ?? (_configuration = PluginManager.GetConfiguration(this)); }
        }

        public bool GlobalJS
        {
            get { return Configuration.GetBool("GlobalJS"); }
        }
        
        public bool WidgetJS
        {
            get { return Configuration.GetBool("WidgetJS"); }
        }

        public bool GlobalCSS
        {
            get { return Configuration.GetBool("GlobalCSS"); }
        }

        public PropertyGroup[] ConfigurationOptions {
            get
            {
                PropertyGroup[] groupArray = new PropertyGroup[2];
                PropertyGroup optionsGroup = new PropertyGroup("Javascript", "Javascript", 0);
                groupArray[0] = optionsGroup;

                optionsGroup.Properties.Add(new Property("GlobalJS", "Optimise any JS script entries in the rendered markup", PropertyType.Bool, 0, "true"));
                optionsGroup.Properties.Add(new Property("WidgetJS", "Optimise any JS script entries from the rendered widgets", PropertyType.Bool, 1, "true"));

                optionsGroup = new PropertyGroup("Stylesheets", "Stylesheets", 1);
                groupArray[1] = optionsGroup;

                optionsGroup.Properties.Add(new Property("GlobalCSS", "Optimise any CSS script entries in the rendered markup", PropertyType.Bool, 0, "true")); 


                return groupArray;

            }
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddRaw("bundling", "bundling/{*pathInfo}", null, null, null, new RawDefinitionOptions()
            {
                ForceLowercaseUrl = false
            });
        }
    }
}