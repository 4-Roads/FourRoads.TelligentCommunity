using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Job;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Services;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Data;
using Quartz;
using Telligent.Common;
using Telligent.Evolution.Api.Services;
using Telligent.Evolution.Blogs.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Components.Email;
using Telligent.Evolution.MailGateway.MailRoom.Components;
using Telligent.Evolution.MailGateway.MailRoom.Services;
using Telligent.Evolution.MediaGalleries.Components;
using Telligent.JobScheduler;
using ApiMedia = Telligent.Evolution.Extensibility.Api.Entities.Version1.Media;
using ApiMediaGallery = Telligent.Evolution.MediaGalleries.Components.MediaGallery;
using ApiMediaFilePost = Telligent.Evolution.Extensibility.Api.Entities.Version1.MediaFile;
using ApiMediaGalleryPost = Telligent.Evolution.MediaGalleries.Components.MediaGalleryPost;

namespace FourRoads.TelligentCommunity.FileNotification.Api.Internal.Handler
{
   public  class FileNotificationEmailHandler : IJob
    {

        private readonly IEmailChecks _iEmailChecks;
        private readonly IGalleryService _iGalleryService;
        private readonly IEmailTemplateUtility _iemailTemplateUtility;
        private readonly IFileSubscriptionDataService _iFileSubscriptionDataService;
        
       
        public FileNotificationEmailHandler()
            : this(Telligent.Common.Services.Get<IEmailChecks>(), Telligent.Common.Services.Get<IGalleryService>(), Telligent.Common.Services.Get<IEmailTemplateUtility>())
        {

        }

        public FileNotificationEmailHandler(IEmailChecks iEmailChecks, IGalleryService iGalleryService, IEmailTemplateUtility utility)
        {
            _iEmailChecks = iEmailChecks;
            _iGalleryService = iGalleryService;
            _iemailTemplateUtility = utility;
        }

        public FileNotificationEmailHandler(IFileSubscriptionDataService iFileSubscriptionDataService)
        {
             _iFileSubscriptionDataService = iFileSubscriptionDataService;
        }


        public void Execute(JobExecutionContext context)
        {
            int result;
            if (!_iEmailChecks.EmailEnabled() || !int.TryParse(context.JobDetail.JobDataMap[(object)"FileId"].ToString(), out result))
                return;
           // WeblogPost editablePost = WeblogPosts.GetEditablePost(result, false);

            ApiMediaGalleryPost galleryPost = MediaGalleryPosts.GetEditableFile(result);

            if (galleryPost == null || !galleryPost.Section.IsActive || (!galleryPost.IsApproved || galleryPost.PostDate > DateTime.Now))
                return;

            List<User> list = ListUsers(galleryPost);
            if (list == null || list.Count == 0)
                return;
            foreach (User user in list)
                JobsManager.Schedule<FileNotificationEmailJob>((IDictionary<string, string>)new Dictionary<string, string>()
                {
                      //{
                      //  "SectionId",
                      //  result.ToString()
                      //},

                      {
                        "FileId",
                        result.ToString()
                      },
                      {
                        "UserId",
                        user.UserID.ToString()
                      },
                      {
                        "FileSubscriptionId",
                        user.GetExtendedAttribute("SubscriptionID")
                      },
                      {
                        "Email",
                        _iemailTemplateUtility.FormatNameAndEmail(user.DisplayName, user.Email)
                      }
                });
        }

       
       private List<User> ListUsers(ApiMediaGalleryPost post)
        {
          var list = new List<User>();
          if (post == null || post.MediaGalleryPostType != MediaGalleryPostType.File)
            return list;

          PagedSet<User> firstFileSubscriptions =  _iFileSubscriptionDataService.GetEmailsFileSubscriptions(post.PostID, 0, EmailConstants.PageSize);
          list.AddRange((IEnumerable<User>) firstFileSubscriptions.Items);
          int totalItems = firstFileSubscriptions.TotalItems;
          int num1 = EmailConstants.PageSize;
          int num2 = 1;
          while (num1 < totalItems)
          {
            PagedSet<User> secondFileSubscriptions =  _iFileSubscriptionDataService.GetEmailsFileSubscriptions(post.PostID, num2++, EmailConstants.PageSize);
            num1 += EmailConstants.PageSize;
            list.AddRange((IEnumerable<User>) secondFileSubscriptions.Items);
          }
          return list;
        }


    }
}
