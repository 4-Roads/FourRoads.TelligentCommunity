using System.Linq;
using FourRoads.Common.TelligentCommunity.Components.Extensions;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.SubscriptionProcessors
{
    public class SubscriptionForumUpdateProcessor : SubscriptionUpdateProcessor
    {
        public override bool CanProcess(JobData jobData)
        {
            return jobData.Data.ContainsKey("processForums");
        }

        protected override void InternalProcess(User user, Group @group, JobData jobData)
        {
            int? forumId = jobData.Data.ContainsKey("ForumId") ? int.Parse(jobData.Data["ForumId"]) : default(int?);

            ForumEnumerate forumEnumerate = new ForumEnumerate(@group.Id.Value, forumId);

            foreach (Forum forum in forumEnumerate)
            {
                var lookups = forum.ExtendedAttributes.ToLookup(attribute => attribute.Key, val => (IExtendedAttribute)val);

                string setting = lookups.GetString("DefaultSubscriptionSetting", "unset");

                SetSubscriptionStatus(forum.ApplicationId, Apis.Get<IForums>().ApplicationTypeId, setting, user.Id.Value);
            }
        }

        public override string ProcessorName
        {
            get { return "Forum"; }
        }
    }
}