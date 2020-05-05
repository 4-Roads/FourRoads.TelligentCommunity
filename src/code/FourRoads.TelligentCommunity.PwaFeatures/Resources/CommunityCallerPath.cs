using System.Runtime.CompilerServices;
using FourRoads.Common.TelligentCommunity.Components.Interfaces;

namespace FourRoads.TelligentCommunity.PwaFeatures.Resources
{
    public class CommunityCallerPath : ICallerPathVistor
    {
        public string GetPath() => InternalGetPath();

        protected string InternalGetPath([CallerFilePath] string path = null) => path;
    }
}