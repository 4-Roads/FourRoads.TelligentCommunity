using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Rules.Helpers;
using FourRoads.TelligentCommunity.Rules.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Forums.Internal.ReplyVote;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class ForumReplyUpVoted : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin, IPluginGroup
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{05FFBDD4-7104-4BF6-B885-14F99AA41FAF}");
        private UpVoteTokensRegister _upVoteTokens = new UpVoteTokensRegister();
        private UserTotalTokensRegister _userTotalTokens = new UserTotalTokensRegister();

        public void Initialize()
        {
            Apis.Get<IForumReplyVotes>().Events.AfterCreate += EventsOnAfterCreate;
            Apis.Get<IForumReplyVotes>().Events.AfterUpdate += EventsOnAfterUpdate;
        }

        /// <summary>
        /// Check on the action performed
        /// For some reason this event is also called when updating ?
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnAfterCreate(ForumReplyVoteAfterCreateEventArgs args)
        {
            try
            {
                if (args.VoteType == ForumReplyVoteType.Quality && _ruleController != null)
                {
                    // true = upvote
                    if (args.Value)
                    {
                        UserTotalValues.Votes(args.UserId, args.ReplyId , 1, UserTotalValues.VoteType.UpVoteCount);

                        _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                        {
                            {
                                "UserId", args.UserId.ToString()
                            },
                            {
                                "ReplyId", args.ReplyId.ToString()
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterCreate failed for userid:{0}", args.UserId),
                    ex).Log();
            }
        }

        /// <summary>
        /// Check on the action performed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnAfterUpdate(ForumReplyVoteAfterUpdateEventArgs args)
        {
            try
            {
                if (args.VoteType == ForumReplyVoteType.Quality && _ruleController != null)
                {
                    // true = upvote
                    if (args.Value)
                    {
                        UserTotalValues.Votes(args.UserId, args.ReplyId, 1, UserTotalValues.VoteType.UpVoteCount);

                        _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                        {
                            {
                                "UserId", args.UserId.ToString()
                            },
                            {
                                "ReplyId", args.ReplyId.ToString()
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterUpdate failed for userid:{0}", args.UserId),
                    ex).Log();
            }
        }

        public string Name
        {
            get { return "4 Roads - Forum Reply Up Voted Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a reply is up voted by a user"; }
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

                        //get the extended user attributes
                        UserTotalValues.UpdateContext(context, user);
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

                        // get thread total up votes 
                        var threadReplies = Apis.Get<IForumReplies>().ListThreaded((int)forumReply.ThreadId, null).ToList();
                        var threadUpVotes = threadReplies.Sum(fv => fv.QualityYesVotes ?? 0);
                        
                        UpVoteTriggerParameters upVoteTriggerParameters = new UpVoteTriggerParameters()
                        {
                            ReplyUpVotes = forumReply.QualityYesVotes ?? 0 , 
                            ThreadUpVotes = threadUpVotes
                        };
                        context.Add(_upVoteTokens.UpVoteTriggerParametersTypeId, upVoteTriggerParameters);
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
            get { return new[]
            {
                Apis.Get<IUsers>().ContentTypeId,
                Apis.Get<IForumReplies>().ContentTypeId,
                _upVoteTokens.UpVoteTriggerParametersTypeId,
                _userTotalTokens.UserTotalTriggerParametersTypeId
            };
            }
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

                defaultTranslation[0].Set("RuleTriggerName", "a forum reply was up voted - 4 roads");
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

        public IEnumerable<Type> Plugins => new Type[] { typeof(UpVoteTokensRegister) , typeof(UserTotalTokensRegister) };

    }
}
