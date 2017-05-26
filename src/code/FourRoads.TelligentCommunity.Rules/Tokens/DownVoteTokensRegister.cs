using System;
using Telligent.Evolution.Extensibility.Templating.Version1;

namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    public class DownVoteTokensRegister : ITokenRegistrar
    {
        public readonly Guid DownVoteTriggerParametersTypeId = new Guid("EC0164C4-3EAA-4A1D-9C4D-69D13F184505");

        public string Name
        {
            get { return "4 Roads - Tokens for Down Vote rule extended attributes"; }
        }

        public string Description
        {
            get { return "Registers Tokens to provide access to additional context values for use in down vote rule conditions"; }
        }

        public void Initialize()
        {
        }

        /// <summary>
        /// Register a new token for accessing the total down votes for a reply
        /// </summary>
        /// <param name="action"></param>
        /// <returns>bool</returns>
        /// 
        public void RegisterTokens(ITokenizedTemplateTokenController tokenController)
        {
            // Naming convention allows consistency in display in the token drop-down menu.
            tokenController.Register(new DownVoteTriggerParametersToken("Forum Reply: Total Down Votes",
                "The total down votes for a forum reply",
                // A unique, static id is needed for each token
                Guid.Parse("5D217DC0-35E3-430E-B592-C0C3C32F23D3"),
                DownVoteTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<DownVoteTriggerParameters>(DownVoteTriggerParametersTypeId).ReplyDownVotes));

            tokenController.Register(new DownVoteTriggerParametersToken("Forum Thread: Total Down Votes",
                "The total down votes for a forum thread",
                // A unique, static id is needed for each token
                Guid.Parse("3383A99D-EF9F-427E-B1CD-656A20BE32BC"),
                DownVoteTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<DownVoteTriggerParameters>(DownVoteTriggerParametersTypeId).ThreadDownVotes));
        }
    }
}
