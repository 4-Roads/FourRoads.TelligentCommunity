using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using FourRoads.TelligentCommunity.InlineContent.ScriptedContentFragments;
using FourRoads.TelligentCommunity.InlineContent.Security;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Components.Logic
{
    public class InlineContentLogic
    {
        public const string FILESTORE_KEY = "fourroads.inlinecontent";
        private ICentralizedFileStorageProvider _inlineContentStore = null;
        private XmlSerializer _inlineContentSerializer = new XmlSerializer(typeof(InlineContentData));
        private static Regex _makeSafeFileNameRegEx = new Regex(CentralizedFileStorage.ValidFileNameRegexPattern, RegexOptions.Compiled);

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
                                if (PublicApi.Permissions.Get(PermissionRegistrar.EditInlineContentPermission, PublicApi.Users.AccessingUser.Id.Value, context.ContentId.Value, context.ContainerTypeId.Value).IsAllowed)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        return PublicApi.Permissions.Get(PermissionRegistrar.EditInlineContentPermission, PublicApi.Users.AccessingUser.Id.GetValueOrDefault(0)).IsAllowed;
                    }

                }

                    return false;
            }
        }

        public ICentralizedFileStorageProvider InlineContentStore
        {
            get
            {
                if (_inlineContentStore == null)
                {
                    _inlineContentStore = CentralizedFileStorage.GetFileStore(FILESTORE_KEY);
                }
                return _inlineContentStore;
            }
        }


        public InlineContentData GetInlineContent(string contentName)
        {
            contentName = MakeSafeFileName(contentName);

            string cacheKey = GetCacheKey(contentName);
            InlineContentData result = CacheService.Get(cacheKey, CacheScope.All) as InlineContentData;

            if (result == null && InlineContentStore != null)
            {
                ICentralizedFile file = InlineContentStore.GetFile("", contentName + ".xml");

                if (file != null)
                {
                    using (Stream stream = file.OpenReadStream())
                    {
                        result = ((InlineContentData)_inlineContentSerializer.Deserialize(stream));

                        CacheService.Put(cacheKey, result , CacheScope.All);
                    }
                }
            }
            return result;
        }

        public void UpdateInlineContent(string contentName, string content, string anonymousContent)
        {
            if (CanEdit)
            {
                contentName = MakeSafeFileName(contentName);

                string cacheKey = GetCacheKey(contentName);

                using (MemoryStream buffer = new MemoryStream(10000))
                {
                    //Translate the URL's if any have been uploaded
                    var pluginMgr = PluginManager.GetSingleton<InlineContentPart>();

                    content = pluginMgr.UpdateInlineContentFiles(content);
                    anonymousContent = pluginMgr.UpdateInlineContentFiles(anonymousContent);

                    _inlineContentSerializer.Serialize(buffer, new InlineContentData() {Content = content, AnonymousContent = anonymousContent});

                    buffer.Seek(0, SeekOrigin.Begin);

                    InlineContentStore.AddFile("", contentName + ".xml", buffer, false);
                }

                CacheService.Remove(cacheKey, CacheScope.All);
            }
        }

        private static string GetCacheKey(string contentName)
        {
            return "4-roads-inline-content:" + contentName;
        }

        private static string MakeSafeFileName(string name)
        {
            string result = string.Empty;

            foreach (Match match in _makeSafeFileNameRegEx.Matches(name))
            {
                result += match.Value;
            }

            return result;
        }

        [Serializable]
        public class InlineContentData
        {
            public string Content;
            public string AnonymousContent;
        }
    }
}
