using System;
using Telligent.Evolution.Extensibility.Templating.Version1;

namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    public class RegisterUpVoteTokens : ITokenRegistrar
    {
        public readonly Guid UpVoteTriggerParametersTypeId = new Guid("FC7E7736-84D4-41AB-BD27-1E746925E7C0");

        public string Name
        {
            get { return "4 Roads - Tokens for Up Vote rule extended attributes"; }
        }

        public string Description
        {
            get { return "Registers Tokens to provide access to additional context values for use in up vote rule conditions"; }
        }

        public void Initialize()
        {
        }

        /// <summary>
        /// Register a new token for accessing the total up votes for a reply
        /// </summary>
        /// <param name="action"></param>
        /// <returns>bool</returns>
        /// 
        public void RegisterTokens(ITokenizedTemplateTokenController tokenController)
        {
            // Naming convention allows consistency in display in the token drop-down menu.
            tokenController.Register(new UpVoteTriggerParametersToken("Forum Reply: Total Up Votes",
                "The total up votes for a forum reply",
                // A unique, static id is needed for each token
                Guid.Parse("5E171D28-BB01-4A2B-A293-5BEF7309A488"),
                UpVoteTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<UpVoteTriggerParameters>(UpVoteTriggerParametersTypeId).ReplyUpVotes));

            tokenController.Register(new UpVoteTriggerParametersToken("Forum Thread: Total Up Votes",
                "The total up votes for a forum thread",
                // A unique, static id is needed for each token
                Guid.Parse("E4A10C42-CB8C-49C9-AAA1-EDCB9037F670"),
                UpVoteTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<UpVoteTriggerParameters>(UpVoteTriggerParametersTypeId).ThreadUpVotes));
        }
    }
}
