using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.ForumLastPost.Interfaces;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.ForumLastPost.ScriptedFragmentss
{
    public class LastReadPostScriptedFragment : ILastReadPostScriptedFragment
    {
        private ILastReadPostLogic _lastReadPostLogic;

        public LastReadPostScriptedFragment(ILastReadPostLogic lastReadPostLogic)
        {
            _lastReadPostLogic = lastReadPostLogic;
        }

        public LastReadPostInfo LastReadPost(Guid appicationId, Guid contentId, int userId)
        {
            return _lastReadPostLogic.GetLastReadPost(appicationId, contentId, userId);
        }

        [
        Documentation(Category = "Forum Extensions" , Description = @"

            Provides the ability for a widget to link to the last forum post that a user read sample -:

                #set($pagedUrl = $frcommon_v1_forumPost.LastReadPostPagedUrl($thread.Application.ApplicationId, $thread.ContentId, $core_v2_user.Accessing.Id)) 
                #if ($pagedUrl)
                <h4>
                    <a href=""$core_v2_encoding.HtmlAttributeEncode($pagedUrl)"">Last Read Post</a>
                </h4>
                #end "),
        ]
        public string LastReadPostPagedUrl(Guid appicationId, Guid contentId, int userId)
        {
            LastReadPostInfo lrp = LastReadPost(appicationId, contentId, userId);

            if (lrp.ContentId.HasValue && lrp.ContentId.Value != Guid.Empty)
            {
                var replies = PublicApi.ForumReplies.Get(lrp.ContentId.Value);

                if (replies != null)
                {
                    string url = PublicApi.ForumUrls.ForumReply(replies.Id.GetValueOrDefault(0));

                    if (lrp.ContentId.Value != Guid.Empty && lrp.ReplyCount > 0)
                        return url;
                }
            }
            return string.Empty;
        }
    }
}
