using System.Linq;
using FourRoads.Common.TelligentCommunity.Components.Extensions;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensions.Calendar.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.SubscriptionProcessors
{
    public class SubscriptionCalendarUpdateProcessor : SubscriptionUpdateProcessor
    {
        public override bool CanProcess(JobData jobData)
        {
            return jobData.Data.ContainsKey("processCalendars");
        }

        protected override void InternalProcess(User user, Group @group, JobData jobData)
        {
            int? calendarId = jobData.Data.ContainsKey("CalendarId") ? int.Parse(jobData.Data["CalendarId"]) : default(int?);

            CalendarEnumerate calendarEnumerate = new CalendarEnumerate(@group.Id.Value, calendarId);

            foreach (Calendar calendar in calendarEnumerate)
            {
                //Because calendars don't support extended attributes we need to store them on the group
                var lookups = calendar.Group.ExtendedAttributes.ToLookup(attribute => attribute.Key, val => (IExtendedAttribute)val);

                string setting = lookups.GetString("DefaultSubscriptionSetting" + calendar.NodeId, "unset");

                SetSubscriptionStatus(calendar.Id.Value, setting, user.Id.Value);
            }
        } 

        public override string ProcessorName
        {
            get { return "Calendar"; }
        }
         
        protected void SetSubscriptionStatus(int calendarId, string setting, int userId)
        {
            PublicApi.Users.RunAsUser(userId, () =>
            {
                if (setting.ToLower() == "subscribed")
                {
                    Telligent.Evolution.Extensions.Calendar.Api.PublicApi.CalendarSubscriptions.Subscribe(userId, calendarId);
                }
                else if (setting.ToLower() == "unsubscribed")
                {
                    Telligent.Evolution.Extensions.Calendar.Api.PublicApi.CalendarSubscriptions.Unsubscribe(userId, calendarId);
                }
            });
        }
    }
}