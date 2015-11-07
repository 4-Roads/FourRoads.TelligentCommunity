using Telligent.Evolution.Extensibility.Email.Version1;
using Telligent.Evolution.Extensibility.Templating.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Splash.Plugins
{
    public class RegisteredEmail : IEmailTemplatePreviewPlugin, ITokenRegistrar, ITranslatablePlugin , ISingletonPlugin
    {
        public void Initialize()
        {

        }

        public string Name {
            get { return string.Empty; }
        }

        public string Description {
            get { return string.Empty; }
        }

        public void RegisterTokens(ITokenizedTemplateTokenController tokenizedTemplateTokenController)
        {
            
        }

        public void SetController(ITemplatablePluginController controller)
        {
            
        }

        public TokenizedTemplate[] DefaultTemplates
        {
            get
            {
                return new TokenizedTemplate[0];


            }
        }

        public string GetTemplateName(EmailTarget target)
        {
            return "SplashRegistered";
        }

        public void SetController(ITranslatablePluginController controller)
        {

        }

        public Translation[] DefaultTranslations
        {
            get
            {
                return new Translation[0];

            }
        }
    }
}
