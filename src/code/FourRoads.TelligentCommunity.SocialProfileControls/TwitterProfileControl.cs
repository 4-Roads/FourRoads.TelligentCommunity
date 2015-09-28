// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2014.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.Common;
using Telligent.Common.Security;
using Telligent.DynamicConfiguration.Components;
using Telligent.DynamicConfiguration.Controls;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.SocialProfileControls
{
    public class TwitterProfileControl : TextProfileControl, ITranslatablePlugin
    {
        public override string Name
        {
            get { return "4 Roads - Twitter"; }
        }

        public override string Description
        {
            get { return "This plugin extends Zimbra Community to include additional profile field to support twitter"; }
        }

        protected override string ValidationError
        {
            get { return TranslationController.GetLanguageResourceValue("profile_twitter_validation_error"); }
        }

        protected override string ValidationRegEx
        {
            get { return string.Empty; }
        }

        public override string FieldName
        {
            get { return "Twitter"; }
        }

        protected override string SetPropertyValue(string value)
        {
            if (!value.StartsWith("http" , StringComparison.OrdinalIgnoreCase))
            {
                value = "https://twitter.com/" + value;
            }

            return value;
        }

        protected override string GetPropertyValue(string value)
        {
            return value;
        }

        protected override string GetValueScript()
        {
            return @"   if (val.toLowerCase().indexOf('http') == 0){{ return val; }}else{{  return 'https://twitter.com/' + val.replace('\@' , ''); }}";
        }

        private ITranslatablePluginController _tranlationController;

        public void SetController(ITranslatablePluginController controller)
        {
            _tranlationController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation[] defaultTranslation = new[] { new Translation("en-us") };

                defaultTranslation[0].Set("profile_twitter_validation_error", "The twitter user name appears to be invalid");

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
    }
}