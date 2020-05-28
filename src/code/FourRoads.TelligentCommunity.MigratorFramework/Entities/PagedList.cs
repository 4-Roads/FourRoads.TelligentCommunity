using System.Collections.Generic;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;

namespace FourRoads.TelligentCommunity.MigratorFramework.Entities
{
    public class PagedList<T> :  List<T>, IPagedList<T>
    {
        public int Total { get; set; }
    }
}