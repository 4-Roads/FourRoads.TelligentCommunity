using FourRoads.TelligentCommunity.InstagramFeed.Models;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;

namespace FourRoads.TelligentCommunity.InstagramFeed.Interfaces
{
    public interface IMediaSearch
    {
        void Initialize();

        void RegisterUrls(IUrlController controller);

        List<Media> GetMediaByHashtag(HashtagSearchRequest request);

        List<Media> GetUserMedia(BasePageSearchRequest pageRequest);
    }
}
