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

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class ForumThreadTagged : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin , IPluginGroup
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{E0EDB61D-1D00-4B09-8C37-09F8EB3F899C}");
        private UserTotalTokensRegister _userTotalTokens = new UserTotalTokensRegister();
        private Guid _forumThreadType;


        public void Initialize()
        {
            Apis.Get<ITags>().Events.AfterAdd += EventsOnAfterAdd;
            _forumThreadType = Apis.Get<IForumThreads>().ContentTypeId;
        }

        private void EventsOnAfterAdd(TagAfterAddEventArgs args)
        {
            try
            {
                if (_ruleController != null && args.ContentTypeId == _forumThreadType)
                {
                    if (args.Tags != null && args.Tags.Any())
                    {
                        UserTotalValues.Tags(args.UserId, args.Tags.Length);
                    }

                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                        {
                            {
                                "UserId", args.UserId.ToString()
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterAdd failed for id :{0}", args.ContentId),
                    ex).Log();
            }
        }

        public string Name
        {
            get { return "4 Roads - Forum thread is tagged trigger"; }
        }

        public string Description
        {
            get { return "Fires when a thread is tagged"; }
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
                        UserTotalValues.UpdateContext(context , user);
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
            get { return new[]
            {
                Apis.Get<IUsers>().ContentTypeId,
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

                defaultTranslation[0].Set("RuleTriggerName", "a thread is tagged - 4 roads");
                defaultTranslation[0].Set("RuleTriggerCategory", "User");

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
        public IEnumerable<Type> Plugins => new Type[] { typeof(UserTotalTokensRegister) };

    }
}
