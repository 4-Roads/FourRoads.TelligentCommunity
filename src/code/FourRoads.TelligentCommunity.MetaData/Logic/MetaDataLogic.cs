using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using FourRoads.Common;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using FourRoads.TelligentCommunity.MetaData.Security;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MetaData.Logic
{
    public class MetaDataLogic : IMetaDataLogic
    {
        public const string FILESTORE_KEY = "fourroads.metadata";
        private ICentralizedFileStorageProvider _metaDataStore = null;
        private readonly XmlSerializer _metaDataSerializer = new XmlSerializer(typeof(MetaData));
        private static readonly Regex MakeSafeFileNameRegEx = new Regex(CentralizedFileStorage.ValidFileNameRegexPattern, RegexOptions.Compiled);
        private MetaDataConfiguration _metaConfig;

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

        public string GetCurrentContentName()
        {
            string name = "default";

            if (PublicApi.Url.CurrentContext != null)
            {
                name = PublicApi.Url.CurrentContext.PageName;
         
                ContextualItem(a =>
                {
                    name += a.ApplicationId.ToString();

                }, c =>
                {
                    name += c.ContainerId.ToString();
                }, ta =>
                {
                    name += GetHashString(string.Join("", ta.OrderBy(s => s)));
                });
            }

            return name;
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

        protected void ContextualItem(Action<IApplication> applicationUse, Action<IContainer> containerUse, Action<string[]> tagsUse)
        {
            IApplication currentApplication = null;
            IContainer currentContainer = null;
            string[] tags = null;

            foreach (var contextItem in PublicApi.Url.CurrentContext.ContextItems.GetAllContextItems())
            {
                var app = PluginManager.Get<IApplicationType>().FirstOrDefault(a => a.ApplicationTypeId == contextItem.ApplicationTypeId);

                if (app != null && contextItem.ApplicationId.HasValue)
                {
                    IApplication application = app.Get(contextItem.ApplicationId.Value);

                    if (application != null)
                    {
                        if (application.Container.ContainerId != application.ApplicationId)
                            currentApplication = application;
                    }
                }

                var container = PluginManager.Get<IContainerType>().FirstOrDefault(a => a.ContainerTypeId == contextItem.ContainerTypeId);

                if (container != null && contextItem.ContainerId.HasValue && contextItem.ContainerTypeId == PublicApi.Groups.ContainerTypeId)
                {
                    currentContainer = container.Get(contextItem.ContainerId.Value);
                }

                if (contextItem.TypeName == "Tags")
                {
                    tags = contextItem.Id.Split(new[] { '/' });
                }
            }

            if (currentApplication != null)
                applicationUse(currentApplication);
            else if (currentContainer != null)
                containerUse(currentContainer);

            if (tags != null)
                tagsUse(tags);
        }


        public MetaData GetCurrentMetaData()
        {
            string contentName = GetCurrentContentName();

            contentName = MakeSafeFileName(contentName);

           // PublicApi.Eventlogs.Write(contentName, new EventLogEntryWriteOptions() {Category = "MetaData"});
           
            string cacheKey = GetCacheKey(contentName);
            MetaData result = CacheService.Get(cacheKey, CacheScope.All) as MetaData;

            if (result == null && MetaDataStore != null)
            {
                //PublicApi.Eventlogs.Write("try get file:" + contentName, new EventLogEntryWriteOptions() { Category = "MetaData" });

                ICentralizedFile file = MetaDataStore.GetFile("", contentName + ".xml");

                if (file != null)
                { 
                   // PublicApi.Eventlogs.Write("opened file ", new EventLogEntryWriteOptions() { Category = "MetaData" });

                   // PublicApi.Eventlogs.Write("opened file ", new EventLogEntryWriteOptions() { Category = "MetaData" });

                    using (Stream stream = file.OpenReadStream())
                    {
                        result = ((MetaData) _metaDataSerializer.Deserialize(stream));

                        //PublicApi.Eventlogs.Write("Deserialized", new EventLogEntryWriteOptions() { Category = "MetaData" });

                        //FIlter out any tags that have oreviously been configured but then removed
                        var lookup = _metaConfig.ExtendedEntries.ToLookup(f => f);

                        foreach (var tag in result.ExtendedMetaTags.Keys)
                        {
                            if (!lookup.Contains(tag))
                                result.ExtendedMetaTags.Remove(tag);
                        }
                    }
                }

                CacheService.Put(cacheKey, result, CacheScope.All);
            }

            //PublicApi.Eventlogs.Write("Returned " + result.Title, new EventLogEntryWriteOptions() { Category = "MetaData" });

            return result;
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

        public void SaveMetaDataConfiguration(string title, string description, string keywords, IDictionary extendedTags)
        {
            if (CanEdit)
            {
                string contentName = GetCurrentContentName();

                contentName = MakeSafeFileName(contentName);

                string cacheKey = GetCacheKey(contentName);

                using (MemoryStream buffer = new MemoryStream(10000))
                {
                    var data = new MetaData()
                    {
                        Title = title,
                        Description = description,
                        Keywords = keywords,
                        ExtendedMetaTags = new SerializableDictionary<string, string>(extendedTags.Keys.Cast<string>()
                            .ToDictionary(name => name, name => extendedTags[name] as string))
                    };

                    //Translate the URL's if any have been uploaded
                    _metaDataSerializer.Serialize(buffer, data);

                    buffer.Seek(0, SeekOrigin.Begin);

                    MetaDataStore.AddFile("", contentName + ".xml", buffer, false);
                }

                CacheService.Remove(cacheKey, CacheScope.All);
            }
        }

        public string GetDynamicFormXml()
        {
            MetaData metaData = GetCurrentMetaData() ?? new MetaData();

            PropertyGroup group = new PropertyGroup("Options" , "Options" , 0);

            int order = 0;
            group.Properties.Add(new Property("Title", "Title", PropertyType.String, order++, metaData.Title));
            group.Properties.Add(new Property("Description", "Description", PropertyType.String, order++, metaData.Description));
            group.Properties.Add(new Property("Keywords", "Keywords", PropertyType.String, order++, metaData.Keywords));

            foreach (string extendedEntry in _metaConfig.ExtendedEntries)
            {
                group.Properties.Add(new Property(extendedEntry, extendedEntry, PropertyType.String, order++, metaData.ExtendedMetaTags.ContainsKey(extendedEntry) ?  metaData.ExtendedMetaTags[extendedEntry] : string.Empty));
            }

            StringBuilder sb = new StringBuilder();

            using(StringWriter sw = new StringWriter(sb))
            using (XmlTextWriter tw = new XmlTextWriter(sw))
                group.Serialize(tw);

            return sb.ToString();
        }
    }
}
