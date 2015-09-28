using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using Ninject.Modules;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Entities = Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.ContentDiscuss
{
// ReSharper disable once RedundantExtendsListEntry
    public class ContentDiscussPlugin : IBindingsLoader, IPlugin, ISingletonPlugin, IPluginGroup
    {
        private PluginGroupLoader _pluginGroupLoader;

        #region IBindingsLoader Members

        public void LoadBindings(NinjectModule module)
        {

        }

        public int LoadOrder
        {
            get { return 0; }
        }

        #endregion

        #region IPlugin Members

        public string Name
        {
            get { return "4 Roads - Content Discussions plugin"; }
        }

        public string Description
        {
            get { return "Provides the ability to link discussions to other content in the platform."; }
        }

        public void Initialize()
        {
            PublicApi.ForumThreads.Events.BeforeCreate += ForumThread_BeforeCreate;
            PublicApi.SearchIndexing.Events.BeforeBulkIndex += Search_BeforeBulkIndex;
        }


        private void ForumThread_BeforeCreate(ForumThreadBeforeCreateEventArgs args)
        {
            String originalContentId = System.Web.HttpContext.Current.Request["originalContentId"];
            String originalContentTypeId = System.Web.HttpContext.Current.Request["originalContentTypeId"];
            Guid originalContentGuid;
            Guid originalContentTypeGuid;
            if (!String.IsNullOrEmpty(originalContentId) && !String.IsNullOrEmpty(originalContentTypeId) && Guid.TryParse(originalContentId, out originalContentGuid) && Guid.TryParse(originalContentTypeId, out originalContentTypeGuid))
            {
                if(originalContentTypeGuid == PublicApi.BlogPosts.ContentTypeId)
                {
                    Entities.BlogPost blogPost = PublicApi.BlogPosts.Get(originalContentGuid);
                    // Add the new forum post to the list of discussions started from this blog post
                    Entities.ExtendedAttribute forumPosts = blogPost.ExtendedAttributes.Get("forumDiscussions");
                    if(forumPosts == null)
                    {
                        blogPost.ExtendedAttributes.Add(new Entities.ExtendedAttribute() { Key = "forumDiscussions", Value = args.ContentId.ToString() });
                    }
                    else
                    {
                        forumPosts.Value = new StringBuilder(forumPosts.Value).Append(",").Append(args.ContentId.ToString()).ToString();
                    }
                    PublicApi.BlogPosts.Update(blogPost.Id.Value, new BlogPostsUpdateOptions() { ExtendedAttributes = blogPost.ExtendedAttributes });
                    // Add the blog post as the originator of the forum post
                    args.ExtendedAttributes.Add(new Entities.ExtendedAttribute() { Key = "createdFrom", Value = blogPost.ContentId.ToString() });
                }
                else if(originalContentTypeGuid == PublicApi.WikiPages.ContentTypeId)
                {
                    Entities.WikiPage wikiPage = PublicApi.WikiPages.Get(originalContentGuid);
                    args.ExtendedAttributes.Add(new Entities.ExtendedAttribute() { Key = "createdFrom", Value = wikiPage.ContentId.ToString() });
                }
            }
        }

        private void Search_BeforeBulkIndex(BeforeBulkIndexingEventArgs args)
        {
            if(args.HandlerName == "Forum Thread Content Type")
            {
                foreach (Entities.SearchIndexDocument document in args.Documents)
                {
                    Entities.ForumThread thread = PublicApi.ForumThreads.Get(document.ContentId);
                    if(thread.ExtendedAttributes["createdFrom"] != null && !String.IsNullOrEmpty(thread.ExtendedAttributes["createdFrom"].Value))
                    {
                        document.AddField("createdfrom_t", thread.ExtendedAttributes["createdFrom"].Value);
                    }
                }
            }
        }

        #endregion

        #region IPluginGroup Members

        private class PluginGroupLoaderTypeVisitor : FourRoads.Common.TelligentCommunity.Plugins.Base.PluginGroupLoaderTypeVisitor
        {
            public override Type GetPluginType()
            {
                return typeof(IApplicationPlugin);
            }
        }

        public IEnumerable<Type> Plugins
        {
            get
            {
                if (_pluginGroupLoader == null)
                {
                    Type[] priorityPlugins =
                    {
                        typeof (FactoryDefaultWidgetProviderInstaller)
                    };
                    
                    _pluginGroupLoader = new PluginGroupLoader();

                    _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor(), priorityPlugins);
                }

                return _pluginGroupLoader.GetPlugins();
            }
        }

        #endregion

        public void Update(IPluginConfiguration configuration)
        {
    
        }

    }
}