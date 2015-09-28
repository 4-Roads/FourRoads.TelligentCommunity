using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.FileNotification.Api.Internal.Entities
{
    public class FileSubscription : ApiEntity
    {
        public Guid? FileSubscriptionId { get; set; }
        public int FileId { get; set; }
        public string Email { get; set; }
        public EmailSubscriptionType SubscriptionType { get; set; }
        public int UserId { get; set; }
        public bool IsConfirmed { get; set; }
    }
}
