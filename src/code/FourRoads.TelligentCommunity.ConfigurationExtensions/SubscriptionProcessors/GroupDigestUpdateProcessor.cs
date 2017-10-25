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
                    Apis.Get<IUsers>().RunAsUser(user.Id.Value, () =>
                    {
                        EmailDigestSubscription subscription = Apis.Get<IEmailDigestSubscriptions>().GetByGroup(@group.Id.Value);
                        if (subscription != null)
                        {
                            Apis.Get<IEmailDigestSubscriptions>().Update(subscription.Id.Value, 1);
                        }
                        else
                        {
                            Apis.Get<IEmailDigestSubscriptions>().Create("group", @group.Id.Value, 1);
                        }
                    });
                }
                else if ("weekly".Equals(setting))
                {
                    Apis.Get<IUsers>().RunAsUser(user.Id.Value, () =>
                    {
                        EmailDigestSubscription subscription = Apis.Get<IEmailDigestSubscriptions>().GetByGroup(@group.Id.Value);
                        if (subscription != null)
                        {
                            Apis.Get<IEmailDigestSubscriptions>().Update(subscription.Id.Value, 7);
                        }
                        else
                        {
                            Apis.Get<IEmailDigestSubscriptions>().Create("group", @group.Id.Value, 7);
                        }
                    });
                }
                else if("off".Equals(setting))
                {
                    Apis.Get<IUsers>().RunAsUser(user.Id.Value, () =>
                    {
                        EmailDigestSubscription subscription = Apis.Get<IEmailDigestSubscriptions>().GetByGroup(@group.Id.Value);
                        if (subscription != null)
                        {
                            Apis.Get<IEmailDigestSubscriptions>().Delete(subscription.Id.Value);
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
