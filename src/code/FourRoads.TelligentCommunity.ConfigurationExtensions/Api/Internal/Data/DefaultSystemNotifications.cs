using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;

using FourRoads.TelligentCommunity.ConfigurationExtensions.Api.Public.Entities;


namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Api.Internal.Data
{

    public class DefaultSystemNotifications
    {

        private static String Serialize(List<SystemNotificationPreference> tbs)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    XmlSerializer serializer = new XmlSerializer(tbs.GetType());
                    serializer.Serialize(ms, tbs);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch
            {
                return null;
            }
        }


        private static List<SystemNotificationPreference> Deserialize(String tbd)
        {
            List<SystemNotificationPreference> l = new List<SystemNotificationPreference>();
            XmlSerializer serializer = new XmlSerializer(l.GetType());
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(tbd)))
            {
                return (List<SystemNotificationPreference>)serializer.Deserialize(stream);
            }
        }

        public static List<SystemNotificationPreference> GetSystemNotificationPreferences()
        {
            Group rootGroup = Apis.Get<IGroups>().Root;
            List<SystemNotificationPreference> defaults = new List<SystemNotificationPreference>();
            if (rootGroup.ExtendedAttributes["DefaultSystemNotifications"] != null && !String.IsNullOrEmpty(rootGroup.ExtendedAttributes["DefaultSystemNotifications"].Value))
            {
                defaults = Deserialize(rootGroup.ExtendedAttributes["DefaultSystemNotifications"].Value);
            }
            /*
            rootGroup.ExtendedAttributes.Remove(rootGroup.ExtendedAttributes.Where(x => x.Key == "DefaultSystemNotifications").FirstOrDefault<ExtendedAttribute>());
            List<ExtendedAttribute> attributes = new List<ExtendedAttribute>(rootGroup.ExtendedAttributes);
            attributes.Add(new ExtendedAttribute() { Key = "DefaultSystemNotifications", Value = "" });
            Apis.Get<IGroups>().Update(rootGroup.Id.Value, new GroupsUpdateOptions() { ExtendedAttributes = attributes });
            rootGroup = Apis.Get<IGroups>().Get(new GroupsGetOptions() { Id = Apis.Get<IGroups>().Root.Id });
            */
            return defaults;
        }

        public static void UpdateSystemNotificationPreferences(List<SystemNotificationPreference> preferences)
        {
            Group rootGroup = Apis.Get<IGroups>().Root;
            rootGroup.ExtendedAttributes.Remove(rootGroup.ExtendedAttributes.Where(x => x.Key == "DefaultSystemNotifications").FirstOrDefault<ExtendedAttribute>());
            List<ExtendedAttribute> attributes = new List<ExtendedAttribute>(rootGroup.ExtendedAttributes);
            attributes.Add(new ExtendedAttribute() { Key = "DefaultSystemNotifications", Value = Serialize(preferences) });
            Apis.Get<IGroups>().Update(rootGroup.Id.Value, new GroupsUpdateOptions() { ExtendedAttributes = attributes });
        }

    }

}
