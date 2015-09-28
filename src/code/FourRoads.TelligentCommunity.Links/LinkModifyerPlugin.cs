using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.RenderingHelper;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Links
{
    public class LinkModifyerPlugin : IConfigurablePlugin
    {
        private IPluginConfiguration _configuration;
        public string Description
        {
            get { return "Provides extensions to allow the web pages adjust URL links in user generated content"; }
        }

        public string Name
        {
            get { return "4 Roads - URL Link Extensions"; }
        }


        public void Initialize()
        {
            PublicApi.Html.Events.Render += EventsOnRender;
        }

        private void EventsOnRender(HtmlRenderEventArgs htmlRenderEventArgs)
        {
            htmlRenderEventArgs.RenderedHtml = new LinkModifyer(_ensureLocalLinksMatchUriScheme, _makeExternalUrlsTragetBlank).UpdateHtml(htmlRenderEventArgs.RenderedHtml);
        }

    
        private bool _ensureLocalLinksMatchUriScheme = true;
        private bool _makeExternalUrlsTragetBlank = true;

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;

            _ensureLocalLinksMatchUriScheme = _configuration.GetBool("ensureLocalLinksMatchUriScheme");
            _makeExternalUrlsTragetBlank = _configuration.GetBool("makeExternalUrlsTragetBlank");
        }

        private IPluginConfiguration Configuration
        {
            get { return _configuration; }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup[] groupArray = new PropertyGroup[1];
                PropertyGroup optionsGroup = new PropertyGroup("options", "Options", 0);
                groupArray[0] = optionsGroup;

                Property schemeMatch = new Property("ensureLocalLinksMatchUriScheme", "Match URI Scheme", PropertyType.Bool, 0, "true") {DescriptionText = "This will check each local link to match the URI scheme of the site, ie if the site is being access through HTTPS then any absolute links that are in the content will be changed to match"};

                optionsGroup.Properties.Add(schemeMatch);

                Property externalNewWindow = new Property("makeExternalUrlsTragetBlank", "Target Blank", PropertyType.Bool, 1, "true") { DescriptionText = "This will check each offsite anchor link and if enabled make every link traget '_blank'" };

                optionsGroup.Properties.Add(externalNewWindow);

                return groupArray;
            }
        }

    }
}
