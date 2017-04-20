using System;
using System.Collections.Generic;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Actions
{

    public class UpdateUserReputationPoints : IConfigurableRuleAction, ITranslatablePlugin, ICategorizedPlugin
    {
        private ITranslatablePluginController _translationController;
        private Guid _componentId = new Guid("{29C18C56-678A-4DC1-BD76-885546D611AD}");

        public void Initialize()
        {
        }

        public string Name { get { return "4 Roads - Update user reputation points"; } }

        public string Description { get{return "Update the extended property field for user reputation points";} }

        public Guid RuleComponentId { get { return _componentId; } }

        public string RuleComponentName { get { return _translationController.GetLanguageResourceValue("RuleComponentName"); ;} }

        public string RuleComponentCategory { get { return _translationController.GetLanguageResourceValue("RuleComponentCategory"); ;} }

        public void Execute(IRuleExecutionRuntime runtime)
        {
            User user = runtime.GetCustomUser("User");

            if (!user.HasErrors())
            {
                var field = Apis.Get<UserProfileFields>().Get("UserReputation");
                if (!field.HasErrors())
                {
                    var value = user.ProfileFields.Get("UserReputation");
                    if (!value.HasErrors())
                    {
                        int reputation = 0;
                        int update = runtime.GetInt("Points");
                        int.TryParse(value.Value, out reputation);

                        reputation += update;

                        try
                        {
                            UsersUpdateOptions userUpdateOptions = new UsersUpdateOptions
                            {
                                ProfileFields = new List<ProfileField>
                                {
                                    new ProfileField
                                    {
                                        Label = "UserReputation",
                                        Value = reputation.ToString()
                                    }
                                },
                                Id = user.Id
                            };

                            var newUser = Apis.Get<IUsers>().Update(userUpdateOptions);
                        }
                        catch (Exception e)
                        {
                            var message = e.Message;
                        }
                    }
                }
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

                defaultTranslation[0].Set("RuleComponentName", "update the user reputation based on system actions");
                defaultTranslation[0].Set("RuleComponentCategory", "Reputation");

                return defaultTranslation;
            }
        }

        public PropertyGroup[] ConfigurableValues
        {
            get
            {
                PropertyGroup group = new PropertyGroup("settings","settings" ,0);
                Property userProp = new Property("User", "User", PropertyType.Custom, 2, "");
                userProp.ControlType = typeof (UserTokenSelectionControl);
                group.Properties.Add(userProp);

                group.Properties.Add(new Property("Points", "Points", PropertyType.Int, 2, ""));

                return new[] {group};
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
