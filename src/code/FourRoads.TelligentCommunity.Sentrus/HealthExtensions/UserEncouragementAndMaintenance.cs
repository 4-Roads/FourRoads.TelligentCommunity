using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
    using System;
    using System.Collections.Generic;
using FourRoads.Common;
using FourRoads.Common.TelligentCommunity.Components.Tokenizers;
using  FourRoads.TelligentCommunity.Sentrus.Controls;
    using  FourRoads.TelligentCommunity.Sentrus.Interfaces;
    using Telligent.Common;
    using Telligent.DynamicConfiguration.Components;
    using Telligent.Evolution.Extensibility.Api.Entities.Version1;
    using Telligent.Evolution.Extensibility.Api.Version1;
    using Telligent.Evolution.Extensibility.Version1;
    using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;
    using Telligent.Evolution.Extensibility.Email.Version1;
    using System.Linq;
    using FourRoads.TelligentCommunity.Sentrus.Entities;
    using Telligent.Evolution.Extensibility.Templating.Version1;
    using FourRoads.Common.TelligentCommunity.Components.Extensions;

namespace FourRoads.TelligentCommunity.Sentrus.HealthExtensions
{


    public class EmailHealthEncouragementContainer
    {
        public static Guid DateTypeId
        {
            get
            {
                return new Guid("D5FA0EBF-C0C6-4C8F-9E1A-43AE2CEA77D4");
            }
        }

        public List<ContentRecommendation> ContentRecommendations { get; set; }
    }

    internal static class EmailTargetExtension
    {
        public static string ToTemplateTypeString(this EmailTarget emumValue)
        {
            return "email_" + emumValue.ToString().ToLower();
        }
    }

    public class UserEncouragementAndMaintenance : HealthExtensionBase, IHealthExtension, IUserEncouragementAndMaintenance , IPluginGroup
    {
        private IUserHealth _userHealth;
        private static object _lock = new object();

        protected override string HealthName
        {
            get { return "User Maintenance & Encouragement"; }
        }

        public override void Initialize()
        {
            PublicApi.Users.Events.AfterAuthenticate += Events_AfterAuthenticate;
            PublicApi.Users.Events.AfterIdentify += Events_AfterIdentify;
        }

        public override string Description
        {
            get { return "This plugin enables the management of user accounts that have been inactive and also sends an email that encourages them to reengage with the site"; }
        }

        protected IUserHealth UserHealth
        {
            get
            {
                if (_userHealth == null)
                {
                    _userHealth = Injector.Get<IUserHealth>();
                }

                return _userHealth;
            }
        }


        public void TestSettings()
        {
            int outofDateMonths = Configuration.GetInt("accountDeleteAge");

            StringBuilder emailAccountList = new StringBuilder();

            foreach (User user in  UserHealth.GetInactiveUsers(Configuration.GetInt("accountAge")))
            {
                var lastLoginData = UserHealth.GetLastLoginDetails(user.ContentId);

                int waringCount = lastLoginData.EmailCountSent;
                DateTime lastSent = lastLoginData.FirstEmailSentAt ?? DateTime.Now;

                if (waringCount == 0 || DateTime.Now > lastSent.AddMonths(outofDateMonths*waringCount))
                {
                    emailAccountList.AppendLine(user.PrivateEmail);
                }
            }

            //Send a sample email
            UserAccountEncouragement(PublicApi.Users.Get(new UsersGetOptions() { Id = PublicApi.Url.ParsePageContext(System.Web.HttpContext.Current.Request.Url.ToString()).UserId }), emailAccountList.ToString());
        }


        public override void InternalExecute()
        {
            //Find all users that have not been active
            if (IsEnabled)
            {
                lock (_lock)
                {
                    //User that have been warned and the warn date is 1 month old should be deleted
                    int outofDateMonths = Configuration.GetInt("accountDeleteAge");
                    int maxCount = 10000;

                    foreach (User user in  UserHealth.GetInactiveUsers(Configuration.GetInt("accountAge")))
                    {
                        maxCount--;
                        if (maxCount <= 0)
                            break;

                        var lastLoginData = UserHealth.GetLastLoginDetails(user.ContentId);

                        int waringCount = lastLoginData.EmailCountSent;
                        DateTime lastSent = lastLoginData.FirstEmailSentAt ?? DateTime.Now;

                        if (waringCount == 0 || DateTime.Now > lastSent.AddMonths(outofDateMonths*waringCount))
                        {
                            UserAccountEncouragement(user);

                            lastLoginData.EmailCountSent = ++waringCount;
                            lastLoginData.FirstEmailSentAt = lastSent;

                            UserHealth.CreateUpdateLastLoginDetails(lastLoginData);
                        }
                    }
                }
            }
        }


