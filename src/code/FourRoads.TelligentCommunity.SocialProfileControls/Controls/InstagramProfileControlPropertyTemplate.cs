using System.IO;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;

namespace FourRoads.TelligentCommunity.SocialProfileControls.Controls
{
    public class InstagramProfileControlPropertyTemplate : SocialProfileFieldControlPropertyTemplate, IPropertyTemplate, ITranslatablePlugin
    {
        public static string GetTemplateName()
        {
            return "socialProfile_instagramProfileControl";
        }

        public string TemplateName => GetTemplateName();

        public string Name => "4 Roads - Instagram - Field Control Property Template";

        public string Description => "Controls the input of instagram details into the users profile";

        public override string GetPlaceholder() =>  "https://www.instagram.com/";

        private ITranslatablePluginController _tranlationController;

        public void SetController(ITranslatablePluginController controller)
        {
            _tranlationController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation[] defaultTranslation = new[] {new Translation("en-us")};

                defaultTranslation[0].Set("profile_Instagram_validation_error", "Invalid instagram url entered");

                return defaultTranslation;
            }
        }

        protected ITranslatablePluginController TranslationController
        {
            get
            {
                if (_tranlationController == null)
                {
                    _tranlationController = new TranslatablePluginController(this, Services.Get<ITranslatablePluginService>());
                }

                return _tranlationController;
            }
        }

        public override string GetValueValidationScript(IPropertyTemplateOptions options, string inputElement, string apiChanged)
        {
            return $@"
                var val = i.val();
                if (val.toLowerCase().indexOf('http') == 0)
                {{ 
                    if (val.toLowerCase().indexOf('instagram.com') != -1)
                    {{
                        return;
                    }}
                    else
                    {{
                        $.telligent.evolution.notifications.show('{_tranlationController.GetLanguageResourceValue("profile_Instagram_validation_error")}', {{ type: 'error' }});
                        i.val('');
                        {apiChanged}
                    }}
                }}
                else if (val && val.length > 0)
                {{  
                    i.val('https://instagram.com/' + val.replace('\@' , '')); 
                    {apiChanged}
                }}
            ";
        }
    }
}
