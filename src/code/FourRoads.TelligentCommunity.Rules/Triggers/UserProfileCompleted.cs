using FourRoads.Common.TelligentCommunity.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class UserProfileCompleted : IRuleTrigger, ITranslatablePlugin, IConfigurablePlugin, ISingletonPlugin, ICategorizedPlugin
    {
        private static object _lockObj = new object();
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private List<string> _fields = new List<string>();
        private readonly Guid _triggerid = new Guid("{A696AC6A-1CC1-4169-B1E4-4458E94B090C}");
        private ConcurrentDictionary<int, User> _beforeUpdateCache = new ConcurrentDictionary<int, User>();

        public void Initialize()
        {
            Apis.Get<IUsers>().Events.BeforeUpdate += EventsOnBeforeUpdate;
            Apis.Get<IUsers>().Events.AfterUpdate += EventsOnAfterUpdate;
        }

        private void EventsOnBeforeUpdate(UserBeforeUpdateEventArgs userBeforerUpdateEventArgs)
        {
            try
            {
                if (userBeforerUpdateEventArgs.Id.HasValue)
                {
                    if (!userBeforerUpdateEventArgs.IsSystemAccount.GetValueOrDefault(true))
                    {
                        int userId = userBeforerUpdateEventArgs.Id.Value;

                        if (!_beforeUpdateCache.ContainsKey(userId))
                        {
                            User user = Apis.Get<IUsers>().Get(new UsersGetOptions() { Id = userId });

                            if (user != null && !user.HasErrors())
                                _beforeUpdateCache.AddOrUpdate(userId, user, (key, existingVal) => user);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(string.Format("EventsOnBeforeUpdate failed for userid:{0}", userBeforerUpdateEventArgs.Id.GetValueOrDefault(-1)), ex).Log();
            }
        }

        private void EventsOnAfterUpdate(UserAfterUpdateEventArgs userAfterUpdateEventArgs)
        {
            try
            {
                if (userAfterUpdateEventArgs.Id.HasValue)
                {
                    int userId = userAfterUpdateEventArgs.Id.Value;

                    if (_beforeUpdateCache.ContainsKey(userId))
                    {
                        if (_ruleController != null && ProfileCompleted(userAfterUpdateEventArgs))
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", userId.ToString()
                                }
                            });
                        }
                        User removed;
                        _beforeUpdateCache.TryRemove(userId , out removed);
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(string.Format("EventsOnAfterUpdate failed for userid:{0}", userAfterUpdateEventArgs.Id.GetValueOrDefault(-1)), ex).Log();
            }
        }

        /// <summary>
        /// Only want to check that the profile has been completed
        /// the user object has a lot of stuff so we need to check just the configured fields
        /// and that something has changed
        /// </summary>
        /// <param name="userAfterUpdateEventArgs"></param>
        /// <returns></returns>
        private bool ProfileCompleted(UserAfterUpdateEventArgs newDetails)
        {
            User oldDetails = _beforeUpdateCache[newDetails.Id.Value];
            bool hasChanged = false;
            int completeFields = 0;

            lock (_lockObj)
            {
                foreach (string field in _fields)
                {
                    if (field.StartsWith("-"))
                    {
                        string trimedField = field.TrimStart(new[] { '-' });

                        if (oldDetails.ProfileFields != null)
                        {
                            if (oldDetails.ProfileFields[trimedField] != null)
                            {
                                if (oldDetails.ProfileFields[trimedField].Value != newDetails.ProfileFields[trimedField].Value)
                                    hasChanged = true;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(newDetails.ProfileFields[trimedField].Value))
                        {
                            completeFields++;
                        }
                    }
                    else
                    {
                        bool evaluate = false;
                        bool populated = false;
                        //Could use reflection if we wanted to increase the number of comparison fields
                        switch (field)
                        {
                            case "UserName":
                                evaluate = oldDetails.Username != newDetails.Username;
                                populated = !string.IsNullOrWhiteSpace(newDetails.Username);
                                break;
                            case "DisplayName":
                                evaluate = oldDetails.DisplayName != newDetails.DisplayName;
                                populated = !string.IsNullOrWhiteSpace(newDetails.DisplayName);
                                break;
                            case "PrivateEmail":
                                evaluate = oldDetails.PrivateEmail != newDetails.PrivateEmail;
                                populated = !string.IsNullOrWhiteSpace(newDetails.PrivateEmail);
                                break;
                        }

                        if (evaluate)
                        {
                            hasChanged = true;
                        }

                        if (populated)
                        {
                            completeFields++;
                        }
                    }
                }

                if (hasChanged && completeFields == _fields.Count)
                {
                    return true;
                }
            }
            return false;
        }

        public string Name
        {
            get { return "4 Roads - Achievements - User Profile Completed Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a user profile is updated to check if its complete."; }
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

                defaultTranslation[0].Set("RuleTriggerName", "a user profile has been completed - 4 roads");
                defaultTranslation[0].Set("RuleTriggerCategory", "User");

                return defaultTranslation;
            }
        }
        
        public void Update(IPluginConfiguration configuration)
        {
            lock (_lockObj)
            {
                _fields.Clear();

                string fieldList = configuration.GetCustom("fields") ?? string.Empty;

                //Convert the string to  a list
                string[] fieldFilter = fieldList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                _fields.AddRange(GetAllFields().Where(k => fieldFilter.Contains(k.Key) || fieldFilter.Length == 0).Select(f => f.Key));
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetAllFields()
        {
            List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();

            results.Add(new KeyValuePair<string, string>("UserName", "User Name"));
            results.Add(new KeyValuePair<string, string>("DisplayName", "Display Name"));
            results.Add(new KeyValuePair<string, string>("PrivateEmail", "Email"));

            // this does not work in 10.1 as when called from ConfigurationOptions 
            // 
            // Apis.Get<IUserProfileFields>() returns null .... but PublicApi.UserProfileFields works OK 
            //
            //foreach (var field in Apis.Get<IUserProfileFields>().List(new UserProfileFieldsListOptions() { PageIndex = 0, PageSize = int.MaxValue }))
            //{
            //    results.Add(new KeyValuePair<string, string>("-" + field.Name, field.Title));
            //}

            foreach (var field in PublicApi.UserProfileFields.List(new UserProfileFieldsListOptions() { PageIndex = 0, PageSize = int.MaxValue }))
            {
                results.Add(new KeyValuePair<string, string>("-" + field.Name, field.Title));
            }

            foreach (KeyValuePair<string, string> keyValuePair in results)
            {
                yield return keyValuePair;
            }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {

                PropertyGroup group = new PropertyGroup("Options", "Options", 0);
                Property availableFields = new Property("fields", "Fields", PropertyType.Custom, 0, "");

                availableFields.ControlType = typeof(CheckboxListControl);
                foreach (var field in GetAllFields())
                {
                    availableFields.SelectableValues.Add(new PropertyValue(field.Key, field.Value, 0) { });
                }

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
