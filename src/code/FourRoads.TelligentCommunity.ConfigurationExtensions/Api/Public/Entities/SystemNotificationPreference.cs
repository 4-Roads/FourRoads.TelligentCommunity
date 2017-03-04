using System;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Api.Public.Entities
{

    public class SystemNotificationPreference
    {

        private Guid notificationTypeId;
        private Guid distributionTypeId;
        private bool enabled;

        public SystemNotificationPreference()
        {

        }

        public SystemNotificationPreference(Guid notificationTypeId, Guid distributionTypeId, bool enabled)
        {
            this.notificationTypeId = notificationTypeId;
            this.distributionTypeId = distributionTypeId;
            this.enabled = enabled;
        }

        public Guid NotificationTypeId
        {
            get
            {
                return notificationTypeId;
            }
            set
            {
                notificationTypeId = value;
            }
        }

        public Guid DistributionTypeId
        {
            get
            {
                return distributionTypeId;
            }
            set
            {
                distributionTypeId = value;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }

    }

}
