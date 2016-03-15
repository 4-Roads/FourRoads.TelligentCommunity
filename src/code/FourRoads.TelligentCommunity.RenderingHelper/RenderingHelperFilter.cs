using System;
using System.IO;
using System.Web;
using CsQuery;
using CsQuery.ExtensionMethods.Internal;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;

namespace FourRoads.TelligentCommunity.RenderingHelper
{
    public class RenderingHelperFilter : MemoryBlockStream
    {
        public RenderingHelperFilter(Stream sink, IRenderingObserverPlugin renderingObserverPlugin)
        {
            _sink = sink;
            _renderingObserverPlugin = renderingObserverPlugin;
        }

        private IRenderingObserverPlugin _renderingObserverPlugin;
        private Stream _sink;
        private bool _isClosing = false;
        private bool _isClosed = false;
        private IContextService _contextService;

        private IContextService ContextService
        {
            get
            {
                if (_contextService == null)
                {
                    _contextService = Services.Get<IContextService>();
                }
                return _contextService;
            }

        }

        public override void Flush()
        {
            if (_isClosing && !_isClosed)
            {
                try
                {
                    //Before the memory stream is closed we have a full copy of the page
                    //we can now put it into the document dom 
                    Seek(0, SeekOrigin.Begin);

                    PageContext currentContext = PublicApi.Url.CurrentContext;

                    if (currentContext != null && !string.IsNullOrWhiteSpace(currentContext.UrlName))
                    {
                        using (MemoryBlockStream tmpStream = new MemoryBlockStream())
                        {
                            CopyTo(tmpStream);

                            tmpStream.Seek(0, SeekOrigin.Begin);

                            //Config.DomIndexProvider = DomIndexProviders.None; this will improve performance but requires csquery 1.3.5-beta and above
                            Config.DomRenderingOptions = DomRenderingOptions.RemoveComments;

                            CQ document = CQ.CreateDocument(tmpStream, HttpContext.Current.Response.ContentEncoding);

                            _renderingObserverPlugin.NotifyObservers(document);

                            using (StreamWriter sw = new StreamWriter(_sink, HttpContext.Current.Response.ContentEncoding,(int) Length + 1000))
                            {
                                document.Render(OutputFormatters.HtmlEncodingNone, sw);
                            }
                        }
                    }
                    else
                    {
                        CopyTo(_sink);
                    }

                }
                catch (Exception ex)
                {
                    new CSException(CSExceptionType.UnknownHttpError, "Rendering Helper Failed", ex).Log();
                }
            }
            else if (!_isClosing && !_isClosed)
            {
                base.Flush();
            }
        }

        public override void Close()
        {
            HttpResponse resposne = HttpContext.Current.Response;

            if (!resposne.IsRequestBeingRedirected && (resposne.StatusCode > 199 && resposne.StatusCode < 300))
            {
            _isClosing = true;
            }

            Flush();
          
            _isClosed = true;
            _isClosing = false;

            base.Close();
        }
    }
}