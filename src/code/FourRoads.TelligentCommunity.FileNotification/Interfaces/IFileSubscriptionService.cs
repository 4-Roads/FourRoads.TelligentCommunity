using System;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Entities;

namespace FourRoads.TelligentCommunity.FileNotification.Interfaces
{
    public interface IFileSubscriptionService
    {
        void UnsubscribeFromFile(Guid fileSubscriptionId, int fileId);
        Guid SubscribeToFile(FileSubscription fileSubscription);
        bool IsSubscribed(Guid fileSubscriptionId, int userId);
        Guid GetSubscriptionId(int fileId, int userId);
    }
}