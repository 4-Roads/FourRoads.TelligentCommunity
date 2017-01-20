using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using CsQuery;
using FourRoads.Common;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using FourRoads.TelligentCommunity.MetaData.Security;
using FourRoads.TelligentCommunity.RenderingHelper;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MetaData.Logic
{
    public class MetaDataLogic : ICQProcessor, IMetaDataLogic
    {
        private static string _imageRegEx = @"<img[^>]*src=(?:(""|')(?<url>[^\1]*?)\1|(?<url>[^\s|""|'|>]+))";
        private static Regex _regex = new Regex(_imageRegEx, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public const string FILESTORE_KEY = "fourroads.metadata";
        private ICentralizedFileStorageProvider _metaDataStore = null;
        private readonly XmlSerializer _metaDataSerializer = new XmlSerializer(typeof(MetaData));
        private static readonly Regex MakeSafeFileNameRegEx = new Regex(CentralizedFileStorage.ValidFileNameRegexPattern, RegexOptions.Compiled);
        private MetaDataConfiguration _metaConfig;

        public void Process(CQ parsedContent)
        {
            if(!String.IsNullOrEmpty(_metaConfig.GoogleTagHead) && !String.IsNullOrEmpty(_metaConfig.GoogleTagBody))
            {
                CQ head = parsedContent["head"];
                CQ fragment = CQ.CreateFragment(_metaConfig.GoogleTagHead);
                CQ firstChild = head.Find(":first");
                fragment.InsertBefore(firstChild);

                CQ body = parsedContent.Select("body");
                firstChild = body.Find(":first");
                fragment = CQ.CreateFragment(_metaConfig.GoogleTagBody);
                fragment.InsertBefore(firstChild);
            }
        }

        public  bool CanEdit
        {
            get
            {
                if (PublicApi.Url.CurrentContext != null && PublicApi.Url.CurrentContext.ContextItems != null && PublicApi.Users.AccessingUser != null)
                {
                    var items  = PublicApi.Url.CurrentContext.ContextItems.GetAllContextItems();

                    if (items.Any())
                    {
                        foreach (var context in items)
                        {
                            if (context.ContentId.HasValue && context.ContainerTypeId.HasValue && PublicApi.Users.AccessingUser.Id.HasValue)
                            {
                                if (PublicApi.Permissions.Get(PermissionRegistrar.EditMetaDataPermission, PublicApi.Users.AccessingUser.Id.Value, context.ContentId.Value, context.ContainerTypeId.Value).IsAllowed)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        return PublicApi.Permissions.Get(PermissionRegistrar.EditMetaDataPermission, PublicApi.Users.AccessingUser.Id.GetValueOrDefault(0)).IsAllowed;
                    }
                }

                return false;
            }
        }

        private static Regex matchFields = new Regex(@"({(?!{)\w+}(?!}))", RegexOptions.Compiled);

        public string FormatMetaString(string rawFieldValue, string seperator, IDictionary namedParameters)
        {
            return matchFields.Replace(rawFieldValue , match =>
            {
                string trimmend = match.Value.Trim(new char[]{' ' , '}', '{' });

                if (namedParameters.Contains(trimmend))
                {
                    string repsonse = string.Empty;

                    if (match.Index > 0 && string.IsNullOrEmpty((string)namedParameters[trimmend]))
                        repsonse += seperator;

                    return repsonse + namedParameters[trimmend];
                }

                return match.Value;
            });
        }

        public string GetBestImageUrlForContent(Guid contentId, Guid contentTypeId)
        {
            ContentDetails details = GetContentDetails(contentId, contentTypeId);

            return GetBestImageUrlForContentDetails(details);
        }

        public string GetBestImageUrlForCurrent()
        {
            ContentDetails details = GetCurrentContentDetails();

            return GetBestImageUrlForContentDetails(details);
        }

        private string GetBestImageUrlForContentDetails(ContentDetails details)
        {
            string imageUrl = string.Empty;

            if (details.ContentId.HasValue && details.ContentTypeId.HasValue)
            {
                //Look for first image in content
                Content content = PublicApi.Content.Get(details.ContentId.Value, details.ContentTypeId.Value);

                if (!content.HasErrors())
                {
                    imageUrl = ExtractImage(content.HtmlDescription(""));
                }
            }

            if (details.ApplicationId.HasValue && string.IsNullOrWhiteSpace(imageUrl))
            {
                //Look for first image in application description
                var app = PublicApi.Applications.Get(details.ApplicationId.Value, details.ApplicationTypeId.Value);

                if (!app.HasErrors())
                {
                    imageUrl = ExtractImage(app.HtmlDescription(""));
                }
            }

            if (details.ContainerId.HasValue && details.ContainerTypeId.HasValue && string.IsNullOrWhiteSpace(imageUrl))
            {
                //Get image from group avatar
                //Look for first image in application description
                var container = PublicApi.Containers.Get(details.ContainerId.Value, details.ContainerTypeId.Value);

                if (!container.HasErrors())
                {
                    imageUrl = ExtractImage(container.AvatarUrl);
                }

            }

            return imageUrl;
        }

        private string ExtractImage(string content)
        {
            List<string> results = new List<string>();
            if (content != null)
            {
                Regex regex = _regex;

                MatchCollection matches = regex.Matches(content);

                foreach (Match match in matches)
                {
                    Uri result;
                    if (Uri.TryCreate(PublicApi.Url.Absolute(match.Groups["url"].Value), UriKind.Absolute, out result))
                    {
                        results.Add(result.AbsoluteUri);
                    }
                }
            }

            return results.FirstOrDefault();
        }

        public ICentralizedFileStorageProvider MetaDataStore
        {
            get
            {
                if (_metaDataStore == null)
                {
                    _metaDataStore = CentralizedFileStorage.GetFileStore(FILESTORE_KEY);
                }
                return _metaDataStore;
            }
        }

        private class ContentDetails
        {
            public Guid? ContainerId;
            public Guid? ApplicationId;

            public string FileName
            {
                get
                {
                    return MakeSafeFileName(PageName + (ContainerId.GetValueOrDefault(Guid.Empty) != Guid.Empty ? "_" + ContainerId : string.Empty) +
                           (ApplicationId.GetValueOrDefault(Guid.Empty) != Guid.Empty ? "_" + ApplicationId : string.Empty));

                }
            }

            public string PageName;
            public Guid? ContainerTypeId { get; set; }
            public Guid? ContentId { get; set; }
            public Guid? ContentTypeId { get; set; }
            public Guid? ApplicationTypeId { get; set; }
        }

        private ContentDetails GetCurrentContentDetails()
        {
            ContentDetails details = (ContentDetails)CacheService.Get("ContentDetails", CacheScope.Context) ?? new ContentDetails() {PageName = "default"};

            if (PublicApi.Url.CurrentContext != null && details.PageName == "default")
            {
                details.PageName = PublicApi.Url.CurrentContext.PageName;

                ContextualItem(coa =>
                {
                    details.ContentId = coa.ContentId;
                    details.ContentTypeId = coa.ContentTypeId;
                },a =>
                {
                    details.ApplicationId = a.ApplicationId;
                    details.ApplicationTypeId = a.ApplicationTypeId;
                }, c =>
                {
                    details.ContainerId = c.ContainerId;
                    details.ContainerTypeId = c.ContainerTypeId;
                });
            }

            CacheService.Put("ContentDetails", details , CacheScope.Context);
            
            return details;
        }

        private ContentDetails GetContentDetails(Guid contentId, Guid contentTypeId)
        {
            string cacheKey = string.Format("ContentDetails::{0}::{1}", contentId, contentTypeId);
            ContentDetails details = CacheService.Get(cacheKey, CacheScope.Distributed) as ContentDetails;

            if (details == null)
            {
                details = new ContentDetails() { PageName = "default" };

                var content = PublicApi.Content.Get(contentId, contentTypeId);

                if (content != null)
                {
                    details.ContentId = content.ContentId;
                    details.ContentTypeId = content.ContentTypeId;
                    details.ApplicationId = content.Application.ApplicationId;
                    details.ApplicationTypeId = content.Application.ApplicationTypeId;
                    details.ContainerId = content.Application.Container.ContainerId;
                    details.ContainerTypeId = content.Application.Container.ContainerTypeId;
                }
            }

            CacheService.Put(cacheKey, details, CacheScope.Distributed, TimeSpan.FromMinutes(5));

            return details;
        }

        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA1.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
     
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        protected void ContextualItem(Action<IContent> contentUse, Action<IApplication> applicationUse, Action<IContainer> containerUse)
        {
            IApplication currentApplication = null;
            IContainer currentContainer = null;
            IContent currentContent = null;

            foreach (var contextItem in PublicApi.Url.CurrentContext.ContextItems.GetAllContextItems())
            {
                var container = PluginManager.Get<IWebContextualContainerType>().FirstOrDefault(a => a.ContainerTypeId == contextItem.ContainerTypeId);

                if (container != null && contextItem.ContainerId.HasValue && contextItem.ContainerTypeId == PublicApi.Groups.ContainerTypeId)
                {
                    currentContainer = container.Get(contextItem.ContainerId.Value);
                }
                else
                {   //Only interested in context in groups
                    break;
                }

                var app = PluginManager.Get<IWebContextualApplicationType>().FirstOrDefault(a => a.ApplicationTypeId == contextItem.ApplicationTypeId && a.ContainerTypes.Any(ct => ct == contextItem.ContainerTypeId));

                if (app != null && contextItem.ApplicationId.HasValue)
                {
                    IApplication application = app.Get(contextItem.ApplicationId.Value);

                    if (application != null)
                    {
                        if (application.Container.ContainerId != application.ApplicationId)
                            currentApplication = application;
                    }
                }

                var content = PluginManager.Get<IContentType>().FirstOrDefault(a => a.ContentTypeId == contextItem.ContentTypeId && a.ApplicationTypes.Any(at => at == contextItem.ApplicationTypeId));

                if (content != null && contextItem.ContentId.HasValue && contextItem.ContentTypeId.HasValue)
                {
                    currentContent = content.Get(contextItem.ContentId.Value);
                }
            }

            if (currentApplication != null)
                applicationUse(currentApplication);
            
            if (currentContent != null)
                contentUse(currentContent);
            
            if (currentContainer != null)
                containerUse(currentContainer);
        }

        private MetaData GetCurrentMetaData(ContentDetails details)
        {
            string cacheKey = GetCacheKey(details.FileName);

            MetaData result = CacheService.Get(cacheKey, CacheScope.All) as MetaData;

            if (result == null && MetaDataStore != null)
            {
                ICentralizedFile file = MetaDataStore.GetFile("", details.FileName + ".xml");

                if (file != null)
                {
                    using (Stream stream = file.OpenReadStream())
                    {
                        result = ((MetaData)_metaDataSerializer.Deserialize(stream));

                        //FIlter out any tags that have oreviously been configured but then removed
                        var lookup = _metaConfig.ExtendedEntries.ToLookup(f => f);

                        foreach (var tag in result.ExtendedMetaTags.Keys)
                        {
                            if (!lookup.Contains(tag))
                                result.ExtendedMetaTags.Remove(tag);
                        }
                    }
                }

                if (result == null)
                {
                    result = new MetaData() { InheritData = true, ContainerId = details.ContainerId.GetValueOrDefault(Guid.Empty), ContainerTypeId = details.ContainerTypeId.GetValueOrDefault(Guid.Empty) };
                }

                CacheService.Put(cacheKey, result, CacheScope.All);
            }

            return result;
        }


        public MetaData GetCurrentMetaData()
        {
            ContentDetails details = GetCurrentContentDetails();

            MetaData data = GetCurrentMetaData(details);

            while (data.InheritData && data.ContainerId != Guid.Empty)
            {
                var container = PublicApi.Containers.Get(data.ContainerId, data.ContainerTypeId);

                //Once we start inheriting we go to the group home page
                if (!container.HasErrors())
                {
                    if (container.ContainerId == PublicApi.Groups.Root.ContainerId)
                        break;

                    var context = PublicApi.Url.ParsePageContext(container.Url);

                    if (context != null && container.ParentContainer != null)
                    {
                        details.ApplicationId = Guid.Empty;
                        details.ApplicationTypeId = Guid.Empty; 
                        details.ContainerId = container.ParentContainer.ContainerId;
                        details.ContainerTypeId = container.ParentContainer.ContainerTypeId;
                        details.PageName = context.PageName;

                        data = GetCurrentMetaData(details);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return data;
        }

        private static string GetCacheKey(string contentName)
        {
            return "4-roads-metadata:" + contentName;
        }

        private static string MakeSafeFileName(string name)
        {
            string result = string.Empty;

            foreach (Match match in MakeSafeFileNameRegEx.Matches(name))
            {
                result += match.Value;
            }

            return result;
        }

        public void UpdateConfiguration(MetaDataConfiguration metaConfig)
        {
            _metaConfig = metaConfig;
        }

        public string[] GetAvailableExtendedMetaTags()
        {
            return _metaConfig.ExtendedEntries.ToArray();
        } 

        public void SaveMetaDataConfiguration(string title, string description, string keywords, bool inherit, IDictionary extendedTags)
        {
            if (CanEdit)
            {
                ContentDetails details = GetCurrentContentDetails();

                string cacheKey = GetCacheKey(details.FileName);

                using (MemoryStream buffer = new MemoryStream(10000))
                {
                    var data = new MetaData()
                    {
                        InheritData = inherit,
                        ApplicationId = details.ApplicationId.GetValueOrDefault(Guid.Empty),
                        ContainerId = details.ContainerId.GetValueOrDefault(Guid.Empty),
                        ContainerTypeId = details.ContainerTypeId.GetValueOrDefault(Guid.Empty),
                        Title = title,
                        Description = description,
                        Keywords = keywords,
                        ExtendedMetaTags = new SerializableDictionary<string, string>(extendedTags.Keys.Cast<string>()
                            .ToDictionary(name => name, name => extendedTags[name] as string))
                    };

                    //Translate the URL's if any have been uploaded
                    _metaDataSerializer.Serialize(buffer, data);

                    buffer.Seek(0, SeekOrigin.Begin);

                    MetaDataStore.AddFile("", details.FileName + ".xml", buffer, false);
                }

                CacheService.Remove(cacheKey, CacheScope.All);
            }
        }

        public string GetDynamicFormXml()
        {
            ContentDetails details = GetCurrentContentDetails();

            MetaData metaData = GetCurrentMetaData(details);

            PropertyGroup group = new PropertyGroup("Meta", "Meta Options", 0);

            PropertySubGroup subGroup = new PropertySubGroup("Options", "Main Options", 0)
            {
            };

            group.PropertySubGroups.Add(subGroup);

            int order = 0;
            subGroup.Properties.Add(new Property("Inherit", "Inherit From Parent", PropertyType.Bool, order++, metaData.InheritData.ToString()));
            subGroup.Properties.Add(new Property("Title", "Title", PropertyType.String, order++, metaData.Title));
            subGroup.Properties.Add(new Property("Description", "Description", PropertyType.String, order++, metaData.Description));
            subGroup.Properties.Add(new Property("Keywords", "Keywords", PropertyType.String, order++, metaData.Keywords));

            PropertySubGroup extendedGroup = new PropertySubGroup("Options", "Extended Tags", 1);

            group.PropertySubGroups.Add(extendedGroup);

            foreach (string extendedEntry in _metaConfig.ExtendedEntries)
            {
                extendedGroup.Properties.Add(new Property(extendedEntry, extendedEntry, PropertyType.String, order++, metaData.ExtendedMetaTags.ContainsKey(extendedEntry) ? metaData.ExtendedMetaTags[extendedEntry] : string.Empty));
            }

            StringBuilder sb = new StringBuilder();

            using(StringWriter sw = new StringWriter(sb))
            using (XmlTextWriter tw = new XmlTextWriter(sw))
                group.Serialize(tw);

            return sb.ToString();
        }
    }
}
