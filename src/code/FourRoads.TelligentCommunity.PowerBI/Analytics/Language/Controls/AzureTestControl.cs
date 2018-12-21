using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;
using TelligentProperty = Telligent.DynamicConfiguration.Components.Property;

namespace FourRoads.TelligentCommunity.PowerBI.Analytics.Language.Controls
{
    public class AzureTestControl : WebControl, IPropertyControl, INamingContainer
    {
        protected Button TestButton;
        protected TextBox TestContent;
        protected Literal Message;

        protected override void EnsureChildControls()
        {
            base.EnsureChildControls();

            if (TestContent == null)
            {
                TestContent = new TextBox();
                TestContent.ID = "AzureTextBox";
                TestContent.Text = "I still have a dream. It is a dream deeply rooted in the American dream. I have a dream that one day this nation will rise up and live out the true meaning of its creed: \"We hold these truths to be self-evident, that all men are created equal.\"";

                Controls.Add(TestContent);
                Controls.Add(new Literal() { Text = "<br>" });
            }
            
            if (TestButton == null)
            {
                TestButton = new Button();
                TestButton.ID = "AzureTestBtn";
                TestButton.Text = "Test";
                TestButton.Click += AzureButton_Click;

                Controls.Add(TestButton);
                Controls.Add(new Literal() { Text = "<br>" });

            }

            if (Message == null)
            {
                Message = new Literal();
                Message.ID = "AzureMessage";
                Message.Text = "<label>Click to test Azure NLP API</label>";

                Controls.Add(Message);
            }
        }

        private void AzureButton_Click(object sender, EventArgs e)
        {
            var plg = PluginManager.GetSingleton<PowerBIPlugin>();

            if (plg != null)
            {
                try
                {
                    string azureRegion = plg.GetConfiguration().GetString("azureRegion");
                    string azuretextAnalyticsAPI = plg.GetConfiguration().GetString("azureTextAnalyticsAPI");

                    if (!string.IsNullOrWhiteSpace(azureRegion) && !string.IsNullOrWhiteSpace(azuretextAnalyticsAPI))
                    {
                        var azureLanguage = new AzureLanguage(azureRegion, azuretextAnalyticsAPI);

                        var keywords = azureLanguage.KeyPhrases(TestContent.Text);
                        if (keywords != null && keywords.Count > 0)
                        {
                            Message.Text = $"<br><label style=\"color:green\">Found {keywords.Count} keywords - [{String.Join(",", keywords)}]";
                        }
                        else
                        {
                            Message.Text = $"<br><label style=\"color:red\">Failed to locate any keywords</label>";
                        }
                    }
                    else
                    {
                        Message.Text = "<br><label style=\"color:red\">Please check that the interface is correctly configured</label>";
                    }
                }
                catch (Exception ex)
                {
                    Message.Text = $"<br><label style=\"color:red\">Error testing watson nlp</label><br><small>{ex.Message}</small><br><small>{((ex.InnerException != null) ? ": " + ex.InnerException.Message + " : " : "")}</small>";
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