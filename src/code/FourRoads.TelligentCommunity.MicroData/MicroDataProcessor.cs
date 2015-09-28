using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using CsQuery;
using FourRoads.TelligentCommunity.RenderingHelper;
using Microsoft.SqlServer.Server;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using IContent = Telligent.Evolution.Extensibility.Content.Version1.IContent;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;

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


        public void Process(CQ parsedContent)
        {
            if (_microDataEntries != null)
            {
                try
                {
                    var body = parsedContent.Select("body");
                    Dictionary<Guid, IContent> contentLookup = new Dictionary<Guid, IContent>();

                    foreach (MicroDataEntry semanticEntry in _microDataEntries)
                    {
                        Guid semanticType = semanticEntry.ContentType.GetValueOrDefault(Guid.Empty);

                        if (semanticType == Guid.Empty)
                        {
                            ProcessSymanticEntry(semanticEntry, body);
                        }
                        else if (WebContextualContentTypes.ContainsKey(semanticType))
                        {
                            if (!contentLookup.ContainsKey(semanticType))
                            {
                                contentLookup.Add(semanticType, WebContextualContentTypes[semanticType].GetCurrentContent(new WebContext()));
                            }

                            if (contentLookup[semanticType] != null)
                            {
                                ProcessSymanticEntry(semanticEntry, body);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    new Telligent.Evolution.Components.CSException(Telligent.Evolution.Components.CSExceptionType.UnknownError, "MicroDataPlugin unkonw error", ex).Log();
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
                    if (PublicApi.Users.AccessingUser != null)
                    {
                        return PublicApi.Users.AccessingUser.Id.GetValueOrDefault(0);
                    }

                    return 0;
                }
            }
        }

        private void ProcessSymanticEntry(MicroDataEntry semanticEntry, CQ body)
        {
            if (semanticEntry.Type == MicroDataType.itemprop ||
                semanticEntry.Type == MicroDataType.rel)
            {
                body.Select(semanticEntry.Selector).Attr(semanticEntry.Type.ToString(), semanticEntry.Value);
            }
            else if (semanticEntry.Type == MicroDataType.itemscope)
            {
                body.Select(semanticEntry.Selector).Each(node => node.SetAttribute("itemscope"));
                body.Select(semanticEntry.Selector).Attr("itemtype", semanticEntry.Value);
            }
            else
            {
                body.Select(semanticEntry.Selector).Attr(semanticEntry.Value, semanticEntry.Value);
            }
        }
    }
}