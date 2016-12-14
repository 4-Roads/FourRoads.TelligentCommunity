using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using System;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Actions
{
    public class UpdateConnectedCRMs : IConfigurableRuleAction, ITranslatablePlugin, ICategorizedPlugin
    {
        private ITranslatablePluginController _translationController;
        private Guid _componentId = new Guid("{1B76FAD0-C376-4202-82B3-B872CA26DCFB}");

        public void Initialize()
        {

        }

        public string Name { get { return "4 Roads - Update CRM Callback"; } }

        public string Description { get { return "Updates any CRM plugins with the user that just got updated."; } }

        public Guid RuleComponentId { get { return _componentId; } }

        public string RuleComponentName { get { return _translationController.GetLanguageResourceValue("RuleComponentName"); ; } }

        public string RuleComponentCategory { get { return _translationController.GetLanguageResourceValue("RuleComponentCategory"); ; } }

        public void Execute(IRuleExecutionRuntime runtime)
        {
            User user = runtime.GetCustomUser("User");

            if ( !user.HasErrors() )
            {
                foreach ( ICrmPlugin plugin in PluginManager.Get<ICrmPlugin>() )
                {
                    plugin.SynchronizeUser(user);
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

                defaultTranslation[ 0 ].Set("RuleComponentName", "Call the update method on all enabled CRM plugins");
                defaultTranslation[ 0 ].Set("RuleComponentCategory", "CRM");

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
