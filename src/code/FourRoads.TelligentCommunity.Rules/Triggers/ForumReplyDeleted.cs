using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Rules.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class ForumReplyDeleted : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin, IPluginGroup
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{A0D49DDB-7336-47D3-9884-23120806D75B}");
        private UpVoteTokensRegister _ruleUpTokens = new UpVoteTokensRegister();
        private DownVoteTokensRegister _ruleDownTokens = new DownVoteTokensRegister();

        private ConcurrentDictionary<int, ForumReply> _beforeDeleteCache = new ConcurrentDictionary<int, ForumReply>();

        public void Initialize()
        {
            Apis.Get<IForumReplies>().Events.BeforeDelete += EventsOnBeforeDelete;
        }

        /// <summary>
        /// Check on the action performed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnBeforeDelete(ForumReplyBeforeDeleteEventArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    var replyUpVotes = 0;
                    var threadUpVotes = 0;
                    var replyDownVotes = 0;
                    var threadDownVotes = 0;

                    var forumReplies = Apis.Get<IForumReplies>();
                    var forumReply = forumReplies.Get((int)args.Id);

                    if (!forumReply.HasErrors())
                    {
                        // get thread total up votes 
                        var threadReplies = Apis.Get<IForumReplies>().ListThreaded((int)forumReply.ThreadId, null).ToList();
                        var threadVotes = threadReplies.Sum(fv => fv.QualityYesVotes ?? 0);

                        replyUpVotes = forumReply.QualityYesVotes ?? 0;
                        threadUpVotes = threadVotes;

                        // get thread total down votes 
                        threadVotes = threadReplies.Sum(fv => fv.QualityNoVotes ?? 0);

                        replyDownVotes = forumReply.QualityNoVotes ?? 0;
                        threadDownVotes = threadVotes;

                        CacheForumReply(forumReply);
                    }

                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                    {
                        {
                            "UserId", Apis.Get<IUsers>().AccessingUser.Id.ToString()
                        },
                        {
                            "ReplyId", args.Id.ToString()
                        },
                        {
                            "ReplyUpVotes" , replyUpVotes.ToString()
                        },
                        {
                            "ThreadUpVotes" , threadUpVotes.ToString()
                        },
                        {
                            "ReplyDownVotes" , replyDownVotes.ToString()
                        },
                        {
                            "ThreadDownVotes" , threadDownVotes.ToString()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnBeforeDelete failed for id:{0}", args.Id),
                    ex).Log();
            }
        }

        /// <summary>
        /// Cache a copy of the reply before it is deleted so that we can use it in the rule trigger
        /// </summary>
        /// <param name="forumReply"></param>
        /// <returns>bool</returns>
        /// 
        private bool CacheForumReply(ForumReply forumReply)
        {
            _beforeDeleteCache.AddOrUpdate((int)forumReply.Id, forumReply, (key, existingVal) => forumReply);
            return true;
        }

        public string Name
        {
            get { return "4 Roads - Achievements - Forum Reply Deleted Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a reply is deleted"; }
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

            if (data.ContainsKey("ReplyUpVotes"))
            {
                int replyUpVotes;
                int threadUpVotes;

                if (int.TryParse(data["ReplyUpVotes"], out replyUpVotes) &&
                    int.TryParse(data["ThreadUpVotes"], out threadUpVotes))
                {
                    UpVoteTriggerParameters upVoteTriggerParameters = new UpVoteTriggerParameters()
                    {
                        ReplyUpVotes = replyUpVotes,
                        ThreadUpVotes = threadUpVotes
                    };
                    context.Add(_ruleUpTokens.UpVoteTriggerParametersTypeId, upVoteTriggerParameters);
                }
            }

            if (data.ContainsKey("ReplyDownVotes"))
            {
                int replyDownVotes;
                int threadDownVotes;

                if (int.TryParse(data["ReplyDownVotes"], out replyDownVotes) &&
                    int.TryParse(data["ThreadDownVotes"], out threadDownVotes))
                {
                    DownVoteTriggerParameters downVoteTriggerParameters = new DownVoteTriggerParameters()
                    {
                        ReplyDownVotes = replyDownVotes,
                        ThreadDownVotes = threadDownVotes
                    };
                    context.Add(_ruleDownTokens.DownVoteTriggerParametersTypeId, downVoteTriggerParameters);
                }
            }

            if (data.ContainsKey("ReplyId"))
            {
                int replyId;
                if (int.TryParse(data["ReplyId"], out replyId))
                {
                    if (_beforeDeleteCache.ContainsKey(replyId))
                    {
                        var forumReply = _beforeDeleteCache[replyId];
                        if (!forumReply.HasErrors())
                        {
                            context.Add(forumReply.GlobalContentTypeId, forumReply);
                        }
                        ForumReply removed;
                        _beforeDeleteCache.TryRemove(replyId, out removed);
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
        /// custom trigger parameters are to allow config in the UI for the action being performed (add-upvote, del-upvote etc)
        /// </summary>
        /// <returns>IEnumerable<Guid></returns>
        /// 
        public IEnumerable<Guid> ContextualDataTypeIds
        {
            get { return new[] { Apis.Get<IUsers>().ContentTypeId, Apis.Get<IForumReplies>().ContentTypeId, _ruleUpTokens.UpVoteTriggerParametersTypeId, _ruleDownTokens.DownVoteTriggerParametersTypeId }; }
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

                defaultTranslation[0].Set("RuleTriggerName", "a forum reply was deleted - 4 roads");
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

        public IEnumerable<Type> Plugins => new Type[] { typeof(UpVoteTokensRegister), typeof(DownVoteTokensRegister) };

    }
}
