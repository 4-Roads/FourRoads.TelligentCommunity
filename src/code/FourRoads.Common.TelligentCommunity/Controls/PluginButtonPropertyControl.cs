using System;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;

namespace FourRoads.Common.TelligentCommunity.Controls
{
    public abstract class PluginButtonPropertyControl : IPropertyControl
    {
        public static Button Button;
        public PluginButtonPropertyControl()
        {
            Button = new Button();
            Button.Text = Text;
            Button.Click += Button_Click;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            OnClick();
        }

        protected abstract void OnClick();

        public abstract string Text { get; }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public Property ConfigurationProperty { get; set; }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public System.Web.UI.Control Control => Button;

        public object GetConfigurationPropertyValue()
        {
            return null;
        }

        public void SetConfigurationPropertyValue(object value)
        {
        }
    }
}