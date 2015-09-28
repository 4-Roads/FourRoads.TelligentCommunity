using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using Quartz.Util;
using Telligent.Common;
using Telligent.Evolution.Api.Services;
using Telligent.Evolution.Api.Entities.Mappers;
using Telligent.Evolution.Api.Services;
using Telligent.Evolution.Components;
using Telligent.Evolution.Components.Email;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.MailGateway.MailRoom.Components;
using Telligent.Evolution.MailGateway.MailRoom.Services;
using Telligent.Evolution.MediaGalleries.Components;
using Telligent.Evolution.Rest.Resources.Media;
using ApiMedia = Telligent.Evolution.Extensibility.Api.Entities.Version1.Media;

namespace FourRoads.TelligentCommunity.FileNotification.Api.Internal.Job
{
    public class FileNotificationEmailJob : IJob
    {
        private readonly IEmailChecks _iEmailChecks;
        private readonly IGalleryService _iGalleryService;
        private readonly IMediaService _iMediaService;
        private readonly IContextService _iContextService;
        private readonly IEmailContextService _iEmailContextService;
        private readonly IEmailTemplateService _iEmailTemplateService;
        private readonly IEmailTemplateUtility _iEmailTemplateUtility;
        private readonly IEmailQueuingService _iEmailQueuingService;
        private readonly IMapper<Telligent.Evolution.Components.User, Telligent.Evolution.Extensibility.Api.Entities.Version1.User> _iMapper;


        public FileNotificationEmailJob()
            : this(Telligent.Common.Services.Get<IEmailChecks>(), Telligent.Common.Services.Get<IGalleryService>(), Telligent.Common.Services.Get<IMediaService>(), Telligent.Common.Services.Get<IContextService>(),
            Telligent.Common.Services.Get<IEmailContextService>(), Telligent.Common.Services.Get<IEmailTemplateService>(), Telligent.Common.Services.Get<IEmailTemplateUtility>(),
            Telligent.Common.Services.Get<IEmailQueuingService>(), Telligent.Common.Services.Get<IMapper<Telligent.Evolution.Components.User, Telligent.Evolution.Extensibility.Api.Entities.Version1.User>>())
        {

        }

        public FileNotificationEmailJob(IEmailChecks iEmailChecks, IGalleryService iGalleryService, IMediaService iMediaService, IContextService iContextService,
            IEmailContextService iEmailContextService, IEmailTemplateService iEmailTemplateService, IEmailTemplateUtility iEmailTemplateUtility,
            IEmailQueuingService iEmailQueuingService, IMapper<Telligent.Evolution.Components.User, Telligent.Evolution.Extensibility.Api.Entities.Version1.User> iMapper )
        {
            _iEmailChecks = iEmailChecks;
            _iGalleryService = iGalleryService;
            _iContextService = iContextService;
            _iEmailContextService = iEmailContextService;
            _iEmailTemplateService = iEmailTemplateService;
            _iEmailTemplateUtility = iEmailTemplateUtility;
            _iEmailQueuingService = iEmailQueuingService;
            _iMapper = iMapper;
            _iMediaService = iMediaService;
        }

        
        public void Execute(JobExecutionContext context)
        {
            int fileId = int.Parse(((DirtyFlagMap)context.JobDetail.JobDataMap)["FileId"].ToString());
            //int sectionId = int.Parse(((DirtyFlagMap)context.JobDetail.JobDataMap)["SectionId"].ToString());
            int userId = int.Parse(((DirtyFlagMap)context.JobDetail.JobDataMap)["UserId"].ToString());
            string filesubscriptionId = ((DirtyFlagMap)context.JobDetail.JobDataMap)["FileSubscriptionId"].ToString();
            string email = ((DirtyFlagMap)context.JobDetail.JobDataMap)["Email"].ToString();
            int index = ((DirtyFlagMap)context.JobDetail.JobDataMap).Contains((object)"SubscriptionFrequency") ? int.Parse(((DirtyFlagMap)context.JobDetail.JobDataMap)["SubscriptionFrequency"].ToString()) : 0;


            //get gallery and media 

            ApiMedia mediaPost = _iGalleryService.GetMedia(new int?(), fileId);
            Gallery gallery = _iGalleryService.GetGallery(mediaPost.Id, (string) null, null);

            Telligent.Evolution.Components.User firstUser = Telligent.Evolution.Users.GetUser(userId);
            firstUser.SetExtendedAttribute("FileSubscriptionId", filesubscriptionId);
            Telligent.Evolution.Extensibility.Api.Entities.Version1.User secondUser = _iMapper.ConvertToApi(firstUser);

            var classFilenotification = new ClassFilenotification();
            classFilenotification.FileNotificationEmailJob = this;
            classFilenotification.iemailContext = _iEmailContextService.Create(new string[1]
                {
                  email
                }, _iEmailTemplateUtility.FormatFromSite(),
                    (secondUser.EnableHtmlEmail.GetValueOrDefault(true) ? 1 : 0) != 0, secondUser.Language, (object)mediaPost, (object)gallery, (object)secondUser);

            // create the email template for file notification message probably : 'file_notification_message'
            classFilenotification.emailTemplate = _iEmailTemplateService.Get("file_notification_message", secondUser.Language);
            _iContextService.GetExecutionContext().RunAsUser(firstUser.UserID, new Action(classFilenotification.SendNotification));

        }

       private sealed class ClassFilenotification
        {
            public IEmailContext iemailContext;
            public EmailTemplate emailTemplate;
            public FileNotificationEmailJob FileNotificationEmailJob;
            public void SendNotification()
            {
                FileNotificationEmailJob._iEmailQueuingService.Queue(iemailContext, emailTemplate);
            }
        }
    }
}
