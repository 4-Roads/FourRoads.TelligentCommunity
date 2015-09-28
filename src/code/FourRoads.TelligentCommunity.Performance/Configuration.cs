using System;

namespace FourRoads.TelligentCommunity.Performance
{
    public static class Configuration
    {
        public static bool OptomizeGlobalJs { get; set; }
        public static bool OptomizeWidgetJs { get; set; }
        public static bool OptomizeGlobalCss { get; set; }
        public static TimeSpan BundleTimeout { get; set; }
    }
}