using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Entities = Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Jobs
{

    public class ResetNotificationsJob : IEvolutionJob
    {

        int pageSize = 100;

        public void Execute(JobData jobData)
        {
            Guid notificationTypeId = Guid.Parse(jobData.Data["NotificationTypeId"]);
            Guid distributionTypeId = Guid.Parse(jobData.Data["DistributionTypeId"]);
            bool enable = bool.Parse(jobData.Data["Enable"]);

            int userCount = PublicApi.Users.List(new UsersListOptions() { PageIndex = 0, PageSize = 1 }).TotalCount;
            int pages = userCount / pageSize;
            if(userCount % pageSize > 0)
            {
                pages++;
            }
            for (int i = 0; i < pages; i++)
            {
                Entities.PagedList<Entities.User> users = PublicApi.Users.List(new UsersListOptions() { PageIndex = i, PageSize = pageSize });
                foreach(Entities.User user in users)
                {
                    PublicApi.Users.RunAsUser(user.Id.Value, () => PublicApi.Notifications.UpdatePreference(notificationTypeId, distributionTypeId, enable));
                }
            }
        }

    }

}
