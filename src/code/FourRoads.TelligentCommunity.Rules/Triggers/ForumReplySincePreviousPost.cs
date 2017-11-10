using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Rules.Tokens;
using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class ForumReplySincePreviousPost : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin, IPluginGroup
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("B295CEC1-CD6D-43AD-B08A-62266E489189");
        private SincePreviousPostTokensRegister _sincePreviousPostTokens = new SincePreviousPostTokensRegister();

        public void Initialize()
        {
            Apis.Get<IForumReplies>().Events.BeforeCreate += EventsOnBeforeCreate;
        }

        /// <summary>
        /// Check on the action performed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnBeforeCreate(ForumReplyBeforeCreateEventArgs args)
        {
            try
            {
                if (args.ThreadId != null && args.Date != null)
                {
                    var thread = Apis.Get<IForumThreads>().Get((int)args.ThreadId, null);
                    if (thread != null && !thread.HasErrors() && thread.LatestPostDate != null)
                    {

                        TimeSpan timeSpan = (DateTime)args.Date - (DateTime)thread.LatestPostDate;
                        if (timeSpan.Days > 0)
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", args.Author.Id.ToString()
                                },
                                {
                                    "ThreadId", thread.Id.ToString()
                                },
                                {
                                    "DaysSinceLastPost", timeSpan.Days.ToString()
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterCreate failed for userid:{0}", args.Author.Id),
                    ex).Log();
            }
        }

        public string Name
        {
            get { return "4 Roads - Achievements - Forum Reply Days Since Previous Post Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a reply is created that is more than 1 day since the previous post"; }
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

            if (data.ContainsKey("ThreadId"))
            {
                int threadId;
                if (int.TryParse(data["ThreadId"], out threadId))
                {
                    var forumThread = Apis.Get<IForumThreads>().Get(threadId);
                    if (!forumThread.HasErrors())
                    {
                        context.Add(forumThread.GlobalContentTypeId, forumThread);
                    }
                }
            }

            if (data.ContainsKey("DaysSinceLastPost"))
            {
                int days;
                if (int.TryParse(data["DaysSinceLastPost"], out days))
                {
                    SincePreviousPostTriggerParameters sincePreviousPostTriggerParameters = new SincePreviousPostTriggerParameters()
                    {
                        DaysSincePreviousPost = days
                    };
                    context.Add(_sincePreviousPostTokens.SincePreviousPostTriggerParametersTypeId, sincePreviousPostTriggerParameters);
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
            get
            {
                return new[]
                {
                    Apis.Get<IUsers>().ContentTypeId,
                    Apis.Get<IForumThreads>().ContentTypeId,
                    _sincePreviousPostTokens.SincePreviousPostTriggerParametersTypeId
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

                defaultTranslation[0].Set("RuleTriggerName", "a forum reply was added x days since the previous post - 4 roads");
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

        public IEnumerable<Type> Plugins => new Type[] { typeof(SincePreviousPostTokensRegister) };

    }
}
