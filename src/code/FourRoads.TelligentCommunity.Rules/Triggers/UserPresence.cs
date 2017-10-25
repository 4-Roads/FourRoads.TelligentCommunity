using FourRoads.Common.TelligentCommunity.Components;
using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class UserPresence : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{17C8432F-25F7-4342-83E4-4C9CBA2D4837}");

        public void Initialize()
        {
            Apis.Get<IContentPresence>().Events.AfterCreate += EventsOnAfterCreate;
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
                var user = Apis.Get<IUsers>().AccessingUser;
                if (user != null && !user.HasErrors() && user.Id != null)
                {
                    _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                    {
                        {
                            "UserId", user.Id.ToString()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterViewCreate failed for id :{0}", args.ContentId),
                    ex).Log();
            }
        }

        public string Name
        {
            get { return "4 Roads - User Presence"; }
        }

        public string Description
        {
            get { return "Fires when a users views a forum or post in the platform"; }
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

                defaultTranslation[0].Set("RuleTriggerName", "a user has a presence in the platform - 4 roads");
                defaultTranslation[0].Set("RuleTriggerCategory", "Achievements");

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
