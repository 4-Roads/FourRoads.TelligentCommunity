using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Actions
{
    public class UnjoinAllGroups : IConfigurableRuleAction, ITranslatablePlugin
    {
        private ITranslatablePluginController _translationController;
        private Guid _componentId = new Guid("{8CF1F489-A812-46C0-BEC1-A63F421365ED}");

        public void Initialize()
        {
            
        }

        public string Name { get { return "4 Roads - Unjoin All Groups Rule Action"; } }

        public string Description { get{return "Unjoins a user from all groups in the system that they are a member of.";} }

        public Guid RuleComponentId { get { return _componentId; } }

        public string RuleComponentName { get { return _translationController.GetLanguageResourceValue("RuleComponentName"); ;} }

        public string RuleComponentCategory { get { return _translationController.GetLanguageResourceValue("RuleComponentCategory"); ;} }

        public void Execute(IRuleExecutionRuntime runtime)
        {
            User user = runtime.GetCustomUser("user");

            if (!user.HasErrors())
            {
                var groupMembership = PublicApi.GroupUserMembers.List(new GroupUserMembersListOptions() {UserId = user.Id.Value, PageSize = int.MaxValue});

                foreach (GroupUser groupUser in groupMembership)
                {
                    PublicApi.GroupUserMembers.Delete(groupUser.Group.Id.Value, new GroupUserMembersDeleteOptions() {UserId = user.Id.Value});
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

                defaultTranslation[0].Set("RuleComponentName", "remove this user from all groups they are a member of");
                defaultTranslation[0].Set("RuleComponentCategory", "Group");

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

                return new[] {group};
            }
        }
    }
}
