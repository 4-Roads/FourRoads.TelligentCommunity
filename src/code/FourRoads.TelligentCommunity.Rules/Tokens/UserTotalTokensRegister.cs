using System;
using Telligent.Evolution.Extensibility.Templating.Version1;

namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    public class UserTotalTokensRegister : ITokenRegistrar
    {
        public readonly Guid UserTotalTriggerParametersTypeId = new Guid("F9210F64-ECE5-4CE2-B50E-2B04DCDD9D55");

        public string Name
        {
            get { return "4 Roads - Tokens for User System Totals"; }
        }

        public string Description
        {
            get { return "Registers tokens to provide access to extended attributes containing user totals for use in rule conditions"; }
        }

        public void Initialize()
        {
        }

        /// <summary>
        /// Register new tokens for accessing user summary/total values held in their extended properties
        /// </summary>
        /// <param name="action"></param>
        /// <returns>bool</returns>
        /// 
        public void RegisterTokens(ITokenizedTemplateTokenController tokenController)
        {
            // Naming convention allows consistency in display in the token drop-down menu.
            tokenController.Register(new UserTotalTriggerParametersToken("User: Total Up Votes",
                "The number of up votes made by the user",
                // A unique, static id is needed for each token
                Guid.Parse("C5A27094-A444-4D2E-8593-50BF25B2CE86"),
                UserTotalTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<UserTotalTriggerParameters>(UserTotalTriggerParametersTypeId).UpVoteCount));

            tokenController.Register(new UserTotalTriggerParametersToken("User: Total Down Votes",
                "The number of down votes made by the user",
                // A unique, static id is needed for each token
                Guid.Parse("E456C124-76EB-4E13-9A12-FFC329B857DC"),
                UserTotalTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<UserTotalTriggerParameters>(UserTotalTriggerParametersTypeId).DownVoteCount));

            tokenController.Register(new UserTotalTriggerParametersToken("User: Total Votes",
                "The number of votes made by the user",
                // A unique, static id is needed for each token
                Guid.Parse("0BB5914A-62D2-4222-9217-D8691B0C045B"),
                UserTotalTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<UserTotalTriggerParameters>(UserTotalTriggerParametersTypeId).VoteCount));

            tokenController.Register(new UserTotalTriggerParametersToken("User: Total Tags",
                "The number of tags used by a user",
                // A unique, static id is needed for each token
                Guid.Parse("5A810C19-F675-4319-B57D-6522C27AC23E"),
                UserTotalTriggerParametersTypeId,
                PrimitiveType.Int,
                context => context.Get<UserTotalTriggerParameters>(UserTotalTriggerParametersTypeId).TagCount));
        }
    }
}
