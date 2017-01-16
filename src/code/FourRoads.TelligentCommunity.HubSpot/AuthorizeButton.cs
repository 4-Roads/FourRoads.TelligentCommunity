using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class AuthorizeButton : WebControl, IPropertyControl , INamingContainer
    {
        protected Button LinkButton;
        protected TextBox AuthCode;
        protected Literal Message; 

        protected override void EnsureChildControls()
        {
            base.EnsureChildControls();

            if ( AuthCode == null )
            {
                AuthCode = new TextBox();
                AuthCode.ID = "AuthCode";
                Controls.Add(AuthCode);
            }

            if ( Message == null )
            {
                Message = new Literal();
                Message.ID = "Message";
                Message.Text = "<label>Press this button once you have obtained your authorization code</label>";

                Controls.Add(Message);
            }

            if ( LinkButton == null )
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

            if ( plg != null )
            {
                if ( plg.InitialLinkoAuth(AuthCode.Text) )
                {
                    Message.Text = "<label style=\"color:red\">Failed to obtain oauth credentials</label>";
                }
                else
                {
                    Message.Text = "<label style=\"color:green\">oAuth Syncronized</label>";
                }
            }
        }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public Property ConfigurationProperty
        { get; set; }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public new Control Control => this;

        public object GetConfigurationPropertyValue()
        {
            return null;
        }

        public void SetConfigurationPropertyValue(object value)
        {

        }
    }
}