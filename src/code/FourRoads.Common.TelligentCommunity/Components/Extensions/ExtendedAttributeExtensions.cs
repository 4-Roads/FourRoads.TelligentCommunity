using System;
using System.Linq;
using FourRoads.Common.Extensions;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;

namespace FourRoads.Common.TelligentCommunity.Components.Extensions
{
    public static class ExtendedAttributeExtensions
    {
        public static void SetAttribute<T>(this ApiList<IExtendedAttribute> attributes, string name, T value)
        {
            IExtendedAttribute attribute = attributes.FirstOrDefault(f => f.Key == name);

            if (attribute == null)
            {
                attribute = new ExtendedAttribute() { Key = name };
                attributes.Add(attribute);
            }

            attribute.Value = Convert.ToString(value);
        }

        public static string GetString(this ILookup<string, IExtendedAttribute> attributes, string name, string defaultValue = null)
        {
            return GetType(attributes, name, Convert.ToString, defaultValue);
        }

        public static int GetInt(this ILookup<string, IExtendedAttribute> attributes, string name, int defaultValue)
        {
            return GetType(attributes, name, Convert.ToInt32, defaultValue);
        }

        public static DateTime GetDateTime(this ILookup<string, IExtendedAttribute> attributes, string name, DateTime defaultValue)
        {
            return GetType(attributes, name, Convert.ToDateTime, defaultValue);
        }

        public static bool GetBool(this ILookup<string, IExtendedAttribute> attributes, string name, bool defaultValue)
        {
            return GetType(attributes, name, Convert.ToBoolean, defaultValue);
        }

        public static Byte[] GetBytes(this ILookup<string, IExtendedAttribute> attributes, string name, Byte[] defaultValue = null)
        {
            return GetType(attributes, name, s => s.ConvertToUTF8ByteArray(), defaultValue);
        }

        public static Guid GetGuid(this ILookup<string, IExtendedAttribute> attributes, string name, Guid defaultValue = default(Guid))
        {
            return GetType(attributes, name, s => new Guid(s), defaultValue);
        }

        private static T GetType<T>(ILookup<string, IExtendedAttribute> attributes, string name, Func<string, T> converter, T defaultValue = default(T))
        {
            T result = defaultValue;

            if (attributes.Contains(name))
            {
                var attribute = attributes.FirstOrDefault(n => Equals(n.Key, name)).FirstOrDefault();

                if (attribute != null)
                {
                    result = converter(attribute.Value);
                }
            }

            return result;
        }

    }
}