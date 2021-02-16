using FourRoads.TelligentCommunity.Installer.Components.Utility;
using System;
using System.Collections.Generic;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.ForumLastPost.DataProvider;
using FourRoads.TelligentCommunity.ForumLastPost.Interfaces;
using FourRoads.TelligentCommunity.ForumLastPost.Logic;
using FourRoads.TelligentCommunity.ForumLastPost.ScriptedFragmentss;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;

namespace FourRoads.TelligentCommunity.ForumLastPost.Extensions
{
    public class LastReadForumSqlInstaller : Installer.SqlScriptsInstaller
    {
        protected override string ProjectName
        {
            get { return "Last Read Forum Post"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.ForumLastPost.Resources."; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return new EmbeddedResources(); }
        }
    }

    public class LastReadForumPostContentFragmentProvider : IScriptedContentFragmentExtension
    {
        public void Initialize()
        {
        }

        public string Name
        {
            get { return "4 Roads - Last Read Forum Post Plugin Scripted Fragment Extension"; }
        }

        public string Description
        {
            get { return "This plugin allows a user to navigate to the last post that they read in a forum thread"; }
        }


        public string ExtensionName
        {
            get { return "frcommon_v1_forumPost"; }
        }

        public object Extension
        {
            get
            {
                return Injector.Get<ILastReadPostScriptedFragment>();
            }
        }

    }

    public class LastReadForumPost :   IBindingsLoader, IPluginGroup
    {
        public void Initialize()
        {
            Apis.Get<IForumReplies>().Events.Render += ForumReplyRender;
        }

        private void ForumReplyRender(ForumReplyRenderEventArgs e)
        {
            User user = Apis.Get<IUsers>().AccessingUser;

            if (Services.Get<IPluginManager>().IsEnabled(this))
            {
                if (!user.IsSystemAccount.GetValueOrDefault(false) && user.Id.HasValue && string.Compare(e.RenderTarget,"web", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Injector.Get<ILastReadPostLogic>().UpdateLastReadPost(e.Application.ApplicationId, user.Id.Value, e.ThreadId.GetValueOrDefault(0), e.ForumId.GetValueOrDefault(0), e.Id.GetValueOrDefault(0), e.ContentId, e.Date.GetValueOrDefault(DateTime.MinValue));
                }
            }
        }

        public string Name
        {
            get { return "4 Roads - Last Read Forum Post Plugin"; }
        }

        public string Description
        {
            get { return "This plugin allows a user to navigate to the last post that they read in a forum thread"; }
        }

        public int LoadOrder
        {
            get { return 0; }
        }

        public void LoadBindings(IContainer module)
        {
            module.Register<ILastReadPostDataProvider, LastReadPostDataProvider>(Reuse.Singleton);
            module.Register<ILastReadPostLogic, LastReadPost>(Reuse.Singleton);
            module.Register<ILastReadPostScriptedFragment, LastReadPostScriptedFragment>(Reuse.Singleton);
        }

        public IEnumerable<Type> Plugins { get { return new[] { typeof(DependencyInjectionPlugin), typeof(LastReadForumSqlInstaller) , typeof(LastReadForumPostContentFragmentProvider) }; } }
    }
}
