namespace FourRoads.TelligentCommunity.InstagramFeed.Models
{
    public class BasePageSearchRequest
    {
        public string PageId { get; set; }

        /// <summary>
        /// Should be Permanent Page Access Token
        /// https://developers.facebook.com/docs/marketing-api/access#graph-api-explorer
        /// </summary>
        public string AccessToken { get; set; }

        public int? Limit { get; set; }

        public bool Valid()
        {
            return !string.IsNullOrWhiteSpace(PageId)
                && !string.IsNullOrWhiteSpace(AccessToken);
        }
    }
}
