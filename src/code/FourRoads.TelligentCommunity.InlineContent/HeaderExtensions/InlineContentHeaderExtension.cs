using System.Collections.Generic;
using System.IO;
using System.Text;
using FourRoads.Common.TelligentCommunity.Components.Logic;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.InlineContent.HeaderExtensions
{
    public class InlineContentHeaderExtension : IHtmlHeaderExtension, IConfigurablePlugin 
    {
        private ICentralizedFileStorageProvider _inlineContentStore;
        private InlineContentLogic _inlineContentLogic = new InlineContentLogic();

        public void Initialize()
        {
            if (!string.IsNullOrWhiteSpace(GetFilestoreCssPath()))
            {
                InlineContentStore.Delete("css", "inlinecontent.css");
            }
        }

        public string Name
        {
            get { return "Header Extension"; }
        }

        public string Description
        {
            get { return "Handles the header css"; }
        }

        public string GetHeader(RenderTarget target)
        {
            if (_inlineContentLogic.CanEdit)
                return string.Format("<link href='{0}' type='text/css' rel='stylesheet' media='screen'>", GetFilestoreCssPath());

            return string.Empty;
        }

        public ICentralizedFileStorageProvider InlineContentStore
        {
            get
            {
                if (_inlineContentStore == null)
                {
                    _inlineContentStore = CentralizedFileStorage.GetFileStore(InlineContentLogic.FILESTORE_KEY);
                }
                return _inlineContentStore;
            }
        }

        private string GetFilestoreCssPath()
        {
            ICentralizedFile file = InlineContentStore.GetFile("css", "inlinecontent.css");

            if (file == null)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(Css)))
                {
                    file = InlineContentStore.AddUpdateFile("css", "inlinecontent.css", stream);
                }
            }

            return file.GetDownloadUrl();
        }

        protected string DefaultCss
        {
            get { return @"
                    .content-fragment.editing{ position: fixed;  z-index: 2000; left: 50px; background-color: white; padding:20px;}
                    .content-inline-editor{position:relative;border-color:#eeeeee;border-width:1px;border-style:dashed; display:inline-block;}
                    .content-inline-editor .editor{position:relative; z-index:20000;}
                    .content-inline-editor.content-updated{border-color:red;}
                    .content-inline-editor.highlight{border-color:Aqua;}
                    .content-inline-editor ul.content-inline-editor-buttons{display:none;}
                    .content-inline-editor .fourroads-inline-content {z-index: 19999 !important;}
                    .content-inline-editor.highlight ul.content-inline-editor-buttons {cursor:pointer;list-style:none;display:inline-block; position:absolute;top:-10px;left:0px;width:200px;padding:0;margin:0;}
                    .content-inline-editor.highlight ul.content-inline-editor-buttons li {display:inline-block;padding:3px;font-weight:bold;background-color:#424242;color:white;border-right: solid 1px grey;}
                    .fourroads-inline-content{    background-color: #FFFFFF;    border: 1px solid;    padding: 10px;    position: fixed;    z-index: 10000;}
            "; }
        }

        public bool IsCacheable
        {
            get { return true; }
        }

        public bool VaryCacheByUser
        {
            get { return false; }
        }

        protected IPluginConfiguration Configuration { get; private set; }

        protected string Css
        {
            get { return Configuration.GetString("css"); }
        }


        public void Update(IPluginConfiguration configuration)
        {
            Configuration = configuration;
        
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                List<PropertyGroup> groups = new List<PropertyGroup>();

                PropertyGroup group = new PropertyGroup("GeneralSettings", "General", 0);

                groups.Add(group);

                Property property = new Property("css", "CSS", PropertyType.String, 3, DefaultCss);
                
                group.Properties.Add(property);

                return groups.ToArray();
            }
        }
    }
}