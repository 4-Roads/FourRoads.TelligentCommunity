using System.IO;
using System.Reflection;
using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.CustomEditor
{
    internal sealed class EmbeddedResources : EmbeddedResourcesBase
    {
        private static readonly Assembly Assembly = typeof(EmbeddedResources).Assembly;

        private EmbeddedResources()
        {
        }

        internal static string GetString(string path)
        {
            return GetResourceString(Assembly, path);
        }

        internal static Stream GetStream(string path)
        {
            return GetResourceStream(Assembly, path);
        }
    }
}