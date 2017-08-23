using System;
using System.Collections.Generic;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Rules.ThreadViews.DataProvider;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Events;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Interfaces;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Jobs;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Services;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Forums.Api.Implementation;

namespace FourRoads.TelligentCommunity.Rules.ThreadViews.Triggers
{
    public class ThreadView : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin, IBindingsLoader, IConfigurablePlugin , IPluginGroup
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private Guid _triggerid = new Guid("{7BC8CCEC-2A48-4B10-A381-33C8295D8E6C}");
        private Guid _forumThreadContentType;
        private IThreadViewService _forumThreadViewService;

        private int _threshold;

        public void Initialize()
        {
            Injector.Get<IContentPresence>().Events.AfterCreate += EventsOnAfterCreate;
            Injector.Get<IThreadViewService>().Events.EventAfterView += EventsAfterView;   

            _forumThreadContentType = Injector.Get<IForumThreads>().ContentTypeId;
            _forumThreadViewService = Injector.Get<IThreadViewService>();
        }

        /// <summary>
        /// Check on the action performed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnAfterCreate(object sender, ContentPresenceAfterCreateEventArgs args)
        {
            try
            {
                if (args.ContentId != null && args.ContentTypeId.Equals(_forumThreadContentType))
                {
                    // go and save it 
                    _forumThreadViewService.Create((Guid)args.ContentId, (DateTime)args.CreatedDate);
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterViewCreate failed for id :{0}", args.ContentId),
                    ex).Log();
            }
        }

        /// <summary>
        ///  Fired from _forumThreadViewService for any threads which have been viewed 
        /// </summary>
        /// <param name="args"></param>
        private void EventsAfterView(ThreadViewEventsArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                    {
                        {
                            "ThreadId", args.ForumThreadId.ToString()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                new TCException(string.Format("EventsAfterView failed for thread id:{0}", args.ForumThreadId), ex).Log();
            }
        }

        /// <summary>
        /// Fired from ThreadViewJob to check every x duration if we have had thread views
        /// </summary>
        public void CheckViewsforTrigger()
        {
            _forumThreadViewService.CheckforViewTriggers(_threshold);
        }

        public string Name
        {
            get { return "4 Roads - Achievements - Monitor forum thread views"; }
        }

        public string Description
        {
            get { return "Fires when a users view forum threads"; }
        }

        public void SetController(IRuleController controller)
        {
            _ruleController = controller;
        }

        public RuleTriggerExecutionContext GetExecutionContext(RuleTriggerData data)
        {
            RuleTriggerExecutionContext context = new RuleTriggerExecutionContext();
            if (data.ContainsKey("ThreadId"))
            {
                Guid threadId;
               
                if (Guid.TryParse(data["ThreadId"], out threadId))
                {
                    var content = Apis.Get<IForumThreads>();
                    var threadContent = content.Get(threadId);

                    if (!threadContent.HasErrors())
                    {
                        context.Add(content.ContentTypeId, threadContent);
                        context.Add(_triggerid, true); //Added this trigger so that it is not re-entrant
                    }
                }
            }
            return context;
        }

        public Guid RuleTriggerId
        {
            get { return _triggerid; }
        }

        public string RuleTriggerName
        {
            get { return _translationController.GetLanguageResourceValue("RuleTriggerName"); }
        }

        public string RuleTriggerCategory
        {
            get { return _translationController.GetLanguageResourceValue("RuleTriggerCategory"); }
        }

        public IEnumerable<Guid> ContextualDataTypeIds
        {
            get { return new[] { Apis.Get<IForumThreads>().ContentTypeId }; }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translationController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation[] defaultTranslation = new[] { new Translation("en-us") };

                defaultTranslation[0].Set("RuleTriggerName", "a user viewed a forum thread");
                defaultTranslation[0].Set("RuleTriggerCategory", "Achievements");

                return defaultTranslation;
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            _threshold = configuration.GetInt("Threshold");
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup group = new PropertyGroup("settings", "settings", 0);
                group.Properties.Add(new Property("Threshold", "Purge Threshold (mins)", PropertyType.Int, 2, "480"));

                return new[] { group };
            }
        }

        public string[] Categories
        {
            get
            {
                return new[]
                {
                    "Rules"
                };
            }
        }

        public IEnumerable<Type> Plugins => new Type[] { typeof(ThreadViewJob) , typeof(Resources.SqlScriptsInstaller) };

        public int LoadOrder
        {
            get { return 0; }
        }

        public void LoadBindings(IContainer module)
        {
            module.Register<IThreadViewService, ThreadViewService>(Reuse.Singleton);
            module.Register<IThreadViewDataProvider, ThreadViewDataProvider>(Reuse.Singleton);
            module.Register<IThreadViewEvents , ThreadViewEvents>(Reuse.Singleton);
        }
    }
}
