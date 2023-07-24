using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SortOrder = System.Data.SqlClient.SortOrder;

namespace FourRoads.Common.TelligentCommunity.Components
{
    /// <summary>
    /// We will continue to use our own cache key generation code, if the CacheKeyAttribute becomes obsolete 
    /// we will add special custom version that works as we need
    /// </summary>
    public abstract class TCPagedQuery : PagedQueryBase
    {
        private static readonly Type _typeCacheKeyAttr = typeof(CacheKeyAttribute);
        private static readonly Type _typeObsoleteAttr = typeof(ObsoleteAttribute);

        internal struct CacheKeyPropertyInfo
        {
            internal object DefaultValue;
            internal CacheKeyAttribute CacheKeyAttribute;
            internal PropertyInfo PropertyInfo;
            internal FieldInfo FieldInfo;
        }

        private static ThreadSafeDictionary<Type, List<CacheKeyPropertyInfo>> _cacheKeyInforamtion = new ThreadSafeDictionary<Type, List<CacheKeyPropertyInfo>>();

        public TCPagedQuery()
        {
            EnsureInitialized();
        }

        [CacheKey("pi")]
        public override uint PageIndex
        {
            get { return base.PageIndex; }
            set { base.PageIndex = value; }
        }

        [CacheKey("ps")]
        public override int PageSize
        {
            get { return base.PageSize; }
            set { base.PageSize = value; }
        }

        [CacheKey("so")]
        public override SortOrder SortOrder
        {
            get { return base.SortOrder; }
            set { base.SortOrder = value; }
        }

        protected void EnsureInitialized()
        {
            _cacheKeyInforamtion.AddIfDoesntContainFunc(GetType(), () =>
            {
                List<CacheKeyPropertyInfo> info = new List<CacheKeyPropertyInfo>();

                PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

                foreach (PropertyInfo pi in properties)
                {
                    if (!pi.IsDefined(_typeObsoleteAttr, true))
                    {
                        CacheKeyAttribute cka = GetFirstCacheKeyAttribute(pi);
                        if (pi.CanRead && !IsIgnored(cka, pi) && cka != null)
                        {
                            info.Add(new CacheKeyPropertyInfo() { CacheKeyAttribute = cka, PropertyInfo = pi, DefaultValue = cka.DefaultValue });
                        }
                    }
                }

                FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (FieldInfo fi in fields)
                {
                    if (!fi.IsDefined(_typeObsoleteAttr, true))
                    {
                        CacheKeyAttribute cka = GetFirstCacheKeyAttribute(fi);
                        if (fi.IsPublic && !IsIgnored(cka, fi) && cka != null)
                        {
                            info.Add(new CacheKeyPropertyInfo() { CacheKeyAttribute = cka, FieldInfo = fi, DefaultValue = cka.DefaultValue });
                        }
                    }
                }

                return info;
            });
        }

        protected string GetCacheKey()
        {
            var sb = new StringBuilder();

            sb.Append("-").Append(GetType().FullName).Append("-").Append(ProcessCacheKeys());
#if DEBUG
            string cacheKey = sb.ToString();

            System.Diagnostics.Debug.WriteLine(cacheKey);

            return cacheKey;
#else

            return sb.ToString();
#endif
        }

        private string ProcessCacheKeys()
        {
            //Just to be safe
            EnsureInitialized();
            List<string> cachekeys = new List<string>();
            List<CacheKeyPropertyInfo> propInfo = _cacheKeyInforamtion[GetType()];

            foreach (CacheKeyPropertyInfo cacheKeyPropertyInfo in propInfo)
            {
                if (cacheKeyPropertyInfo.PropertyInfo != null)
                {
                    SetKeyAndObject(cacheKeyPropertyInfo.CacheKeyAttribute, cacheKeyPropertyInfo.DefaultValue, cachekeys, cacheKeyPropertyInfo.PropertyInfo, cacheKeyPropertyInfo.PropertyInfo.GetValue(this, null));
                }
                else
                {
                    SetKeyAndObject(cacheKeyPropertyInfo.CacheKeyAttribute, cacheKeyPropertyInfo.DefaultValue, cachekeys, cacheKeyPropertyInfo.FieldInfo, cacheKeyPropertyInfo.FieldInfo.GetValue(this));
                }

            }

            cachekeys.Sort();

            return string.Join("-", cachekeys.ToArray());
        }

        private void SetKeyAndObject(CacheKeyAttribute cka, object defaultValue, List<string> cachekeys, MemberInfo mi, object baseObjectValue)
        {
            if (defaultValue != baseObjectValue)
            {
                if (baseObjectValue is IEnumerable && !(baseObjectValue is string))
                {
                    List<string> values = new List<string>();

                    foreach (var listItem in (IEnumerable)baseObjectValue)
                    {
                        values.Add(listItem.ToString());
                    }

                    values.Sort();

                    cachekeys.Add(KeyName(cka, mi) + ":" + string.Join("~", values));
                }
                else
                {
                    cachekeys.Add(KeyName(cka, mi) + ":" + (baseObjectValue != null ? baseObjectValue.ToString() : string.Empty));
                }
              
            }
        }

        private static string KeyName(CacheKeyAttribute cka, MemberInfo mi)
        {
            if (cka != null && mi.IsDefined(_typeCacheKeyAttr, true))
                return cka.KeyName;

            return mi.Name;
        }

        private bool IsIgnored(CacheKeyAttribute cka, MemberInfo mi)
        {
            if (cka != null && mi.IsDefined(_typeCacheKeyAttr, true))
                return cka.Ignored;

            return false;
        }

        private CacheKeyAttribute GetFirstCacheKeyAttribute(MemberInfo mi)
        {
            object[] ca = mi.GetCustomAttributes(_typeCacheKeyAttr, true);

            if (ca != null)
            {
                for (int i = 0; i < ca.Length; i++)
                {
                    CacheKeyAttribute cka = ca[i] as CacheKeyAttribute;
                    if (cka != null)
                        return cka;
                }
            }

            return null;
        }
    }
}