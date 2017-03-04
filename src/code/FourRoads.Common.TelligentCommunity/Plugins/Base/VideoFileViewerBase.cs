using System;
using System.Web;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;


namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public abstract class VideoFileViewerBase : IPlugin, Telligent.Evolution.Extensibility.UI.Version1.IFileViewer, IConfigurablePlugin
    {
        private int _odering = 100;

        public abstract string SupportedUrlPattern { get; }

        public string Name { get { return "4 Roads - Viewers - File Viewer" + ViewerName; } }

        public string Description { get { return "Enables the detection and rendering of embedded URLs referencing " + ViewerName + " Videos"; } }

        protected abstract string ViewerName { get; }

        public int DefaultOrderNumber
        {
            get
            {
                return this._odering;
            }
        }

        public virtual string[] SupportedFileExtensions
        {
            get { return new string[0]; }
        }

        public virtual PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup propertyGroupArray = new PropertyGroup("options", "Options", 0);

                propertyGroupArray.Properties.Add(new Property("orderNumber", "Order Number", PropertyType.Int, 0, "100")
                {
                    DescriptionText = "Sets the priority when multiple file viewers can display the same file or URL (lower number takes priority)."
                });

                return new PropertyGroup[] { propertyGroupArray };
            }
        }

        public virtual string GetPreviewImageUrl()
        {
            return "~/utility/images/video-preview.gif";
        }

        public virtual string GetPreviewImageUrl(Uri url)
        {
            return GetPreviewImageUrl();
        }

        public abstract string CreateRenderedViewerMarkup(Uri url, int width, int height);

        public string Render(ICentralizedFile file, IFileViewerOptions options)
        {
            return string.Empty;
        }

        public string Render(Uri url, IFileViewerOptions options)
        {
            if (options.ViewType != FileViewerViewType.Preview)
                return CreateRenderedViewerMarkup(url, options.Width.HasValue ? options.Width.Value : 0, options.Height.HasValue ? options.Height.Value : 0);

            HttpContext current = HttpContext.Current;

            if (current != null)
            {
                return PublicApi.UI.GetResizedImageHtml(current.Response.ApplyAppPathModifier(this.GetPreviewImageUrl(url)), options.Height.HasValue ? options.Height.Value : 0, options.Width.HasValue ? options.Width.Value : 0);
            }

            return string.Empty;
        }

        public FileViewerMediaType GetMediaType(ICentralizedFile file, IFileViewerOptions options)
        {
            return FileViewerMediaType.Empty;
        }

        public FileViewerMediaType GetMediaType(Uri url, IFileViewerOptions options)
        {
            return options.ViewType == FileViewerViewType.Preview ? FileViewerMediaType.Image : FileViewerMediaType.Video;
        }

        public void Initialize()
        {
        }

        public void Update(IPluginConfiguration configuration)
        {
            this._odering = configuration.GetInt("orderNumber");
        }

    }
}
