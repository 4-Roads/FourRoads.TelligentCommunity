using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.ForumLastPost.Interfaces
{
    public interface ILastReadPostScriptedFragment
    {
        LastReadPostInfo LastReadPost(Guid appicationId, Guid contentId, int userId);
        string LastReadPostPagedUrl(Guid appicationId, Guid contentId, int userId);
    }
}
