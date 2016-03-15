using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Entities = Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Jobs
{
    public class ResetNotificationsJob : IEvolutionJob
    {
        public void Execute(JobData jobData)
        {
            Guid notificationTypeId = Guid.Parse(jobData.Data["NotificationTypeId"]);
            Guid distributionTypeId = Guid.Parse(jobData.Data["DistributionTypeId"]);
            bool enable = bool.Parse(jobData.Data["Enable"]);

            UserEnumerate enumerateUsers = new UserEnumerate(null, null);

            foreach (Entities.User user in enumerateUsers)
            {
                PublicApi.Users.RunAsUser(user.Id.Value, () => PublicApi.Notifications.UpdatePreference(notificationTypeId, distributionTypeId, enable));
            }
        }
    }

}
