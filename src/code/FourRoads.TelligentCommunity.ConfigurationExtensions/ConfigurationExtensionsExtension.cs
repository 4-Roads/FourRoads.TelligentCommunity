using System.Collections.Generic;
using System.Linq;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Interfaces;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Api.Public.Entities;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Api.Internal.Data;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions
{
    public class ConfigurationExtensionsExtension : IScriptedContentFragmentExtension, IApplicationPlugin
    {
        public void Initialize()
        {
            Apis.Get<IUsers>().Events.AfterCreate += Events_UserAfterCreate;
        }

        private void Events_UserAfterCreate(UserAfterCreateEventArgs args)
        {
            List<SystemNotificationPreference> defaults = DefaultSystemNotifications.GetSystemNotificationPreferences();
            if (defaults != null && defaults.Count > 0)
            {
                ApiList<NotificationDistributionTypeInfo> distributionTypes = Apis.Get<INotifications>().ListDistributionTypes();
                ApiList<NotificationTypeInfo> notificationTypes = Apis.Get<INotifications>().ListNotificationTypes();
                foreach (NotificationTypeInfo notificationType in notificationTypes)
                {
                    foreach (NotificationDistributionTypeInfo distributionType in distributionTypes)
                    {
                        SystemNotificationPreference preference = (
                            from p in defaults
                            where p.NotificationTypeId == notificationType.NotificationTypeId && p.DistributionTypeId == distributionType.DistributionTypeId
                            select p).FirstOrDefault();
                        if (preference != null)
                        {
                            Apis.Get<IUsers>().RunAsUser(args.Id.Value, () => Apis.Get<INotifications>().UpdatePreference(preference.NotificationTypeId, preference.DistributionTypeId, preference.IsEnabled));
                        }
                        else
                        {
                            Apis.Get<IUsers>().RunAsUser(args.Id.Value, () => Apis.Get<INotifications>().UpdatePreference(notificationType.NotificationTypeId, distributionType.DistributionTypeId, false));
                        }
                    }
                }
            }
        }

        public string Name {
            get { return "4 Roads - Scripted Fragment Extension (frcommon_v1_configurationExtensions)"; }
        }
        public string Description {
            get { return "Provides access to settings and configuration needed for advanced functionlity"; }
        }
        public string ExtensionName {
            get { return "frcommon_v1_configurationExtensions"; }
        }

        public object Extension
        {
            get
            {
                return new ConfigurationExtensions();
            }
        }
    }
}
