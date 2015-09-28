using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Entities;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.FileNotification.Interfaces.Data
{
    public interface IFileSubscriptionDataService
    {

        void UnsubscribeFromFile(Guid fileSubscriptionId, int fileId);
        Guid SubscribeToFile(FileSubscription fileSubscription);
        bool IsSubscribed(Guid fileSubscriptionId, int userId);
        Guid GetSubscriptionId(int fileId, int userId);
        PagedSet<User> GetEmailsFileSubscriptions(int postId, int pageIndex, int pageSize);

    }
}
