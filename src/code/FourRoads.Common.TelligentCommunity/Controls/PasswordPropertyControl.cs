using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;

namespace FourRoads.Common.TelligentCommunity.Controls
{
	public class PasswordPropertyControl: Control, IPropertyControl
	{
		private TextBox _textBox;
        private string _orginal;

		#region Control overrides

		protected override void OnPreRender(EventArgs e)
		{
			EnsureChildControls();
			base.OnPreRender(e);
		}

		protected override void CreateChildControls()
		{
			base.CreateChildControls();
            if ( _textBox == null )
            {
                _textBox = new TextBox { TextMode = TextBoxMode.Password };
                Controls.Add(_textBox);
            }
		}

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);
			EnsureChildControls();
		}

		#endregion

		#region IPropertyControl Members

		public ConfigurationDataBase ConfigurationData { get; set; }

		public Property ConfigurationProperty { get; set; }

		public event ConfigurationPropertyChanged ConfigurationValueChanged;

		public Control Control
		{
			get { return this; }
		}

		public object GetConfigurationPropertyValue()
		{
			EnsureChildControls();

            if ( string.IsNullOrWhiteSpace(_textBox.Text) )
                return ConfigurationData.GetStringValue(ConfigurationProperty.ID , "");

            return _textBox.Text;
		}

		public void SetConfigurationPropertyValue(object value)
		{
			EnsureChildControls();

            if (!string.IsNullOrWhiteSpace((string)value) )
                _orginal = _textBox.Text = ((value == null) ? string.Empty : value.ToString());
		}

		#endregion
	}
}