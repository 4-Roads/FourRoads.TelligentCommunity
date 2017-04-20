using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Rules.Tokens;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Templating.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class ForumReplyVoteUpdated : IRuleTrigger, ITranslatablePlugin, IConfigurablePlugin, ISingletonPlugin , ITokenRegistrar
    {
        private static object _lockObj = new object();
        private List<string> _actions = new List<string>();
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{8D90F6A7-D4DB-4C6F-8634-CDE6A9D165A2}");
        private readonly Guid _customTriggerParametersTypeId = new Guid("FC7E7736-84D4-41AB-BD27-1E746925E7C0");
        
        private ConcurrentDictionary<string, ForumReplyVote>
            _beforeUpdateCache = new ConcurrentDictionary<string, ForumReplyVote>();

        public void Initialize()
        {
            Apis.Get<IForumReplyVotes>().Events.BeforeCreate += EventsOnBeforeCreate;
            Apis.Get<IForumReplyVotes>().Events.AfterCreate += EventsOnAfterCreate;
            Apis.Get<IForumReplyVotes>().Events.AfterDelete += EventsOnAfterDelete;
            Apis.Get<IForumReplyVotes>().Events.BeforeUpdate += EventsOnBeforeUpdate;
            Apis.Get<IForumReplyVotes>().Events.AfterUpdate += EventsOnAfterUpdate;
        }

        /// <summary>
        /// Save a version of the vote so that we can determine what actually happened 
        /// </summary>
        /// <param name="forumReplyVoteBeforeCreateEventArgs"></param>
        /// <returns></returns>
        private void EventsOnBeforeCreate(ForumReplyVoteBeforeCreateEventArgs forumReplyVoteBeforeCreateEventArgs)
        {
            try
            {
                CacheUserVote(forumReplyVoteBeforeCreateEventArgs.ReplyId, forumReplyVoteBeforeCreateEventArgs.UserId);
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnBeforeCreate failed for userid:{0}",
                        forumReplyVoteBeforeCreateEventArgs.UserId), ex).Log();
            }
        }

        /// <summary>
        /// Check on the action performed
        /// For some reason this event is also called whren updating ?
        /// </summary>
        /// <param name="forumReplyVoteAfterCreateEventArgs"></param>
        /// <returns></returns>
        private void EventsOnAfterCreate(ForumReplyVoteAfterCreateEventArgs forumReplyVoteAfterCreateEventArgs)
        {
            try
            {
                if (_ruleController != null)
                {
                    string key = forumReplyVoteAfterCreateEventArgs.UserId + "," +
                                 forumReplyVoteAfterCreateEventArgs.ReplyId;

                    List<string> actions = new List<string>();
                    if (forumReplyVoteAfterCreateEventArgs.Value)
                    {
                        actions.Add("Add-UpVote");
                    }
                    else
                    {
                        actions.Add("Add-DownVote");
                    }

                    if (_beforeUpdateCache.ContainsKey(key))
                    {
                        var old = _beforeUpdateCache[key];
                        if (old.Value)
                        {
                            actions.Add("Del-UpVote");
                        }
                        else
                        {
                            actions.Add("Del-DownVote");
                        }

                        ForumReplyVote removed;
                        _beforeUpdateCache.TryRemove(key, out removed);
                    }
                    foreach (var action in actions)
                    {
                        if (IsActionActive(action))
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", forumReplyVoteAfterCreateEventArgs.UserId.ToString()
                                },
                                {
                                    "ReplyId", forumReplyVoteAfterCreateEventArgs.ReplyId.ToString()
                                },
                                {
                                    "Action", action
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterCreate failed for userid:{0}", forumReplyVoteAfterCreateEventArgs.UserId),
                    ex).Log();
            }
        }

        /// <summary>
        /// Determine what was removed
        /// </summary>
        /// <param name="forumReplyVoteAfterDeleteEventArgs"></param>
        /// <returns></returns>
        private void EventsOnAfterDelete(ForumReplyVoteAfterDeleteEventArgs forumReplyVoteAfterDeleteEventArgs)
        {
            try
            {
                if (_ruleController != null)
                {
                    string action = "Del-DownVote";

                    if (forumReplyVoteAfterDeleteEventArgs.Value)
                    {
                        action = "Del-UpVote";
                    }
                    if (IsActionActive(action))
                    {
                        _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                        {
                            {
                                "UserId", forumReplyVoteAfterDeleteEventArgs.UserId.ToString()
                            },
                            {
                                "ReplyId", forumReplyVoteAfterDeleteEventArgs.ReplyId.ToString()
                            },
                            {
                                "Action", action
                            }

                        });
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterDelete failed for userid:{0}", forumReplyVoteAfterDeleteEventArgs.UserId),
                    ex).Log();
            }
        }

        /// <summary>
        /// Save a version of the vote so that we can determine what actually happened 
        /// </summary>
        /// <param name="forumReplyVoteBeforeUpdateEventArgs"></param>
        /// <returns></returns>
        private void EventsOnBeforeUpdate(ForumReplyVoteBeforeUpdateEventArgs forumReplyVoteBeforeUpdateEventArgs)
        {
            try
            {
                CacheUserVote(forumReplyVoteBeforeUpdateEventArgs.ReplyId, forumReplyVoteBeforeUpdateEventArgs.UserId);
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnBeforeUpdate failed for userid:{0}",
                        forumReplyVoteBeforeUpdateEventArgs.UserId), ex).Log();
            }
        }

        /// <summary>
        /// Check on the action performed
        /// This does not seem to get fired, but is here incase it gets fixed ....
        /// The updates are currently handled by the Update events 
        /// </summary>
        /// <param name="forumReplyVoteAfterUpdateEventArgs"></param>
        /// <returns></returns>
        private void EventsOnAfterUpdate(ForumReplyVoteAfterUpdateEventArgs forumReplyVoteAfterUpdateEventArgs)
        {
            try
            {
                if (_ruleController != null)
                {
                    string key = forumReplyVoteAfterUpdateEventArgs.UserId + "," +
                                 forumReplyVoteAfterUpdateEventArgs.ReplyId;

                    List<string> actions = new List<string>();
                    if (forumReplyVoteAfterUpdateEventArgs.Value)
                    {
                        actions.Add("Add-UpVote");
                    }
                    else
                    {
                        actions.Add("Add-DownVote");
                    }

                    if (_beforeUpdateCache.ContainsKey(key))
                    {
                        var old = _beforeUpdateCache[key];
                        if (old.Value)
                        {
                            actions.Add("Del-UpVote");
                        }
                        else
                        {
                            actions.Add("Del-DownVote");
                        }

                        ForumReplyVote removed;
                        _beforeUpdateCache.TryRemove(key, out removed);
                    }
                    foreach (var action in actions)
                    {
                        if (IsActionActive(action))
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", forumReplyVoteAfterUpdateEventArgs.UserId.ToString()
                                },
                                {
                                    "ReplyId", forumReplyVoteAfterUpdateEventArgs.ReplyId.ToString()
                                },
                                {
                                    "Action", action
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterUpdate failed for userid:{0}", forumReplyVoteAfterUpdateEventArgs.UserId),
                    ex).Log();
            }
        }

        /// <summary>
        /// Cache a copy of the vote before it is updated so that we can later determine what happed
        /// </summary>
        /// <param name="replyId"></param>
        /// <param name="userId"></param>
        /// <returns>bool</returns>
        /// 
        private bool CacheUserVote(int replyId, int userId)
        {
            if (!_beforeUpdateCache.ContainsKey(userId + "," + replyId))
            {
                var vote = Apis.Get<IForumReplyVotes>()
                    .Get(replyId, new ForumReplyVoteGetOptions() {VoteType = "Quality"});

                if (!vote.HasErrors())
                {
                    _beforeUpdateCache.AddOrUpdate(userId + "," + replyId, vote, (key, existingVal) => vote);
                }
            }

            return true;
        }

        /// <summary>
        /// Check if the action is configured for the current action
        /// </summary>
        /// <param name="action"></param>
        /// <returns>bool</returns>
        /// 
        private bool IsActionActive(string action)
        {
            lock (_lockObj)
            {
                if (_actions.Contains(action))
                {
                    return true;
                }
                return false;
            }
        }

        public string Name
        {
            get { return "4 Roads - Forum Reply Vote Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a vote is added, removed or toggled for a forum post."; }
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
                    var user = users.Get(new UsersGetOptions() {Id = userId});

                    if (!user.HasErrors())
                    {
                        context.Add(users.ContentTypeId, user);
                        context.Add(_triggerid, true); //Added this trigger so that it is not re-entrant
                    }
                }
            }

            if (data.ContainsKey("ReplyId"))
            {
                int replyId;

                if (int.TryParse(data["ReplyId"], out replyId))
                {
                    var forumReplies = Apis.Get<IForumReplies>();
                    var forumReply = forumReplies.Get(replyId);

                    if (!forumReply.HasErrors())
                    {
                        context.Add(forumReply.GlobalContentTypeId, forumReply);
                    }
                }
            }

            if (data.ContainsKey("Action"))
            {
                CustomTriggerParameters ruleParameters = new CustomTriggerParameters() {Action = data["Action"]};
                context.Add(_customTriggerParametersTypeId, ruleParameters);
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

        /// <summary>
        /// Setup the contextual datatype ids for users, forum reply and custom trigger parameters
        /// custom trigger parameters are to allow config in the UI for the action being performed (add-upvote, del-upvote etc)
        /// </summary>
        /// <returns>IEnumerable<Guid></returns>
        /// 
        public IEnumerable<Guid> ContextualDataTypeIds
        {
            get { return new[] {Apis.Get<IUsers>().ContentTypeId, Apis.Get<IForumReplies>().ContentTypeId, _customTriggerParametersTypeId }; }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translationController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation[] defaultTranslation = new[] {new Translation("en-us")};

                defaultTranslation[0].Set("RuleTriggerName", "a forum reply was voted upon");
                defaultTranslation[0].Set("RuleTriggerCategory", "Forum Reply");

                return defaultTranslation;
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            lock (_lockObj)
            {
                _actions.Clear();

                string fieldList = configuration.GetCustom("Actions") ?? string.Empty;

                //Convert the string to  a list
                string[] fieldFilter = fieldList.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

                _actions.AddRange(fieldFilter);
            }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup group = new PropertyGroup("Options", "Options", 0);
                Property availableFields = new Property("Actions", "Actions", PropertyType.Custom, 0, "");

                availableFields.ControlType = typeof(CheckboxListControl);
                availableFields.SelectableValues.Add(new PropertyValue("Add-DownVote", "Added Down Vote", 0) {});
                availableFields.SelectableValues.Add(new PropertyValue("Add-UpVote", "Added Up Vote", 0) {});
                availableFields.SelectableValues.Add(new PropertyValue("Del-DownVote", "Removed Down Vote", 0) {});
                availableFields.SelectableValues.Add(new PropertyValue("Del-UpVote", "Removed Up Vote", 0) {});

                group.Properties.Add(availableFields);

                return new[] {group};
            }
        }

        /// <summary>
        /// Register a new token for accessing the voting action performed
        /// </summary>
        /// <param name="action"></param>
        /// <returns>bool</returns>
        /// 
        public void RegisterTokens(ITokenizedTemplateTokenController tokenController)
        {
            // Naming convention allows consistency in display in the token drop-down menu.
            tokenController.Register(new CustomTriggerParametersToken("CustomParameters: Action",
                "The vote action for the rule trigger event.",
                // A unique, static id is needed for each token
                Guid.Parse("5E171D28-BB01-4A2B-A293-5BEF7309A488"),
                _customTriggerParametersTypeId,
                PrimitiveType.String,
                context => context.Get<CustomTriggerParameters>(_customTriggerParametersTypeId).Action));
        }
    }
}