        void Events_AfterIdentify(UserAfterIdentifyEventArgs e)
        {
            UpdateLoginDate(e.ContentId);
        }

        private void Events_AfterAuthenticate(UserAfterAuthenticateEventArgs e)
        {
            UpdateLoginDate(e.ContentId);
        }

        private void UpdateLoginDate(Guid contentId)
        {
            var lastLoginData = Telligent.Evolution.Extensibility.Caching.Version1.CacheService.Get(LastLoginDetails.CacheKey(contentId), Telligent.Evolution.Extensibility.Caching.Version1.CacheScope.All) as LastLoginDetails;

            if (lastLoginData == null || lastLoginData.LastLogonDate < DateTime.Now.AddHours(-1))
            {
                lastLoginData = UserHealth.GetLastLoginDetails(contentId) ?? new LastLoginDetails { MembershipId = contentId };

                lastLoginData.LastLogonDate = DateTime.Now;
                lastLoginData.EmailCountSent = 0;
                lastLoginData.FirstEmailSentAt = null;

                UserHealth.CreateUpdateLastLoginDetails(lastLoginData);

                Telligent.Evolution.Extensibility.Caching.Version1.CacheService.Put(LastLoginDetails.CacheKey(contentId), lastLoginData, Telligent.Evolution.Extensibility.Caching.Version1.CacheScope.All);
            }
        }

        private void UserAccountEncouragement(User user , string additionalData = null)
        {
            if (!user.EnableEmail.GetValueOrDefault(true))
                return;

            EmailHealthEncouragementContainer emailDigestContainer = new EmailHealthEncouragementContainer();

            PublicApi.Users.RunAsUser(user.Username, () =>
            {
                emailDigestContainer.ContentRecommendations = PublicApi.ContentRecommendations.List(new ContentRecommendationsListOptions()
                {
                    PageSize = 25,
                    PageIndex = 0,
                    ContentTypeIds = new Guid[] { PublicApi.BlogPosts.ContentTypeId, PublicApi.ForumThreads.ContentTypeId, PublicApi.Media.ContentTypeId, PublicApi.WikiPages.ContentTypeId, PublicApi.ForumReplies.ContentTypeId }
                }).ToList();
            });

            TemplateContext templateContext = new TemplateContext(new Dictionary<Guid, object>()
                    {
                        { PublicApi.Users.ContentTypeId, user },
                        { EmailHealthEncouragementContainer.DateTypeId, emailDigestContainer }
                    });

            PublicApi.Users.RunAsUser(user.Username, () =>
            {
                var mailTempalte = Telligent.Evolution.Extensibility.Version1.PluginManager.GetSingleton<UserEncouragementEmailTemplate>();

                if (mailTempalte != null)
                {
                    var mailOptions = mailTempalte.GetSendMailOptions(user.Id.GetValueOrDefault(), templateContext);

                    if (additionalData != null)
                    {
                        var attachments = new List<System.Net.Mail.Attachment>();

                        byte[] byteArray = Encoding.UTF8.GetBytes(additionalData);

                        var attachment =
                            new System.Net.Mail.Attachment(new MemoryStream(byteArray),
                                new System.Net.Mime.ContentType("text/plain; charset=us-ascii"));

                        attachment.ContentDisposition.DispositionType = "attachment";
                        attachment.ContentDisposition.FileName = "userlist.txt";
                        attachment.ContentDisposition.Size = byteArray.Length;

                        attachments.Add(attachment);

                        mailOptions.Attachments = attachments;
                    }

                    PublicApi.SendEmail.Send(mailOptions);
                }
            });
        }

        #region IHealthExtension Members

        public override PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup deleteUsersGroup = new PropertyGroup("deleteUsers", "User Maintenance", 1);

