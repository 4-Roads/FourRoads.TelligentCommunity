using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;

using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using Telligent.Evolution.Extensibility.Configuration.Version1;

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
            Apis.Get<IHtml>().Events.Render += EventsOnRender;
        }

        private void EventsOnRender(HtmlRenderEventArgs htmlRenderEventArgs)
        {
            htmlRenderEventArgs.RenderedHtml = new LinkModifyer(_ensureLocalLinksMatchUriScheme, _makeExternalUrlsTragetBlank, _ensureLocalLinksLowercase).UpdateHtml(htmlRenderEventArgs.RenderedHtml);
        }

    
        private bool _ensureLocalLinksMatchUriScheme = true;
        private bool _makeExternalUrlsTragetBlank = true;
        private bool _ensureLocalLinksLowercase = false;

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;

            _ensureLocalLinksMatchUriScheme = _configuration.GetBool("ensureLocalLinksMatchUriScheme").HasValue ? _configuration.GetBool("ensureLocalLinksMatchUriScheme").Value : true;
            _makeExternalUrlsTragetBlank = _configuration.GetBool("makeExternalUrlsTragetBlank").HasValue ? _configuration.GetBool("makeExternalUrlsTragetBlank").Value : true;
            _ensureLocalLinksLowercase = _configuration.GetBool("ensureLocalLinksLowercase").HasValue ? _configuration.GetBool("ensureLocalLinksLowercase").Value : false;
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
                PropertyGroup optionsGroup = new PropertyGroup
                {
                        Id ="options",
                        LabelText = "Options"
                };
                groupArray[0] = optionsGroup;

                optionsGroup.Properties.Add(new Property {
                    Id="ensureLocalLinksMatchUriScheme",
                    LabelText = "Match URI Scheme",
                    DescriptionText = "This will check each local link to match the URI scheme of the site, ie if the site is being access through HTTPS then any absolute links that are in the content will be changed to match",
                    DataType = "bool" ,
                    Template = "bool",
                    OrderNumber = 0,
                    DefaultValue = "true"
                });

                optionsGroup.Properties.Add(new Property {
                    Id="makeExternalUrlsTragetBlank",
                    LabelText = "Target Blank",
                    DescriptionText = "This will check each offsite anchor link and if enabled make every link target '_blank'",
                    DataType = "bool",
                    Template = "bool",
                    OrderNumber = 1,
                    DefaultValue = "true"
                });

                optionsGroup.Properties.Add(new Property {
                    Id="ensureLocalLinksLowercase",
                    LabelText = "Lowercase",
                    DescriptionText = "This will check each local link and make the link lowercase",
                    DataType = "bool", 
                    Template = "bool", 
                    OrderNumber = 2,
                    DefaultValue = "false"
                });

                return groupArray;
            }
        }

    }
}
