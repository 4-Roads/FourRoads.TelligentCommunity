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
using Telligent.DynamicConfiguration.Components;
using Telligent.DynamicConfiguration.Controls;

namespace FourRoads.TelligentCommunity.SocialProfileControls
{
    public abstract class TextProfileControl : Control, IHtmlPropertyControl, IProfilePlugin
    {
        private RegularExpressionValidator regExVal;
        private readonly object _eventKey = new object();

        protected TextBox TextBox { get; private set; }

        public void Initialize()
        {
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public Property ConfigurationProperty { get; set; }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public Control Control
        {
            get { return this; }
        }

        public event ConfigurationPropertyChanged ConfigurationValueChanged
        {
            add
            {
                EnsureChildControls();
                TextBox.AutoPostBack = true;
                Events.AddHandler(_eventKey , value); 
            }
            remove
            {
                Events.RemoveHandler(_eventKey, value);
            }
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            TextBox = new TextBox {ID = "Text"};
            Controls.Add(TextBox);
            if (ConfigurationControlUtility.Instance().GetCurrentConfigurationForm(this) != null && !string.IsNullOrWhiteSpace(ValidationRegEx))
            {
                regExVal = new RegularExpressionValidator
                {
                    ID = "Validator",
                    ControlToValidate = TextBox.ID,
                    ValidationExpression = ValidationRegEx,
                    ErrorMessage = ValidationError,
                    Text = "<span class=\"field-item-validation\">*</span>"
                };
                Controls.Add(regExVal);
            }
            TextBox.TextChanged += TextChanged;
        }

        protected abstract string ValidationError { get;  }
        protected abstract string ValidationRegEx { get; }
    
        private void TextChanged(object sender, EventArgs e)
        {
            OnConfigurationValueChanged(GetConfigurationPropertyValue());
        }

        public abstract string FieldName { get; }
        public virtual string FieldType { get { return "Url"; } }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            EnsureChildControls();
        }

        public void SetConfigurationPropertyValue(object value)
        {
            EnsureChildControls();

            string fullpath = Convert.ToString(value);

            TextBox.Text = SetPropertyValue(fullpath);
        }

        protected abstract string SetPropertyValue(string value); 

        public object GetConfigurationPropertyValue()
        {
            EnsureChildControls();
            return GetPropertyValue(TextBox.Text);
        }

        protected abstract string GetPropertyValue(string value); 
         
        protected virtual void OnConfigurationValueChanged(object value)
        {
            ConfigurationPropertyChanged configurationPropertyChanged = (ConfigurationPropertyChanged)Events[_eventKey];

            if (configurationPropertyChanged == null)
                return;

            configurationPropertyChanged(this, value);
        }

        public string GetValueScript(HtmlConfigurationForm form)
        {
            return string.Format(@" function(val){{ 
                                 {1}
                                 return val; }}(jQuery('#{0}').val());", form.GetUniqueId(ConfigurationProperty), GetValueScript());
        }

        protected abstract string GetValueScript();

        public string Render(HtmlConfigurationForm form)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (TextBox.TextMode == TextBoxMode.MultiLine)
            {
                stringBuilder.AppendFormat("<textarea id=\"{0}\" rows=\"{1}\" cols=\"{2}\">{3}</textarea>",
                    form.GetUniqueId(ConfigurationProperty), TextBox.Rows, TextBox.Columns,
                    string.IsNullOrEmpty(TextBox.Text) ? "" : HttpUtility.HtmlEncode(TextBox.Text));
            }
            else
            {
                stringBuilder.AppendFormat("<input type=\"text\" id=\"{0}\"{1} />",
                    form.GetUniqueId(ConfigurationProperty),
                    string.IsNullOrEmpty(TextBox.Text)
                        ? ""
                        : string.Format(" value=\"{0}\"", HttpUtility.HtmlEncode(TextBox.Text)));
            }

            return stringBuilder.ToString();
        }
    }
}