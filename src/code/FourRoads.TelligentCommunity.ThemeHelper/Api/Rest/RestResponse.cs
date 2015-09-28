using Telligent.Evolution.Extensibility.Rest.Version2;

namespace FourRoads.TelligentCommunity.DeveloperTools.Api.Rest
{
    public class RestResponse: IRestResponse
    {
        public object Data { get; set; }
        public string[] Errors { get; set; }
        public string Name { get; set; }
    }
}