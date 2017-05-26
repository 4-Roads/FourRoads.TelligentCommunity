using System;
using FourRoads.TelligentCommunity.Rules.Tokens;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;

namespace FourRoads.TelligentCommunity.Rules.Helpers
{
    public static class UserTotalValues
    {
        public enum VoteType
        {
            UpVoteCount = 1,
            DownVoteCount = 2
        }

        private static UserTotalTokensRegister _userTotalTokens = new UserTotalTokensRegister();

        public static bool Votes(int userid, int replyId, int value , VoteType voteType)
        {
            var user = Apis.Get<IUsers>().Get(new UsersGetOptions() {Id = userid});

            if (!user.HasErrors())
            {
                // update the number of up/down votes a user has made globally
                int votes = 0;
                ExtendedAttribute userVotes = user.ExtendedAttributes.Get(voteType.ToString());
                if (userVotes != null)
                {
                    int.TryParse(userVotes.Value, out votes);
                }

                votes += value;

                if (userVotes == null)
                {
                    user.ExtendedAttributes.Add(new ExtendedAttribute()
                    {
                        Key = voteType.ToString(),
                        Value = Math.Max(0, votes).ToString()
                    });
                }
                else
                {
                    userVotes.Value = Math.Max(0, votes).ToString();
                }

                // update the number of upvotes the user has made against answers
                if (voteType == VoteType.UpVoteCount)
                {
                    var reply = Apis.Get<IForumReplies>().Get(replyId);

                    if (!reply.HasErrors() && (reply.IsAnswer ?? false))
                    {
                        votes = 0;
                        ExtendedAttribute userAnswerVotes = user.ExtendedAttributes.Get("AnswerUpVoteCount");
                        if (userAnswerVotes != null)
                        {
                            int.TryParse(userAnswerVotes.Value, out votes);
                        }

                        votes += value;

                        if (userAnswerVotes == null)
                        {
                            user.ExtendedAttributes.Add(new ExtendedAttribute()
                            {
                                Key = "AnswerUpVoteCount",
                                Value = Math.Max(0, votes).ToString()
                            });
                        }
                        else
                        {
                            userAnswerVotes.Value = Math.Max(0, votes).ToString();
                        }
                    }
                }

                Apis.Get<IUsers>()
                    .Update(new UsersUpdateOptions() {Id = user.Id, ExtendedAttributes = user.ExtendedAttributes});

                return true;
            }
            return false;
        }

        public static bool Tags(int userid, int value)
        {
            User user = Apis.Get<IUsers>().Get(new UsersGetOptions() { Id = userid });
            if (!user.HasErrors())
            {
                int tags = 0;
                ExtendedAttribute userTagCount = user.ExtendedAttributes.Get("TagCount");
                if (userTagCount != null)
                {
                    int.TryParse(userTagCount.Value, out tags);
                }

                tags += value;

                if (userTagCount == null)
                {
                    user.ExtendedAttributes.Add(new ExtendedAttribute()
                    {
                        Key = "TagCount",
                        Value = Math.Max(0, tags).ToString()
                    });
                }
                else
                {
                    userTagCount.Value = Math.Max(0, tags).ToString();
                }

                Apis.Get<IUsers>()
                    .Update(new UsersUpdateOptions() { Id = user.Id, ExtendedAttributes = user.ExtendedAttributes });

                return true;
            }
            return false;
        }

        public static void UpdateContext(RuleTriggerExecutionContext context, User user)
        {
            //get the extended user attributes
            UserTotalTriggerParameters userTotalTriggerParameters = new UserTotalTriggerParameters()
            {
                UpVoteCount = GetUserExtendedValue(user , "UpVoteCount"),
                DownVoteCount = GetUserExtendedValue(user , "DownVoteCount"),
                TagCount = GetUserExtendedValue(user , "TagCount")
            };
            userTotalTriggerParameters.VoteCount = (userTotalTriggerParameters.UpVoteCount +
                                                    userTotalTriggerParameters.DownVoteCount);
            context.Add(_userTotalTokens.UserTotalTriggerParametersTypeId, userTotalTriggerParameters);
        }

        private static int GetUserExtendedValue(User user, string key)
        {
            int value = 0;
            ExtendedAttribute extendedAttribute = user.ExtendedAttributes.Get(key);
            if (extendedAttribute != null)
            {
                int.TryParse(extendedAttribute.Value, out value);
            }
            return value;
        }

    }
}
