namespace FourRoads.TelligentCommunity.InstagramFeed.Models
{
    public class HashtagSearchRequest : BasePageSearchRequest
    {
        public string Query { get; set; }

        public bool MostRecent { get; set; }

        public bool HashtagSearchValid()
        {
            return !string.IsNullOrWhiteSpace(Query) && Valid();
        }
    }
}
