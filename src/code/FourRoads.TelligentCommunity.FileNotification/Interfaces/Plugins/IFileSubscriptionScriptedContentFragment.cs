using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Entities;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.FileNotification.Interfaces.Plugins
{
    public interface IFileSubscriptionScriptedContentFragment
    {
        Guid SubscribeToFile(int fileId, string email, int userId);
        Guid SubscribeAnonymously(string email, int fileId);
        bool IsSubscribed(Guid fileSubscriptionId, int userId);
        void UnsubscribeToFile(Guid fileSubscriptionId, int fileId);
        Guid GetSubscriptionId(int fileId, int userId);
    }
}
