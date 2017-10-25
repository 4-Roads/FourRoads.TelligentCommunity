using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;
using TelligentProperty = Telligent.DynamicConfiguration.Components.Property;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class AuthorizeButton : WebControl, IPropertyControl, INamingContainer
    {
        protected Button LinkButton;
        protected TextBox AuthCode;
        protected Literal Message;

        protected override void EnsureChildControls()
        {
            base.EnsureChildControls();

            if (AuthCode == null)
            {
                AuthCode = new TextBox();
                AuthCode.ID = "AuthCode";
                Controls.Add(AuthCode);
            }

            if (Message == null)
            {
                Message = new Literal();
                Message.ID = "Message";
                Message.Text = "<label>Press this button once you have obtained your authorization code</label>";

                Controls.Add(Message);
            }

            if (LinkButton == null)
            {
                LinkButton = new Button();
                LinkButton.ID = "LinkBtn";
                LinkButton.Text = "Link oAuth";
                LinkButton.Click += LinkButton_Click;

                Controls.Add(LinkButton);
            }
        }

        private void LinkButton_Click(object sender, EventArgs e)
        {
            var plg = PluginManager.GetSingleton<HubspotCrm>();

            if (plg != null)
            {
                try
                {
                    if (plg.InitialLinkoAuth(AuthCode.Text))
                    {
                        Message.Text = "<label style=\"color:green\">oAuth Syncronized</label>";
                    }
                    else
                    {
                        Message.Text = "<label style=\"color:red\">Failed to setup oauth credentials</label>";
                    }
                }
                catch (Exception ex)
                {
                    Message.Text = $"<label style=\"color:red\">Error while trying to setup oauth credentials</label><br><small>{ex.Message + ((ex.InnerException != null) ? ": " + ex.InnerException.Message + " : " : "")}</small>";
                }

            }
        }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public TelligentProperty ConfigurationProperty
        { get; set; }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public Control Control => this;

        public object GetConfigurationPropertyValue()
        {
            return null;
        }

        public void SetConfigurationPropertyValue(object value)
        {

        }
    }
}