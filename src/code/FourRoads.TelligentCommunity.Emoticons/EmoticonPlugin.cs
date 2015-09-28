using System;
using System.Collections.Generic;
using FourRoads.Common;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Emoticons.CentralizedFileStore;
using FourRoads.TelligentCommunity.Emoticons.Interfaces;
using FourRoads.TelligentCommunity.Emoticons.Logic;
using Ninject.Modules;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Emoticons
{
    public class EmoticonPlugin : IEmoticonPlugin
    {
        public void Initialize()
        {
            PublicApi.Html.Events.Render += EventsOnRender;
        }

        private IEmoticonLogic _emoticonLogic;

        private IEmoticonLogic EmoticonLogic {
            get
            {
                if (_emoticonLogic == null)
                    _emoticonLogic = Injector.Get<IEmoticonLogic>();

                return _emoticonLogic;
            }
        }

        private void EventsOnRender(HtmlRenderEventArgs htmlRenderEventArgs)
        {
            htmlRenderEventArgs.RenderedHtml = EmoticonLogic.UpdateMarkup(htmlRenderEventArgs.RenderedHtml, Width, Height);
        }

        public string Name
        {
            get { return "4 Roads - Advanced Emoticon Support"; }
        }

        public string Description
        {
            get { return "This plugin provides emoticon support that does not require the user to wrap the emoticons in [], it also uses CSS classes and span tags to allow a user to copy and paste the text without copying images"; }
        }

        public void LoadBindings(NinjectModule module)
        {
            module.Bind<IEmoticonLogic>().To<EmoticonLogic>().InSingletonScope();
        }

        public int LoadOrder {
            get { return 0; }
        }

        public string GetHeader(RenderTarget target)
        {
            return string.Format("<link href='{0}' type='text/css' rel='stylesheet' media='screen'>", EmoticonLogic.GetFilestoreCssPath());
        }

        public bool IsCacheable {
            get { return true; }
        }

        public bool VaryCacheByUser {
            get { return false; }
        }

        public IEnumerable<Type> Plugins {
            get
            {
                return new[]
                {
                    typeof(EmoticonsStore),
                    typeof (DependencyInjectionPlugin)
                };
            }
        }

        public int Width { get; set; }
        public int Height { get; set; }

        public void Update(IPluginConfiguration configuration)
        {
            Width = configuration.GetInt("width");
            Height = configuration.GetInt("height");
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup grp = new PropertyGroup("options" , "Options" , 0);
                
                Property width = new Property("width" ,"Width" , PropertyType.Int , 0 , "12");
                Property height = new Property("height", "Height", PropertyType.Int, 0, "12");

                grp.Properties.Add(width);
                grp.Properties.Add(height);

                return new [] { grp };
            }
        }
    }
}
