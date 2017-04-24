using System;
using Telligent.Evolution.Extensibility.Templating.Version1;

namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    public class RegisterRuleTokens : ITokenRegistrar
    {
        public readonly Guid CustomTriggerParametersTypeId = new Guid("FC7E7736-84D4-41AB-BD27-1E746925E7C0");

        public string Name
        {
            get { return "4 Roads - Rules and Actions Tokens"; }
        }

        public string Description
        {
            get { return "Registers Tokens used by the 4 roads rules and actions"; }
        }

        public void Initialize()
        {
        }

        /// <summary>
        /// Register a new token for accessing the rule action performed
        /// </summary>
        /// <param name="action"></param>
        /// <returns>bool</returns>
        /// 
        public void RegisterTokens(ITokenizedTemplateTokenController tokenController)
        {
            // Naming convention allows consistency in display in the token drop-down menu.
            tokenController.Register(new CustomTriggerParametersToken("CustomParameters: Action",
                "The action for the rule trigger event.",
                // A unique, static id is needed for each token
                Guid.Parse("5E171D28-BB01-4A2B-A293-5BEF7309A488"),
                CustomTriggerParametersTypeId,
                PrimitiveType.String,
                context => context.Get<CustomTriggerParameters>(CustomTriggerParametersTypeId).Action));
        }
    }
}
