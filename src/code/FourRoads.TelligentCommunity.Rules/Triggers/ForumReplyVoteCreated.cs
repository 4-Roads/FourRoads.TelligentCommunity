using System;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class ForumReplyVoteCreated : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private Guid _triggerid = new Guid("{8D90F6A7-D4DB-4C6F-8634-CDE6A9D165A2}");

        public void Initialize()
        {
            Apis.Get<IForumReplyVotes>().Events.AfterCreate += EventsOnAfterCreate;
            Apis.Get<IForumReplyVotes>().Events.AfterDelete += EventsOnAfterDelete;
            Apis.Get<IForumReplyVotes>().Events.AfterUpdate += EventsOnAfterUpdate;
        }

        private void EventsOnAfterCreate(ForumReplyVoteAfterCreateEventArgs forumReplyVoteAfterCreateEventArgs)
        {
            try
            {
                int userId = forumReplyVoteAfterCreateEventArgs.UserId;

                if (_ruleController != null)
                {
                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                    {
                        {
                            "UserId", userId.ToString()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                new TCException(string.Format("EventsOnAfterCreate failed for userid:{0}", forumReplyVoteAfterCreateEventArgs.UserId), ex).Log();
            }
        }


        private void EventsOnAfterDelete(ForumReplyVoteAfterDeleteEventArgs forumReplyVoteAfterDeleteEventArgs)
        {
            try
            {
                int userId = forumReplyVoteAfterDeleteEventArgs.UserId;

                if (_ruleController != null)
                {
                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                    {
                        {
                            "UserId", userId.ToString()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                new TCException(string.Format("EventsOnAfterDelete failed for userid:{0}", forumReplyVoteAfterDeleteEventArgs.UserId), ex).Log();
            }
        }

        private void EventsOnAfterUpdate(ForumReplyVoteAfterUpdateEventArgs forumReplyVoteAfterUpdateEventArgs)
        {
            try
            {
                int userId = forumReplyVoteAfterUpdateEventArgs.UserId;

                if (_ruleController != null)
                {
                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                    {
                        {
                            "UserId", userId.ToString()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                new TCException(string.Format("EventsOnAfterUpdate failed for userid:{0}", forumReplyVoteAfterUpdateEventArgs.UserId), ex).Log();
            }
        }


        public string Name
        {
            get { return "4 Roads - Forum Reply Vote Created Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a vote is added to a forum post."; }
        }

        public void SetController(IRuleController controller)
        {
            _ruleController = controller;
        }

        public RuleTriggerExecutionContext GetExecutionContext(RuleTriggerData data)
        {
            RuleTriggerExecutionContext context = new RuleTriggerExecutionContext();
            if (data.ContainsKey("UserId"))
            {
                int userId;

                if (int.TryParse(data["UserId"], out userId))
                {
                    var users = Apis.Get<IUsers>();

                    var user = users.Get(new UsersGetOptions() { Id = userId });

                    if (!user.HasErrors())
                    {
                        context.Add(users.ContentTypeId, user);
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
            get { return new[] { Apis.Get<IUsers>().ContentTypeId }; }
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

                defaultTranslation[0].Set("RuleTriggerName", "A forum reply was voted upon");
                defaultTranslation[0].Set("RuleTriggerCategory", "Forum Reply");

                return defaultTranslation;
            }
        }
    }
}
