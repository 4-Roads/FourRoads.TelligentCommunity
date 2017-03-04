using System;

namespace FourRoads.TelligentCommunity.ForumLastPost.Interfaces
{
    public interface ILastReadPostScriptedFragment
    {
        LastReadPostInfo LastReadPost(Guid appicationId, Guid contentId, int userId);
        string LastReadPostPagedUrl(Guid appicationId, Guid contentId, int userId);
    }
}
