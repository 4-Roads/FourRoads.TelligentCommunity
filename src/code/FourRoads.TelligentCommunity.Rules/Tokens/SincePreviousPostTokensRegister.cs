using System;
using Telligent.Evolution.Extensibility.Templating.Version1;

namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    public class SincePreviousPostTokensRegister : ITokenRegistrar
    {
        public readonly Guid SincePreviousPostTriggerParametersTypeId = new Guid("762B1394-22B8-4F76-8962-235A46835859");

        public string Name
        {
            get { return "4 Roads - Token for Days Since Previous Post rule extended attributes"; }
        }

        public string Description
        {
            get { return "Registers Token to provide access to additional context values for use in thread reply rule conditions"; }
        }

        public void Initialize()
        {
        }

        /// <summary>
        /// Register a new token for accessing the number of days since the previous post to a thread when adding a new reply
        /// </summary>
        /// <param name="action"></param>
        /// <returns>bool</returns>
        /// 
        public void RegisterTokens(ITokenizedTemplateTokenController tokenController)
        {
            // Naming convention allows consistency in display in the token drop-down menu.
            tokenController.Register(new SincePreviousPostTriggerParametersToken("Forum Thread: Days Since Previous Post",
                "The numer of days since the previous post for a forum thread",
                // A unique, static id is needed for each token
                Guid.Parse("2DDE29CA-E973-4C16-984C-C9C67F5F0CE8"),
                SincePreviousPostTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<SincePreviousPostTriggerParameters>(SincePreviousPostTriggerParametersTypeId).DaysSincePreviousPost));

        }
    }
}
