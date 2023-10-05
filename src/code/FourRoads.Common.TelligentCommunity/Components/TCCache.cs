// //------------------------------------------------------------------------------
// // <copyright company="4 Roads LTD">
// //     Copyright (c) 4 Roads LTD 2019.  All rights reserved.
// // </copyright>
// //------------------------------------------------------------------------------

#region

using System;
using System.Linq;
using System.Xml;
using FourRoads.Common.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
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
        readonly TimeSpan _defaulTimeSpan =  new TimeSpan(0, 0, 0, 30);

        public void Insert(ICacheable value)
        {
            Insert(value ,value.CacheTags ?? Array.Empty<string>());
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
                cacheRefreshInterval = _defaulTimeSpan;
            }

            Insert(value.CacheID, value, (value.CacheTags ?? Array.Empty<string>()).Union(additionalTags ?? Array.Empty<string>()).ToArray(), cacheRefreshInterval, value.CacheScope);
        }

        public void Insert(string key, object value)
		{
            Insert(key, value, Array.Empty<string>(), _defaulTimeSpan, CacheScopeOption.All);
		}

		public void Insert(string key, object value, string[] tags)
		{
            Insert(key, value, tags, _defaulTimeSpan, CacheScopeOption.All);
		}

		public void Insert(string key, object value, TimeSpan timeout)
		{
            Insert(key, value, Array.Empty<string>(), timeout, CacheScopeOption.All);
		}

		public void Insert(string key, object value, string[] tags, TimeSpan timeout)
		{
            Insert(key, value, tags ?? Array.Empty<string>(), timeout, CacheScopeOption.All);
		}

        public void Insert(string key, object value, string[] tags, TimeSpan timeout, CacheScopeOption scope)
        {
            CacheService.Put(key, value, ConvertToLocalScope(scope), tags ?? Array.Empty<string>(), timeout);
        }

        private CacheScope ConvertToLocalScope(CacheScopeOption scope)
        {
            return (CacheScope)(int)scope;
        }

        public object Get(string key)
        {
            try
            {
                return CacheService.Get(key, CacheScope.All);
            }
            catch (Exception ex)
            {
                Apis.Get<IExceptions>().Log(ex);

                return null;
            }
        }

        public T Get<T>(string key)
        {
            try
            {
                return (T)CacheService.Get(key, CacheScope.All);
            }
            catch (Exception ex)
            {
                Apis.Get<IExceptions>().Log(ex);

                return default(T);
            }
        }

        public void Remove(string key)
        {
            CacheService.Remove(key, CacheScope.All);
        }

		public void RemoveByTags(string[] tags)
		{
            CacheService.RemoveByTags(tags, CacheScope.All);
		}

        #endregion
	}
}