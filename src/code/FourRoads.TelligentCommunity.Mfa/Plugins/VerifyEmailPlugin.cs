using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Email.Version1;
using Telligent.Evolution.Extensibility.Templating.Version1;
using Telligent.Evolution.Extensibility.Version1;
using ISendEmail = Telligent.Evolution.Extensibility.Api.Version2.ISendEmail;
using SendEmailOptions = Telligent.Evolution.Extensibility.Api.Version2.SendEmailOptions;
namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class VerifyEmailPlugin : ISingletonPlugin, IEmailTemplatePreviewPlugin, IVerifyEmailProvider
    {


        private const string _defaultHeaderContent = @"<template id=""email_header"" name=""Header"" description=""Header of the email"">
			                                            <source />
			                                            <fragments />
		                                            </template>";

        private const string _defualtFooterContent = @"<template id=""email_footer"" name=""Footer"" description=""Footer of the email"">
			                                            <source></source>
			                                            <fragments />
		                                               </template>";

        private const string _defualtSubjectContent = @"<template id=""email_subject"" name=""Subject"" description=""Subject of the email"">
			                                                    <source>Verify your account email address</source>
			                                                    <fragments />
		                                                </template>";

        private const string _defualtBodyContent = @"<template id=""email_body"" name=""Body"" description=""Body of the email"">
        <source>
        <![CDATA[
<p></p>
<p>Hi&nbsp;${token:fcc0b64e-5df3-4d47-9df7-97944c8fda37:sub-template=%24%7Btoken%3A43d3b489-d151-4d30-96de-f1978a29c450%7D},</p>
<p>You have been sent this email to verify that you own it.</p>
<p>Please click on the link to confirm that this is your email and activate your community account or type in the following&nbsp;${token:5da2854d-7831-492b-9d36-080edf1ad458:tag-attributes=&title-template=&internal-template=Code}:&nbsp;</p>
<p>${token:2d28492c-f737-4523-9ead-b6d7198a0057:tag-attributes=&title-template=&internal-template=Code}</p>
<p></p>
        ]]>
        </source>
         <fragments />
		</template>";

        public string Description => "Verify Email";

        public void Initialize()
        {

        }


        public string Name => "Verify Email";

        private TokenizedTemplate CreateTokenizedTemplate(EmailTarget target, string defaultTemplate, params Guid[] contexts)
        {
            var template = new TokenizedTemplate(target.ToTemplateTypeString())
            {
                Name = target.ToString(),
                Description = target + " of the email",
                ContextualDataTypeIds = contexts
            };

            template.Set("en-us", defaultTemplate);

            return template;
        }

        private ITemplatablePluginController _templateController;
        private TokenizedTemplate[] _tokenizedTemplates;
        public void SetController(ITemplatablePluginController controller)
        {
            _templateController = controller;
        }

        public void SendEmail(User user, string code)
        {
            TemplateContext templateContext = new TemplateContext(new Dictionary<Guid, object>()
            {
                { VerifyEmailTokens.VerifyCode, code},
                { VerifyEmailTokens.VerifyCodeUrl, $"/verifyemail?code={code}&username={HttpUtility.UrlDecode(user.Username)}"}
            });

            Apis.Get<ISendEmail>().SendAsync( new SendEmailOptions()
            {
                ToUserId = user.Id,
                Header = _templateController.RenderTokenString(EmailTarget.Header.ToTemplateTypeString(), templateContext),
                Footer = _templateController.RenderTokenString(EmailTarget.Footer.ToTemplateTypeString(), templateContext),
                Subject = _templateController.RenderTokenString(EmailTarget.Subject.ToTemplateTypeString(), templateContext),
                Body = _templateController.RenderTokenString(EmailTarget.Body.ToTemplateTypeString(), templateContext),
            }).ContinueWith(t =>
            {
                Apis.Get<IExceptions>().Log(t.Exception);

            } , TaskContinuationOptions.OnlyOnFaulted);

        }

        public TokenizedTemplate[] DefaultTemplates
        {
            get
            {
                if (_tokenizedTemplates == null)
                {
                    _tokenizedTemplates = new[]
                    {
                        CreateTokenizedTemplate(EmailTarget.Header, _defaultHeaderContent,VerifyEmailTokens.VerifyCodeUrl),
                        CreateTokenizedTemplate(EmailTarget.Footer, _defualtFooterContent, VerifyEmailTokens.VerifyCodeUrl),
                        CreateTokenizedTemplate(EmailTarget.Subject, _defualtSubjectContent,VerifyEmailTokens.VerifyCodeUrl),
                        CreateTokenizedTemplate(EmailTarget.Body, _defualtBodyContent,VerifyEmailTokens.VerifyCodeUrl)
                    };
                }

                return _tokenizedTemplates;
            }
        }

        public string GetTemplateName(EmailTarget target)
        {
            return target.ToTemplateTypeString();
        }

    }
}