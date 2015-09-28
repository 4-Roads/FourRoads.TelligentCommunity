using System;

namespace FourRoads.TelligentCommunity.DeveloperTools
{
    [Flags]
    public enum ReversionType
    {
        None = 0,
        Layouts = 1,
        HeadersAndFooters = 2,
        Configuration = 4,
        Files = 8,
        ScopedProperties = 16,
        All = Layouts | HeadersAndFooters | Configuration | Files | ScopedProperties
    }
}