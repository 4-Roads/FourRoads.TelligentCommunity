using FourRoads.Common.TelligentCommunity.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class ForumAnswerRejected : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{BE32C756-44D1-4756-BC9F-E2972898BCEA}");

        private ConcurrentDictionary<int, ForumReply>
            _beforeUpdateCache = new ConcurrentDictionary<int, ForumReply>();

        public void Initialize()
        {
            Apis.Get<IForumReplies>().Events.BeforeDelete += EventsOnBeforeDelete;
            Apis.Get<IForumReplies>().Events.BeforeUpdate += EventsOnBeforeUpdate;
            Apis.Get<IForumReplies>().Events.AfterUpdate += EventsOnAfterUpdate;
        }

        /// <summary>
        /// Determine what has been effected by the reply being deleted
        /// </summary>
        /// <param name="ForumReplyBeforeDeleteEventArgs"></param>
        /// <returns></returns>
        private void EventsOnBeforeDelete(ForumReplyBeforeDeleteEventArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    // add in any checks in here 
                    if (args.IsAnswer.HasValue && (bool)args.IsAnswer)
                    {
                        CacheForumReply((int)args.Id);

                        _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                        {
                            {
                                "UserId", args.Author.Id.ToString()
                            },
                            {
                                "ReplyId", args.Id.ToString()
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnBeforeDelete failed for forum reply id :{0}", args.Id),
                    ex).Log();
            }
        }

        /// <summary>
        /// Save a version of the forum reply so that we can determine what actually happened 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnBeforeUpdate(ForumReplyBeforeUpdateEventArgs args)
        {
            try
            {
                CacheForumReply((int)args.Id);
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnBeforeUpdatefailed for forum reply id :{0}", args.Id),
                    ex).Log();
            }
        }

        /// <summary>
        /// Check on the action performed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnAfterUpdate(ForumReplyAfterUpdateEventArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    int key = (int)args.Id;

                    string action = string.Empty;

                    // add in any checks in here 
                    if (_beforeUpdateCache.ContainsKey(key))
                    {
                        var old = _beforeUpdateCache[key];
                        if ((old.IsAnswer ?? false) != (args.IsAnswer ?? false))
                        {
                            if (args.IsAnswer ?? false)
                            {
                                action = "Add";
                            }
                            else
                            {
                                action = "Del";
                            }
                        }
                        ForumReply removed;
                        _beforeUpdateCache.TryRemove(key, out removed);
                    }

                    if (action.Equals("Del"))
                    {
                        _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                        {
                            {
                                "UserId", args.Author.Id.ToString()
                            },
                            {
                                "ReplyId", args.Id.ToString()
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterUpdate failed for forum reply id :{0}", args.Id),
                    ex).Log();
            }
        }

        /// <summary>
        /// Cache a copy of the reply before it is updated so that we can later determine what happened
        /// </summary>
        /// <param name="replyId"></param>
        /// <returns>bool</returns>
        /// 
        private bool CacheForumReply(int replyId)
        {
            if (!_beforeUpdateCache.ContainsKey(replyId))
            {
                var reply = Apis.Get<IForumReplies>().Get(replyId);
                if (reply != null && !reply.HasErrors())
                {
                    _beforeUpdateCache.AddOrUpdate(replyId, reply, (key, existingVal) => reply);
                }
            }

            return true;
        }

        public string Name
        {
            get { return "4 Roads - Achievements - Forum Reply Rejected as Answer Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a forum reply is rejected as the answer"; }
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
                    else
                    {
                        // if deleting may have already gone so use the cache 
                        if (_beforeUpdateCache.ContainsKey(replyId))
                        {
                            var cachedForumReply = _beforeUpdateCache[replyId];
                            if (!cachedForumReply.HasErrors())
                            {
                                context.Add(cachedForumReply.GlobalContentTypeId, cachedForumReply);
                            }
                            ForumReply removed;
                            _beforeUpdateCache.TryRemove(replyId, out removed);
                        }
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

        /// <summary>
        /// Setup the contextual datatype ids for users, forum reply and custom trigger parameters
        /// custom trigger parameters are to allow config in the UI for the action being performed (suggested-answer etc)
        /// </summary>
        /// <returns>IEnumerable<Guid></returns>
        /// 
        public IEnumerable<Guid> ContextualDataTypeIds
        {
            get { return new[] { Apis.Get<IUsers>().ContentTypeId, Apis.Get<IForumReplies>().ContentTypeId }; }
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

                defaultTranslation[0].Set("RuleTriggerName", "a forum reply was rejected as the answer - 4 roads");
                defaultTranslation[0].Set("RuleTriggerCategory", "Forum Reply");

                return defaultTranslation;
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

    }
}
