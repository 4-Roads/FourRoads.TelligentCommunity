using System.Linq;
using FourRoads.Common.TelligentCommunity.Components.Extensions;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.SubscriptionProcessors
{

    public class GroupDigestUpdateProcessor : SubscriptionUpdateProcessor
    {

        public override bool CanProcess(JobData jobData)
        {
            return jobData.Data.ContainsKey("processGroups");
        }

        protected override void InternalProcess(User user, Group @group, JobData jobData)
        {
            GroupEnumerate groupEnumerate = new GroupEnumerate(@group.Id.Value);

            foreach (Group g in groupEnumerate)
            {
                var lookups = g.ExtendedAttributes.ToLookup(attribute => attribute.Key, val => (IExtendedAttribute)val);

                string setting = lookups.GetString("DefaultDigestSetting", "unset");

                if ("daily".Equals(setting))
                {
                    PublicApi.Users.RunAsUser(user.Id.Value, () =>
                    {
                        EmailDigestSubscription subscription = PublicApi.EmailDigestSubscriptions.GetByGroup(@group.Id.Value);
                        if (subscription != null)
                        {
                            PublicApi.EmailDigestSubscriptions.Update(subscription.Id.Value, 1);
                        }
                        else
                        {
                            PublicApi.EmailDigestSubscriptions.Create("group", @group.Id.Value, 1);
                        }
                    });
                }
                else if ("weekly".Equals(setting))
                {
                    PublicApi.Users.RunAsUser(user.Id.Value, () =>
                    {
                        EmailDigestSubscription subscription = PublicApi.EmailDigestSubscriptions.GetByGroup(@group.Id.Value);
                        if (subscription != null)
                        {
                            PublicApi.EmailDigestSubscriptions.Update(subscription.Id.Value, 7);
                        }
                        else
                        {
                            PublicApi.EmailDigestSubscriptions.Create("group", @group.Id.Value, 7);
                        }
                    });
                }
                else if("off".Equals(setting))
                {
                    PublicApi.Users.RunAsUser(user.Id.Value, () =>
                    {
                        EmailDigestSubscription subscription = PublicApi.EmailDigestSubscriptions.GetByGroup(@group.Id.Value);
                        if (subscription != null)
                        {
                            PublicApi.EmailDigestSubscriptions.Delete(subscription.Id.Value);
                        }
                    });
                }
            }
        }

        public override string ProcessorName
        {
            get { return "Group"; }
        }

    }

}
