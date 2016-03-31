// //------------------------------------------------------------------------------
// // <copyright company="Four Roads LLC">
// //     Copyright (c) Four Roads LLC.  All rights reserved.
// // </copyright>
// //------------------------------------------------------------------------------

#region

using System;
using System.Linq;
using System.Web.Caching;
using Telligent.Common;
using FourRoads.Common.Interfaces;
using ICache = FourRoads.Common.Interfaces.ICache;
using Telligent.Evolution.Extensibility.Caching.Version1;

#endregion

namespace FourRoads.Common.TelligentCommunity.Components
{
// ReSharper disable InconsistentNaming
    public class TCCache : ICache
// ReSharper restore InconsistentNaming
    {
        #region ICache Members

        public void Insert(ICacheable value)
        {
            Insert(value ,value.CacheTags ?? new string[0]);
        }

        public void Insert(ICacheable value, string[] additionalTags)
        {
            TimeSpan cacheRefreshInterval;
            try
            {
                cacheRefreshInterval = new TimeSpan(0, 0, 0, value.CacheRefreshInterval);
            }
            catch
            {
                cacheRefreshInterval = TimeSpan.MinValue;
            }

            Insert(value.CacheID, value, (value.CacheTags ?? new string[0]).Union(additionalTags ?? new string[0]).ToArray(), cacheRefreshInterval, value.CacheScope);
        }

        public void Insert(string key, object value)
		{
            Insert(key, value, new string[0], TimeSpan.MinValue, CacheScopeOption.All);
		}

		public void Insert(string key, object value, string[] tags)
		{
            Insert(key, value, tags, TimeSpan.MinValue, CacheScopeOption.All);
		}

		public void Insert(string key, object value, TimeSpan timeout)
		{
            Insert(key, value, new string[0], timeout, CacheScopeOption.All);
		}

		public void Insert(string key, object value, string[] tags, TimeSpan timeout)
		{
            Insert(key, value, tags ?? new string[0], timeout, CacheScopeOption.All);
		}

        public void Insert(string key, object value, string[] tags, TimeSpan timeout, CacheScopeOption scope)
        {
            CacheService.Put(key, value, ConvertToLocalScope(scope), tags ?? new string[0], timeout);
        }

        private CacheScope ConvertToLocalScope(CacheScopeOption scope)
        {
            return (CacheScope)(int)scope;
        }

        public object Get(string key)
		{
            return CacheService.Get(key, CacheScope.All);
		}

        public T Get<T>(string key)
        {
            return (T)CacheService.Get(key, CacheScope.All);
		}

        public void Remove(string key)
        {
            CacheService.Remove(key, CacheScope.All);
        }

		public void RemoveByTags(string[] tags)
		{
            CacheService.RemoveByTags(tags, CacheScope.All);
		}

		public void Clear()
		{
            CacheService.RemoveByTags(new [] { "*"}, CacheScope.All);
		}

		#endregion
	}
}