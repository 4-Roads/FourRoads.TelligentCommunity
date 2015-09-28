using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Handler;
using Telligent.Evolution.Api.Entities.Mappers;
using Telligent.Evolution.Api.Services;
using Telligent.Evolution.Components.Email;
using Telligent.Evolution.MailGateway.MailRoom.Services;
using Telligent.JobScheduler;
using ApiMedia = Telligent.Evolution.Extensibility.Api.Entities.Version1.Media;
using ApiMediaGallery = Telligent.Evolution.MediaGalleries.Components.MediaGallery;
using ApiMediaFilePost = Telligent.Evolution.Extensibility.Api.Entities.Version1.MediaFile;
using ApiMediaGalleryPost = Telligent.Evolution.MediaGalleries.Components.MediaGalleryPost;

namespace FourRoads.TelligentCommunity.FileNotification.Api.Internal.Provider
{
    public class FileNotificationEmails
    {
        private readonly IEmailContextService _iemailContextService;
        private readonly IEmailTemplateService _iemailTemplateService;
        private readonly IUserService _iuserService;
        private readonly IEmailTemplateUtility _iemailTemplateUtility;
        private readonly IEmailQueuingService _iemailQueuingService;
        private readonly IMapper<Telligent.Evolution.Components.User, Telligent.Evolution.Extensibility.Api.Entities.Version1.User> _imapper;
        private readonly IMapper<ApiMediaGallery, ApiMedia> _imapper1;
        private readonly IMapperWithOptions<ApiMediaGalleryPost, ApiMediaFilePost> _imapperWithOptions;
        private readonly Telligent.Evolution.Api.Services.ICommentService _icommentService;
        private readonly IMapper<Telligent.Evolution.CoreServices.Comments.Comment, Telligent.Evolution.Extensibility.Api.Entities.Version1.Comment> _imapper2;
        private readonly IGalleryService _igalleryService;
        private readonly IEmailChecks _iemailChecks;

        public FileNotificationEmails()
            : this(Telligent.Common.Services.Get<IEmailContextService>(), Telligent.Common.Services.Get<IEmailTemplateService>(), Telligent.Common.Services.Get<IUserService>(), Telligent.Common.Services.Get<IEmailTemplateUtility>(),
              Telligent.Common.Services.Get<IEmailQueuingService>(), Telligent.Common.Services.Get<IMapper<Telligent.Evolution.Components.User, Telligent.Evolution.Extensibility.Api.Entities.Version1.User>>(),
              Telligent.Common.Services.Get<IMapper<ApiMediaGallery, ApiMedia>>(), Telligent.Common.Services.Get<IMapperWithOptions<ApiMediaGalleryPost, ApiMediaFilePost>>(),
              Telligent.Common.Services.Get<Telligent.Evolution.Api.Services.ICommentService>(), Telligent.Common.Services.Get<IMapper<Telligent.Evolution.CoreServices.Comments.Comment,
              Telligent.Evolution.Extensibility.Api.Entities.Version1.Comment>>(), Telligent.Common.Services.Get<IGalleryService>(), Telligent.Common.Services.Get<IEmailChecks>())
        {
        }

        public FileNotificationEmails(IEmailContextService emailContextService, IEmailTemplateService emailTemplateService, IUserService userService,
            IEmailTemplateUtility emailTemplateUtility, IEmailQueuingService emailQueuingService, IMapper<Telligent.Evolution.Components.User,
            Telligent.Evolution.Extensibility.Api.Entities.Version1.User> userMapper, IMapper<ApiMediaGallery, ApiMedia> blogMapper,
            IMapperWithOptions<ApiMediaGalleryPost, ApiMediaFilePost> blogPostMapper, Telligent.Evolution.Api.Services.ICommentService commentService,
            IMapper<Telligent.Evolution.CoreServices.Comments.Comment, Telligent.Evolution.Extensibility.Api.Entities.Version1.Comment> commentMapper,
            IGalleryService galleryService, IEmailChecks emailChecks)
        {
            _iemailContextService = emailContextService;
            _iuserService = userService;
            _iemailTemplateService = emailTemplateService;
            _iemailTemplateUtility = emailTemplateUtility;
            _iemailQueuingService = emailQueuingService;
            _imapper = userMapper;
            _imapper1 = blogMapper;
            _imapperWithOptions = blogPostMapper;
            _icommentService = commentService;
            _imapper2 = commentMapper;
            _igalleryService = galleryService;
            _iemailChecks = emailChecks;
        }

        public void FileNotificationTracking(ApiMediaGalleryPost galleryPost)
        {
            if (galleryPost == null || !galleryPost.IsApproved || !_iemailChecks.EmailEnabled())
                return;
            Dictionary<string,string> dictionary = new Dictionary<string, string>()
                  {
                    {
                      "FileId",
                      galleryPost.PostID.ToString()
                    },
                     //{
                     //   "SectionId",
                     //   galleryPost.SectionID.ToString()
                     //},
                     {
                        "UserId",
                        galleryPost.AuthorID.ToString()
                     },
                     {
                        "FileSubscriptionId",
                        galleryPost.GetExtendedAttribute("FileSubscriptionId")
                     },
                     {
                        "Email",
                        _iemailTemplateUtility.FormatNameAndEmail(galleryPost.User.DisplayName, galleryPost.User.Email)
                     }
                  };
            if (galleryPost.PostDate > DateTime.Now)
                JobsManager.Schedule<FileNotificationEmailHandler>(galleryPost.PostDate.ToUniversalTime(), (IDictionary<string, string>)dictionary);
            else
                JobsManager.Schedule<FileNotificationEmailHandler>(galleryPost.PostDate.AddSeconds(5.0), (IDictionary<string, string>)dictionary);
        }
    }
}

