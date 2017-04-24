using System;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Rules.Tokens;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class AbusiveContentUpdated : IRuleTrigger, ITranslatablePlugin, IConfigurablePlugin, ISingletonPlugin, ICategorizedPlugin
    {
        private static object _lockObj = new object();
        private List<string> _actions = new List<string>();
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private Guid _triggerid = new Guid("{E234C7D0-5649-47B4-BEB6-AD8D8CAC8786}");
        private RegisterRuleTokens _ruleTokens = new RegisterRuleTokens();

        public void Initialize()
        {
            Apis.Get<IAbusiveContent>().Events.AfterFoundAbusive += EventsOnAfterFoundAbusive;
            Apis.Get<IAbusiveContent>().Events.AfterFoundNotAbusive += EventsOnAfterFoundNotAbusive;
        }

        private void EventsOnAfterFoundAbusive(AbusiveContentAfterFoundAbusiveEventArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    List<string> actions = new List<string>();
                    actions.Add("Add-Abuse");
                    
                    foreach (var action in actions)
                    {
                        if (IsActionActive(action))
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", args.AuthorUserId.ToString()
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
                new TCException(string.Format("EventsOnAfterFoundAbusive failed for abuse report id:{0}", args.AbuseId), ex).Log();
            }
        }

        private void EventsOnAfterFoundNotAbusive(AbusiveContentAfterFoundNotAbusiveEventArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    List<string> actions = new List<string>();
                    actions.Add("Del-Abuse");

                    foreach (var action in actions)
                    {
                        if (IsActionActive(action))
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", args.AuthorUserId.ToString()
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
                new TCException(string.Format("EventsOnAfterFoundNotAbusive failed for abuse report id:{0}", args.AbuseId), ex).Log();
            }
        }

        /// <summary>
        /// Check if the action is configured for the current rule
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
            get { return "4 Roads - Content flagged as abusive trigger"; }
        }

        public string Description
        {
            get { return "Fires when a post is marked as abusive."; }
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
                        context.Add(_triggerid , true); //Added this trigger so that it is not re-entrant
                    }
                }
            }

            if (data.ContainsKey("Action"))
            {
                CustomTriggerParameters ruleParameters = new CustomTriggerParameters() { Action = data["Action"] };
                context.Add(_ruleTokens.CustomTriggerParametersTypeId, ruleParameters);
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
            get { return new[] { Apis.Get<IUsers>().ContentTypeId , _ruleTokens.CustomTriggerParametersTypeId }; }
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

                defaultTranslation[0].Set("RuleTriggerName", "a post is flagged as abusive");
                defaultTranslation[0].Set("RuleTriggerCategory", "User");

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
                string[] fieldFilter = fieldList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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
                availableFields.SelectableValues.Add(new PropertyValue("Add-Abuse", "Reply marked as being abusive", 0) { });
                availableFields.SelectableValues.Add(new PropertyValue("Del-Abuse", "Reply cleared of being abusing", 0) { });

                group.Properties.Add(availableFields);

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
    }
}
