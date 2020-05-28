using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IPagedList<T> : IEnumerable<T>
    {
        int Total { get; }
    }
}