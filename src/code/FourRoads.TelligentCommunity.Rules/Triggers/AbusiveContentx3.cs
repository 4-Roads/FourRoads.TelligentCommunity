using System;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class AbusiveContentx3 : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{6F378117-BC9F-4C87-890F-477D9D0D6245}");

        public void Initialize()
        {
            Apis.Get<IAbusiveContent>().Events.AfterFoundAbusive += EventsOnAfterFoundAbusive;
        }

        private void EventsOnAfterFoundAbusive(AbusiveContentAfterFoundAbusiveEventArgs args)
        {
            try
            {
                if (_ruleController != null && args.TotalReportCount.Equals(3))
                {
                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                    {
                        {
                            "UserId", args.AuthorUserId.ToString()
                        },
                        {
                            "AbuseId", args.AbuseId.ToString()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                new TCException(string.Format("EventsOnAfterFoundAbusive failed for abuse report id:{0}", args.AbuseId), ex).Log();
            }
        }

        public string Name
        {
            get { return "4 Roads - Content flagged as abusive trigger (x3)"; }
        }

        public string Description
        {
            get { return "Fires when a post is marked as abusive three times or more."; }
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

                if (data.ContainsKey("AbuseId"))
                {
                    Guid abuseId;

                    if (Guid.TryParse(data["AbuseId"], out abuseId))
                    {
                        var content = Apis.Get<IAbusiveContent>();
                        var abusiveContent = content.Get(abuseId, content.DataTypeId);

                        if (!abusiveContent.HasErrors())
                        {
                            context.Add(content.DataTypeId, abusiveContent);
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

                defaultTranslation[0].Set("RuleTriggerName", "a post is flagged as abusive 3 times - 4 roads");
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
    }
}
