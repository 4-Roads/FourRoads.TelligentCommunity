using System;
using System.Globalization;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;

namespace FourRoads.TelligentCommunity.Rules.Actions
{

    public class UpdateUserReputationPoints : IConfigurableRuleAction, ITranslatablePlugin, ICategorizedPlugin , IConfigurablePlugin
    {
        private ITranslatablePluginController _translationController;
        private Guid _componentId = new Guid("{29C18C56-678A-4DC1-BD76-885546D611AD}");
        private int _dailyGainLimit = 200;
        
        public void Initialize()
        {
        }

        public string Name { get { return "4 Roads - Achievements - Update user reputation points"; } }

        public string Description { get { return "Update the user reputation points/score with daily limit"; } }

        public Guid RuleComponentId { get { return _componentId; } }

        public string RuleComponentName { get { return _translationController.GetLanguageResourceValue("RuleComponentName"); } }

        public string RuleComponentCategory { get { return _translationController.GetLanguageResourceValue("RuleComponentCategory"); } }

        public void Execute(IRuleExecutionRuntime runtime)
        {
            User user = runtime.GetCustomUser("User");
            if (!user.HasErrors())
            {
                int points = runtime.GetInt("Points");
                int reputationGain = 0;
                //int reputation = 0;

                ExtendedAttribute userReputationDate = user.ExtendedAttributes.Get("UserReputationDate");
                ExtendedAttribute userReputationGain = user.ExtendedAttributes.Get("UserReputationGain");

                // check if the user has reached their daily limit for reputation gain
                if (userReputationDate != null && userReputationGain != null)
                {
                    DateTime reputationDate;
                    if (DateTime.TryParseExact(userReputationDate.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out reputationDate))
                    {
                        if (reputationDate.Date.Equals(DateTime.Now.Date))
                        {
                            if (int.TryParse(userReputationGain.Value, out reputationGain))
                            {
                                // stop if we are adding points and have reached the daily limit
                                if (points > 0 && reputationGain >= _dailyGainLimit)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }

                var desc = runtime.GetString("Description");
                if (string.IsNullOrWhiteSpace(desc))
                {
                    desc = "Reputation";
                }

                Apis.Get<IPointTransactions>().Create(desc, user.Id.Value, points, user.ContentId, Apis.Get<IUsers>().ContentTypeId, new PointTransactionCreateOptions() { });

                //ExtendedAttribute userReputation = user.ExtendedAttributes.Get("UserReputation");
                //if (userReputation != null)
                //{
                //    int.TryParse(userReputation.Value, out reputation);
                //}

                //reputation += points;
                reputationGain += points;

                if (userReputationDate == null)
                {
                    user.ExtendedAttributes.Add(new ExtendedAttribute() { Key = "UserReputationDate", Value = DateTime.Now.ToString("yyyyMMdd") });
                }
                else
                {
                    userReputationDate.Value = DateTime.Now.ToString("yyyyMMdd");
                }

                if (userReputationGain == null)
                {
                    user.ExtendedAttributes.Add(new ExtendedAttribute() { Key = "UserReputationGain", Value = Math.Max(0, reputationGain).ToString() });
                }
                else
                {
                    userReputationGain.Value = Math.Max(0,reputationGain).ToString();
                }

                //if (userReputation == null)
                //{
                //    user.ExtendedAttributes.Add(new ExtendedAttribute() { Key = "UserReputation", Value = Math.Max(0, reputation).ToString() });
                //}
                //else
                //{
                //    userReputation.Value = Math.Max(0, reputation).ToString();
                //}
                Apis.Get<IUsers>().Update(new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = user.ExtendedAttributes });
               
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

                defaultTranslation[0].Set("RuleComponentName", "award points with daily limit - 4 roads");
                defaultTranslation[0].Set("RuleComponentCategory", "Reputation");

                return defaultTranslation;
            }
        }

        public PropertyGroup[] ConfigurableValues
        {
            get
            {
                PropertyGroup group = new PropertyGroup("settings", "settings", 0);
                Property userProp = new Property("User", "User", PropertyType.Custom, 2, "");
                userProp.ControlType = typeof(UserTokenSelectionControl);
                group.Properties.Add(userProp);

                group.Properties.Add(new Property("Points", "Points", PropertyType.Int, 2, ""));

                group.Properties.Add(new Property("Description", "Description", PropertyType.String, 3, ""));

                return new PropertyGroup[] { group };
            }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var pg = new PropertyGroup("Options", "Options", 0);
                pg.Properties.Add(new Property("DailyGainLimit", "Daily Gain Limit", PropertyType.Int, 2, _dailyGainLimit.ToString()));

                return new PropertyGroup[] { pg };
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            _dailyGainLimit = configuration.GetInt("DailyGainLimit");
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
