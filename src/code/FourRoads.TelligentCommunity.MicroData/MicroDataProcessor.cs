using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.RenderingHelper;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using IContent = Telligent.Evolution.Extensibility.Content.Version1.IContent;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace FourRoads.TelligentCommunity.MicroData
{
    public class MicroDataProcessor : ICQProcessor
    {
        private readonly IEnumerable<MicroDataEntry> _microDataEntries;
        private Dictionary<Guid , IWebContextualContentType> _webContextualContentTypes;

        public Dictionary<Guid, IWebContextualContentType> WebContextualContentTypes
        {
            get
            {
                if (_webContextualContentTypes == null)
                {
                    _webContextualContentTypes = new Dictionary<Guid, IWebContextualContentType>();

                    foreach (IWebContextualContentType contentType in PluginManager.Get<IWebContextualContentType>())
                    {
                        _webContextualContentTypes.Add(contentType.ContentTypeId, contentType);
                    }
                }
                return _webContextualContentTypes;
            }
        }

        public MicroDataProcessor(IEnumerable<MicroDataEntry> microDataEntries)
        {
            if (microDataEntries != null)
            {
                _microDataEntries = microDataEntries.OrderByDescending(o => o.ContentType.GetValueOrDefault(Guid.Empty));
            }
        }


        public void Process(IHtmlDocument parsedContent)
        {
            if (_microDataEntries != null)
            {
                try
                {
                    var body = parsedContent.QuerySelectorAll("body").FirstOrDefault();

                    if ( body != null )
                    {
                        Dictionary<Guid, IContent> contentLookup = new Dictionary<Guid, IContent>();

                        foreach ( MicroDataEntry semanticEntry in _microDataEntries )
                        {
                            Guid semanticType = semanticEntry.ContentType.GetValueOrDefault(Guid.Empty);

                            if ( semanticType == Guid.Empty )
                            {
                                ProcessSymanticEntry(semanticEntry, body);
                            }
                            else if ( WebContextualContentTypes.ContainsKey(semanticType) )
                            {
                                if ( !contentLookup.ContainsKey(semanticType) )
                                {
                                    contentLookup.Add(semanticType, WebContextualContentTypes[ semanticType ].GetCurrentContent(new WebContext()));
                                }

                                if ( contentLookup[ semanticType ] != null )
                                {
                                    ProcessSymanticEntry(semanticEntry, body);
                                }
                            }
                        }

                    }                }
                catch (Exception ex)
                {
                    new TCException( "MicroDataPlugin unknown error", ex).Log();
                }
            }
        }

        private class WebContext : IWebContext
        {
            private NameValueCollection _nvc;
            private string _url = null;

            public NameValueCollection QueryString
            {
                get
                {
                    if (_nvc == null)
                    {
                        if (HttpContext.Current != null)
                        {
                            _nvc = new NameValueCollection(HttpContext.Current.Request.QueryString);
                        }
                        else
                        {
                            _nvc = new NameValueCollection();
                        }
                    }
                    return _nvc;
                }
            }

            public string Url
            {
                get
                {
                    if (_url == null)
                    {
                        if (HttpContext.Current != null)
                        {
                            _url = HttpContext.Current.Request.RawUrl;

                            string rewritten = HttpContext.Current.Request.Headers["X-REWRITE-URL"];

                            if (rewritten != null)
                            {
                                _url = rewritten;
                            }
                        }
                    }
                    return _url ?? string.Empty;
                }
            }

            public int UserId
            {
                get
                {
                    if (Apis.Get<IUsers>().AccessingUser != null)
                    {
                        return Apis.Get<IUsers>().AccessingUser.Id.GetValueOrDefault(0);
                    }

                    return 0;
                }
            }
        }

        private void ProcessSymanticEntry(MicroDataEntry semanticEntry, IElement body)
        {
            foreach ( var entry in body.QuerySelectorAll(semanticEntry.Selector) )
            {
                if ( semanticEntry.Type == MicroDataType.itemprop || semanticEntry.Type == MicroDataType.rel )
                {
                    entry.SetAttribute(semanticEntry.Type.ToString(), semanticEntry.Value);
                }
                else if ( semanticEntry.Type == MicroDataType.itemscope )
                {
                    entry.SetAttribute("itemtype", semanticEntry.Value);
                    entry.SetAttribute("itemscope", string.Empty);
                }
                else
                {
                    entry.SetAttribute(semanticEntry.Value, semanticEntry.Value);
                }
            }
        }
    }
}