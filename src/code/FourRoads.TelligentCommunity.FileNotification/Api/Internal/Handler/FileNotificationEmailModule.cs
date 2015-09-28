using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Provider;
using Telligent.Evolution.Components;
using ApiMedia = Telligent.Evolution.Extensibility.Api.Entities.Version1.Media;

namespace FourRoads.TelligentCommunity.FileNotification.Api.Internal.Handler
{
   public  class FileNotificationEmailModule : ICSModule
    {
        public void Init(CSApplication csa, System.Xml.XmlNode node)
        {
            csa.PostPostUpdate += new CSPostEventHandler(this.Execute);
        }

        private void Execute(IContent content, CSPostEventArgs e)
        {
            ApiMedia post = content as ApiMedia;

            //if (post == null || e.State == ObjectState.Delete || post.Section.BlockSpamFeedbackNotifications && (post.PostStatus & PostStatus.Spam) == PostStatus.Spam || !post.EnableExternalNotificatons)
            //    return;
            //WeblogEmailsProvider.Instance().WeblogTracking(post);
            //var message = new FileNotificationEmails();
            //message.FileNotificationTracking(post);
        }
    }
}

