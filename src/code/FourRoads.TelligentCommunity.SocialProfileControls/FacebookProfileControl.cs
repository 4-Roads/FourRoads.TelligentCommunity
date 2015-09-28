// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2014.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.SocialProfileControls
{
    public class FacebookProfileControl : TextProfileControl, ITranslatablePlugin
    {
        public override string Name
        {
            get { return "4 Roads - Facebook"; }
        }

        public override string Description
        {
            get { return "This plugin extends Zimbra Community to include additional profile field to support Facebook"; }
        }

        protected override string ValidationError
        {
            get { return TranslationController.GetLanguageResourceValue("profile_Facebook_validation_error"); }
        }

        protected override string ValidationRegEx
        {
            get { return ""; }
        }

        public override string FieldName
        {
            get { return "Facebook"; }
        }


        protected override string SetPropertyValue(string value)
        {
            if (!value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                value = "https://facebook.com/" + value;
            }

            return value;
        }

        protected override string GetPropertyValue(string value)
        {
            return value;
        }

        protected override string GetValueScript()
        {
            return @"   if (val.toLowerCase().indexOf('http') == 0){{ return val; }}else{{  return 'https://facebook.com/' + val.replace('\@' , ''); }}";
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
                Translation[] defaultTranslation = new[] {new Translation("en-us")};

                defaultTranslation[0].Set("profile_Facebook_validation_error", "The Facebook user name appears to be invalid");

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