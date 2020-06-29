using System;
using FourRoads.Common.TelligentCommunity.Components.Tokenizers;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Templating.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class VerifyEmailTokens : ITranslatablePlugin, ITokenRegistrar
    {
        private ITranslatablePluginController _translationController;
        public static Guid VerifyCodeUrl = new Guid("{416362A8-5F1A-45FB-8B89-8335597ED14D}");
        public static Guid VerifyCode = new Guid("{5EF76F9C-3D77-4716-B356-3529F0619890}");
        
        public void Initialize()
        {

        }

        public string Name => "Email Verify Code Tokens";
        public string Description => "Registers tokens for MFA plugin";

        public void RegisterTokens(ITokenizedTemplateTokenController tokenController)
        {
            tokenController.Register(
                new TokenizedTemplateLinkUrlToken(
                    new Guid("{2D28492C-F737-4523-9EAD-B6D7198A0057}"),
                    VerifyCodeUrl,
                    "VerifyCodeUrl",
                    "VerifyCodeUrl_Description",
                    _translationController,
                    document => Apis.Get<IUrl>().Absolute(document.Get<string>(VerifyCodeUrl)),
                    () => "Link to email verification"));


            tokenController.Register(
                new TokenizedTemplateToken(
                    new Guid("{5DA2854D-7831-492B-9D36-080EDF1AD458}"),
                    VerifyCode,
                    "VerifyCode",
                    "VerifyCode_Description",
                    _translationController,
                    document =>document.Get<string>(VerifyCode),
                    () => "Email Verification Code"));
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translationController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation trn = new Translation("en-us");

                trn.Set("VerifyCodeUrl", "Email Verify Code Url");
                trn.Set("VerifyCodeUrl_Description", "Url used to verify this users email account");

                trn.Set("VerifyCode", "Email Verify Code");
                trn.Set("VerifyCode_Description", "Code used to verify this users email account");

                return new[] { trn };
            }
        }
    }
}