                Property accountAge = new Property("userMaintenance", "Disengaged Users", PropertyType.Custom, 1, "")
                {
                    ControlType = typeof (InactiveUserManagement)
                };

                deleteUsersGroup.Properties.Add(accountAge);

                return new[] {GetConfiguration(), deleteUsersGroup};
            }
        }

        public override void InternalUpdate(IPluginConfiguration configuration)
        {



        }

        public int InactivityPeriod
        {
            get { return Configuration.GetInt("accountAge"); }
        }

        protected override PropertyGroup GetRootGroup()
        {
            PropertyGroup group = new PropertyGroup("autouserCleanup", "User Encouragement", 0);
            group.DescriptionText = "Sends an email to try and encourage users to renengage with the site.";

            Property accountAge = new Property("accountAge", "Inactivity Period (months)", PropertyType.Int, 3, "24")
            {
                DescriptionText = "A period after which users start recieving encouragement to re-engage the site"

            };
            group.Properties.Add(accountAge);

            Property accountDeleteAge = new Property("accountDeleteAge", "Encouragement Period (months)", PropertyType.Int, 4, "1")
            {
                DescriptionText = "A period after which the user is then emailed to re-engage with content updates"
            };

            group.Properties.Add(accountDeleteAge);

            Property testButton = new Property("testButton", "Test Settings", PropertyType.Custom, 5, "")
            {
                ControlType = typeof(TestSettingButton)
            };

            group.Properties.Add(testButton);


            return group;
        }

        #endregion

        public IEnumerable<Type> Plugins {
            get { return new[] {typeof (UserEncouragementEmailTemplate)}; }
        }
    }

    public class UserEncouragementEmailTemplate : IEmailTemplatePreviewPlugin, ITokenRegistrar, ITranslatablePlugin , ISingletonPlugin
    {
        private ITemplatablePluginController _templateController;
        private ITranslatablePluginController _translationController;
        private TokenizedTemplate[] _tokenizedTemplates = null;

        public SendEmailOptions GetSendMailOptions(int userid, TemplateContext templateContext)
        {
            var mailOptions = new SendEmailOptions()
            {
                ToUserId = userid,
                Header = _templateController.RenderTokenString(EmailTarget.Header.ToTemplateTypeString(), templateContext),
                Footer = _templateController.RenderTokenString(EmailTarget.Footer.ToTemplateTypeString(), templateContext),
                Subject = _templateController.RenderTokenString(EmailTarget.Subject.ToTemplateTypeString(), templateContext),
                Body = _templateController.RenderTokenString(EmailTarget.Body.ToTemplateTypeString(), templateContext),
            };
            return mailOptions;
        }

        public string GetTemplateName(EmailTarget target)
        {
            return target.ToTemplateTypeString();
        }

        private TokenizedTemplate CreateTokenizedTemplate(EmailTarget target, string defaultTemplate, params Guid[] contexts)
        {
            var template = new TokenizedTemplate(target.ToTemplateTypeString())
            {
                Name = target.ToString(),
                Description = target.ToString() + " of the email",
                ContextualDataTypeIds = contexts
            };

            template.Set("en-us", defaultTemplate);

            return template;
        }

        public TokenizedTemplate[] DefaultTemplates
        {
            get
            {
                if (_tokenizedTemplates == null)
                {
                    _tokenizedTemplates = new TokenizedTemplate[]{

                        CreateTokenizedTemplate(EmailTarget.Header  , _defaultHeaderContent ,  PublicApi.Users.ContentTypeId,  EmailHealthEncouragementContainer.DateTypeId),
                        CreateTokenizedTemplate(EmailTarget.Footer  ,  _defualtFooterContent ,  PublicApi.Users.ContentTypeId,  EmailHealthEncouragementContainer.DateTypeId),
                        CreateTokenizedTemplate(EmailTarget.Subject  , _defualtSubjectContent ,  PublicApi.Users.ContentTypeId,  EmailHealthEncouragementContainer.DateTypeId),
                        CreateTokenizedTemplate(EmailTarget.Body  ,  _defualtBodyContent ,  PublicApi.Users.ContentTypeId,  EmailHealthEncouragementContainer.DateTypeId)
                    };
                }

                return _tokenizedTemplates;
            }
        }

        public void SetController(ITemplatablePluginController controller)
        {
            _templateController = controller;
        }

        public void RegisterTokens(ITokenizedTemplateTokenController tokenizedTemplateTokenController)
        {
            tokenizedTemplateTokenController.Register(new TokenizedTemplateEnumerableToken(new Guid("{82944BE4-1557-4780-B61C-DB5EB5AB5D89}"), EmailHealthEncouragementContainer.DateTypeId, "ContentRecommendations", "ContentRecommendations_Description", _translationController,
                    tc =>
                    {
                        List<TemplateContext> list = new List<TemplateContext>();

                        foreach (ContentRecommendation contentRecommendation in tc.Get<EmailHealthEncouragementContainer>(EmailHealthEncouragementContainer.DateTypeId).ContentRecommendations)
                        {
                            TemplateContext templateContext = new TemplateContext();
                            templateContext.AddItem(Telligent.Evolution.Api.Content.ContentTypes.GenericContent, (object)contentRecommendation.Content);
                            list.Add(templateContext);
                        }
                        return list;

                    }, Telligent.Evolution.Api.Content.ContentTypes.GenericContent));


        }

        public Translation[] DefaultTranslations
        {
            get
            {

                Translation trn = new Translation("en-us");

                trn.Set("ContentRecommendations", "Recommended Content");
                trn.Set("ContentRecommendations_Description", "A list of content that is recommended for this user");

                return new[] { trn };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translationController = controller;
        }


        private const string _defaultHeaderContent = @"<template id=""email_header"" name=""Header"" description=""Header of the email"">
			                                            <source />
			                                            <fragments />
		                                            </template>";

        private const string _defualtFooterContent = @"<template id=""email_footer"" name=""Footer"" description=""Footer of the email"">
			                                            <source>&lt;p&gt;You received this email because you are member of ${token:7cb0b4af-6718-4330-a231-e712b328f8ea}&lt;/p&gt;</source>
			                                            <fragments />
		                                               </template>";

        private const string _defualtSubjectContent = @"<template id=""email_subject"" name=""Subject"" description=""Subject of the email"">
			                                                    <source>&lt;p&gt;Come back to - ${token:7cb0b4af-6718-4330-a231-e712b328f8ea}&lt;/p&gt;</source>
			                                                    <fragments />
		                                                </template>";

        private const string _defualtBodyContent = @"<template id=""email_body"" name=""Body"" description=""Body of the email"">
			<source>&lt;p&gt;Hi ${token:fcc0b64e-5df3-4d47-9df7-97944c8fda37:sub-template=6c3dbc96-96b4-49b4-84ca-28c366f77f85},&lt;/p&gt;
&lt;p&gt;We've missed you on the communty, here are some thigns that have been happening on the site.&lt;/p&gt;
${token:82944be4-1557-4780-b61c-db5eb5ab5d89:max-count=f690b73b-e95e-4699-b40b-0a3069479b2f&amp;item-template=8d593add-432b-4cdb-8df9-e037c8fca7e2&amp;empty-template=&amp;before-template=&amp;after-template=}
&lt;p&gt;|&lt;/p&gt;
&lt;p&gt;&amp;nbsp;&lt;/p&gt;</source>
			<fragments>
				<fragment id=""6c3dbc96-96b4-49b4-84ca-28c366f77f85"" tokenName=""Current User"" fragmentName=""sub-template"">${token:43d3b489-d151-4d30-96de-f1978a29c450}</fragment>
				<fragment id=""f690b73b-e95e-4699-b40b-0a3069479b2f"" tokenName=""Recommended Content"" fragmentName=""max-count"">3</fragment>
				<fragment id=""4e669117-2d5f-4ba0-a675-149509995a59"" tokenName=""Content - Url"" fragmentName=""internal-template"">&lt;span style=""color: #0087c3; font-size: 14pt; text-decoration: none; font-family: Arial, Helvetica, sans-serif;""&gt;${token:4a432584-f740-4042-b7fb-e9b5807a1a2f}&lt;/span&gt;</fragment>
				<fragment id=""6d7a7642-3ab8-4b4e-b4df-1b96c53bae17"" tokenName=""User - Avatar Url"" fragmentName=""tag-attributes"">width=70&amp;height=70</fragment>
				<fragment id=""722e2550-5994-4e17-8885-737bdd0b3462"" tokenName=""User - Profile Url"" fragmentName=""internal-template"">${token:9d4aad1b-7f2a-4651-ac68-f47a51262dec:tag-attributes=6d7a7642-3ab8-4b4e-b4df-1b96c53bae17&amp;title-template=}</fragment>
				<fragment id=""58314a17-609c-48b2-8644-bbc91827da89"" tokenName=""User - Profile Url"" fragmentName=""internal-template"">&lt;span style=""color: #0087c3; font-size: 10pt; text-decoration: none; font-family: Arial, Helvetica, sans-serif;""&gt;${token:43d3b489-d151-4d30-96de-f1978a29c450}&lt;/span&gt;</fragment>
				<fragment id=""55fa1f78-a30c-419a-a343-f92cf9634711"" tokenName=""Content - Author"" fragmentName=""sub-template"">&lt;table border=""0"" cellspacing=""0"" cellpadding=""0""&gt;
					&lt;tbody&gt;
					&lt;tr&gt;
					&lt;td&gt;${token:8b8b6940-1eb0-4143-94d6-cc5445ec5252:tag-attributes=&amp;title-template=&amp;internal-template=722e2550-5994-4e17-8885-737bdd0b3462}&lt;/td&gt;
					&lt;td style=""padding-left: 10px;""&gt;${token:8b8b6940-1eb0-4143-94d6-cc5445ec5252:tag-attributes=&amp;title-template=&amp;internal-template=58314a17-609c-48b2-8644-bbc91827da89}&lt;/td&gt;
					&lt;/tr&gt;
					&lt;/tbody&gt;
					&lt;/table&gt;</fragment>
				<fragment id=""f8a1ed28-cdf5-4917-aece-f0ad2030f07e"" tokenName=""Content - Body"" fragmentName=""max-characters"">300</fragment>
				<fragment id=""b4d53774-feda-410e-85ef-c22e815a8a59"" tokenName=""Content - Url"" fragmentName=""internal-template"">&lt;span style=""color: #0087c3; text-decoration: none;""&gt;More&lt;/span&gt;</fragment>
				<fragment id=""af80fc3c-0333-4fb5-93f0-c8f425f4f36b"" tokenName=""Content - Body"" fragmentName=""after-truncation-template"">...${token:01a29c5b-7e53-4b9b-9a04-784cc459bee6:tag-attributes=&amp;title-template=&amp;internal-template=b4d53774-feda-410e-85ef-c22e815a8a59}</fragment>
				<fragment id=""071944d7-4f12-4c06-b4cd-26637a9241e0"" tokenName=""Content - Like Count"" fragmentName=""comparison-type"">gt</fragment>
				<fragment id=""144f9fc9-e937-43aa-bc22-189ab8667854"" tokenName=""Content - Like Count"" fragmentName=""comparison-value"">0</fragment>
				<fragment id=""9108754c-1422-43e9-8b9f-7314c94af138"" tokenName=""Content - Like Count"" fragmentName=""comparison-type"">gt</fragment>
				<fragment id=""59cd00e1-8f84-439a-b380-b519cb7c2f9b"" tokenName=""Content - Like Count"" fragmentName=""comparison-value"">1</fragment>
				<fragment id=""27e57d98-7cb3-4a6f-8535-68dd4262517d"" tokenName=""Content - Like Count"" fragmentName=""true-template"">Likes</fragment>
				<fragment id=""1a979909-a56f-4973-8465-0a368b88db86"" tokenName=""Content - Like Count"" fragmentName=""false-template"">Like</fragment>
				<fragment id=""7672aa10-8f8e-402e-800e-2d980cc64c0c"" tokenName=""Content - Like Count"" fragmentName=""true-template"">${token:d8c52075-604b-4b19-b96a-7251d7e8493f}&amp;nbsp;${token:d8c52075-604b-4b19-b96a-7251d7e8493f:comparison-type=9108754c-1422-43e9-8b9f-7314c94af138&amp;comparison-value=59cd00e1-8f84-439a-b380-b519cb7c2f9b&amp;true-template=27e57d98-7cb3-4a6f-8535-68dd4262517d&amp;false-template=1a979909-a56f-4973-8465-0a368b88db86}</fragment>
				<fragment id=""1ac310af-5f37-4b07-8c7b-f45cfcb9d8f0"" tokenName=""Content - Reply Count"" fragmentName=""comparison-type"">gt</fragment>
				<fragment id=""a1e5801d-c424-4c6c-8bd9-e5bc7a57d35d"" tokenName=""Content - Reply Count"" fragmentName=""comparison-value"">0</fragment>
				<fragment id=""fabe339d-939e-493a-8162-3a5d9aeec403"" tokenName=""Content - Reply Count"" fragmentName=""comparison-type"">gt</fragment>
				<fragment id=""17315bbf-248a-4094-a103-398efa5de3aa"" tokenName=""Content - Reply Count"" fragmentName=""comparison-value"">1</fragment>
				<fragment id=""9256aef2-f2ac-4c0c-b2f7-8d0717c698bd"" tokenName=""Content - Reply Count"" fragmentName=""true-template"">Replies</fragment>
				<fragment id=""f0cca3d2-a19f-47d2-afae-dbebc6629c3c"" tokenName=""Content - Reply Count"" fragmentName=""false-template"">Reply</fragment>
				<fragment id=""55c804b0-be57-47c8-9767-38fa5de4cd59"" tokenName=""Content - Reply Count"" fragmentName=""true-template"">${token:6387cae6-4240-4bab-95c6-e844c1765f8d}&amp;nbsp;${token:6387cae6-4240-4bab-95c6-e844c1765f8d:comparison-type=fabe339d-939e-493a-8162-3a5d9aeec403&amp;comparison-value=17315bbf-248a-4094-a103-398efa5de3aa&amp;true-template=9256aef2-f2ac-4c0c-b2f7-8d0717c698bd&amp;false-template=f0cca3d2-a19f-47d2-afae-dbebc6629c3c}</fragment>
				<fragment id=""8d593add-432b-4cdb-8df9-e037c8fca7e2"" tokenName=""Recommended Content"" fragmentName=""item-template"">&lt;p&gt;${token:01a29c5b-7e53-4b9b-9a04-784cc459bee6:tag-attributes=&amp;title-template=&amp;internal-template=4e669117-2d5f-4ba0-a675-149509995a59}&lt;/p&gt;
					&lt;p&gt;${token:b2cfd83f-353b-4b4e-9ce8-b5be613e7562:sub-template=55fa1f78-a30c-419a-a343-f92cf9634711}&lt;/p&gt;
					&lt;p style=""font-size: 10pt; font-family: Arial, Helvetica, sans-serif;""&gt;${token:96e7a55c-5f24-4e96-a8b6-2f36b376b6cf:max-characters=f8a1ed28-cdf5-4917-aece-f0ad2030f07e&amp;after-truncation-template=af80fc3c-0333-4fb5-93f0-c8f425f4f36b}&lt;/p&gt;
					&lt;p&gt;&lt;span style=""color: #7f7f7f; font-size: 8pt;""&gt;${token:d8c52075-604b-4b19-b96a-7251d7e8493f:comparison-type=071944d7-4f12-4c06-b4cd-26637a9241e0&amp;comparison-value=144f9fc9-e937-43aa-bc22-189ab8667854&amp;true-template=7672aa10-8f8e-402e-800e-2d980cc64c0c&amp;false-template=}&amp;nbsp;${token:6387cae6-4240-4bab-95c6-e844c1765f8d:comparison-type=1ac310af-5f37-4b07-8c7b-f45cfcb9d8f0&amp;comparison-value=a1e5801d-c424-4c6c-8bd9-e5bc7a57d35d&amp;true-template=55c804b0-be57-47c8-9767-38fa5de4cd59&amp;false-template=}&lt;/span&gt;&lt;/p&gt;
					&lt;hr style=""width: 100%; color: #e6e6e6;"" width=""100%"" /&gt;</fragment>
			</fragments>
		</template>";

        public void Initialize()
        {
            
        }

        public string Name {
            get { return "Email Template"; }
        }
        public string Description {
            get { return "The email that is sent to users to encourage them to come back to the site"; }
        }
    }
}