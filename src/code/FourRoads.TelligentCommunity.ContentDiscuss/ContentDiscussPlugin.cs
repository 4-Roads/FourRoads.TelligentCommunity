using System;
using System.Collections.Generic;
using System.Text;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Entities = Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.ContentDiscuss
{
// ReSharper disable once RedundantExtendsListEntry
    public class ContentDiscussPlugin : IBindingsLoader, IPlugin, ISingletonPlugin, IPluginGroup
    {
        private PluginGroupLoader _pluginGroupLoader;

        #region IBindingsLoader Members

        public void LoadBindings(IContainer module)
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
            Apis.Get<IForumThreads>().Events.BeforeCreate += ForumThread_BeforeCreate;
            Apis.Get<ISearchIndexing>().Events.BeforeBulkIndex += Search_BeforeBulkIndex;
        }


        private void ForumThread_BeforeCreate(ForumThreadBeforeCreateEventArgs args)
        {
            String originalContentId = System.Web.HttpContext.Current.Request["originalContentId"];
            String originalContentTypeId = System.Web.HttpContext.Current.Request["originalContentTypeId"];
            Guid originalContentGuid;
            Guid originalContentTypeGuid;
            if (!String.IsNullOrEmpty(originalContentId) && !String.IsNullOrEmpty(originalContentTypeId) && Guid.TryParse(originalContentId, out originalContentGuid) && Guid.TryParse(originalContentTypeId, out originalContentTypeGuid))
            {
                if(originalContentTypeGuid == Apis.Get<IBlogPosts>().ContentTypeId)
                {
                    Entities.BlogPost blogPost = Apis.Get<IBlogPosts>().Get(originalContentGuid);
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
                    Apis.Get<IBlogPosts>().Update(blogPost.Id.Value, new BlogPostsUpdateOptions() { ExtendedAttributes = blogPost.ExtendedAttributes });
                    // Add the blog post as the originator of the forum post
                    args.ExtendedAttributes.Add(new Entities.ExtendedAttribute() { Key = "createdFrom", Value = blogPost.ContentId.ToString() });
                }
                else if(originalContentTypeGuid == Apis.Get<IWikiPages>().ContentTypeId)
                {
                    Entities.WikiPage wikiPage = Apis.Get<IWikiPages>().Get(originalContentGuid);
                    args.ExtendedAttributes.Add(new Entities.ExtendedAttribute() { Key = "createdFrom", Value = wikiPage.ContentId.ToString() });
                }
            }
        }

        private void Search_BeforeBulkIndex(BeforeBulkIndexingEventArgs args)
        {
            // --todo look at this re string below .....
            var threadService = Apis.Get<IForumThreads>();

            foreach (var doc in args.Documents)
            {
                // Select documents that are forum threads
                if (doc.ContentTypeId == threadService.ContentTypeId)
                {
                    var thread = threadService.Get(doc.ContentId);
                    if (thread == null)
                        continue;

                    if (thread.ExtendedAttributes["createdFrom"] != null && !String.IsNullOrEmpty(thread.ExtendedAttributes["createdFrom"].Value))
                    {
                        doc.AddField("createdfrom_t", thread.ExtendedAttributes["createdFrom"].Value);
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