using System;
using System.Collections.Generic;
using FourRoads.Common;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.ForumLastPost.DataProvider;
using FourRoads.TelligentCommunity.ForumLastPost.Interfaces;
using FourRoads.TelligentCommunity.ForumLastPost.Logic;
using FourRoads.TelligentCommunity.ForumLastPost.ScriptedFragmentss;
using Ninject.Modules;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;

namespace FourRoads.TelligentCommunity.ForumLastPost.Extensions
{
    public class LastReadForumPost : SqlScriptsInstaller , IScriptedContentFragmentExtension, IInstallablePlugin, IBindingsLoader, IPluginGroup
    {
        void IPlugin.Initialize()
        {
            PublicApi.ForumReplies.Events.Render += ForumReplyRender;

            Initialize();
        }

        private void ForumReplyRender(ForumReplyRenderEventArgs e)
        {
            User user = PublicApi.Users.AccessingUser;

            if (Services.Get<IPluginManager>().IsEnabled(this))
            {
                if (!user.IsSystemAccount.GetValueOrDefault(false) && user.Id.HasValue)
                {
                    Injector.Get<ILastReadPostLogic>().UpdateLastReadPost(e.Application.ApplicationId, user.Id.Value, e.ThreadId.GetValueOrDefault(0), e.ForumId.GetValueOrDefault(0), e.Id.GetValueOrDefault(0), e.ContentId, e.Date.GetValueOrDefault(DateTime.MinValue));
                }
            }
        }

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

        string IPlugin.Name
        {
            get { return "4 Roads - Last Read Forum Post Plugin"; }
        }

        string IPlugin.Description
        {
            get { return "This plugin allows a user to navigate to the last post that they read in a forum thread"; }
        }

        public int LoadOrder
        {
            get { return 0; }
        }

        public void LoadBindings(NinjectModule module)
        {
            module.Rebind<ILastReadPostDataProvider>().To<LastReadPostDataProvider>().InSingletonScope();
            module.Rebind<ILastReadPostLogic>().To<LastReadPost>().InSingletonScope();
            module.Rebind<ILastReadPostScriptedFragment>().To<LastReadPostScriptedFragment>().InSingletonScope(); 
        }

        public string ExtensionName {
            get { return "frcommon_v1_forumPost"; }
        }

        public object Extension
        {
            get
            {
                return Injector.Get<ILastReadPostScriptedFragment>(); 
            }
        }

        public IEnumerable<Type> Plugins { get { return new[] { typeof(DependencyInjectionPlugin) }; } }
    }
}
