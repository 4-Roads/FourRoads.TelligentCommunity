using System;

namespace FourRoads.TelligentCommunity.ForumLastPost.Interfaces
{
    public struct LastReadPostInfo
    {
        public int ReplyCount { get; set; }
        public Guid? ContentId { get; set; }
        public DateTime PostDate { get; set; }
    }
}