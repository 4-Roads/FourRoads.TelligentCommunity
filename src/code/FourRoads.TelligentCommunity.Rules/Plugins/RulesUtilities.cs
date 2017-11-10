using FourRoads.TelligentCommunity.Rules.ThreadViews.Triggers;
using System;
using System.Collections.Generic;
using FourRoads.TelligentCommunity.Rules.Actions;
using Telligent.Evolution.Extensibility.Version1;
using FourRoads.TelligentCommunity.Rules.Triggers;

namespace FourRoads.TelligentCommunity.Rules.Plugins
{
    public class ThemeUtilities : IPluginGroup
    {
        #region IPlugin Members

        public string Description
        {
            get { return "A collection of plugins to implement additional rules for achievements"; }
        }

        public void Initialize()
        {
        }

        public string Name
        {
            get { return "4 Roads - Achievements - Rules utilities"; }
        }

        #endregion

        #region IPluginGroup Members

        public IEnumerable<Type> Plugins
        {
            get
            {
                return new[]
                {
                    typeof(ThreadView),
                    typeof(AbusiveContent),
                    typeof(AbusiveContentx3),
                    typeof(AbusiveContentx5),
                    typeof(ForumAnswerAccepted),
                    typeof(ForumAnswerRejected),
                    typeof(ForumReplyDeleted),
                    typeof(ForumReplyDownVoteCancel),
                    typeof(ForumReplyDownVoted),
                    typeof(ForumReplySincePreviousPost),
                    typeof(ForumReplyUpVoteCancel),
                    typeof(ForumReplyUpVoted),
                    typeof(ForumThreadTagged),
                    typeof(UserPresence),
                    typeof(UpdateUserReputationPoints)
                };
            }
        }

        #endregion
    }
}