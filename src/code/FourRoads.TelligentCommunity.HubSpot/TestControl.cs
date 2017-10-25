using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using FourRoads.TelligentCommunity.HubSpot.Models;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;
using TelligentProperty = Telligent.DynamicConfiguration.Components.Property;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class TestControl : WebControl, IPropertyControl, INamingContainer
    {
        protected Button TestButton;
        protected Literal Message;

        protected override void EnsureChildControls()
        {
            base.EnsureChildControls();

            if (TestButton == null)
            {
                TestButton = new Button();
                TestButton.ID = "TestBtn";
                TestButton.Text = "Test";
                TestButton.Click += LinkButton_Click;

                Controls.Add(TestButton);
            }
            
            if (Message == null)
            {
                Message = new Literal();
                Message.ID = "Message";
                Message.Text = "<br><label>Click to test hubspot API integration</label>";

                Controls.Add(Message);
            }
        }

        private void LinkButton_Click(object sender, EventArgs e)
        {
            var plg = PluginManager.GetSingleton<HubspotCrm>();

            if (plg != null)
            {
                try
                {
                    //var listContactPropertyGroups = plg.GetContactPropertyGroups();
                    //if (!listContactPropertyGroups.Exists(c => c.name == "crm_import"))
                    //{
                    //    ContactPropertyGroup contactPropertyGroup = new ContactPropertyGroup()
                    //    {
                    //        name = "crm_import",
                    //        displayName = "CRM Import"
                    //    };
                    //    var newContactPropertyGroup = plg.AddContactPropertyGroup(contactPropertyGroup);
                    //}

                    //var existingContactProperty = plg.GetContactProperty("crm_project_description");
                    //if (existingContactProperty == null || string.IsNullOrWhiteSpace(existingContactProperty.name))
                    //{
                    //    ContactProperty contactProperty = new ContactProperty()
                    //    {
                    //        name = "crm_project_description",
                    //        label = "CRM Project Description",
                    //        description = "CRM Project Description",
                    //        groupName = "crm_import",
                    //        type = "string",
                    //        fieldType = "textarea",
                    //        formField = true,
                    //        displayOrder = 1
                    //    };
                    //    var newContactProperty = plg.AddContactProperty(contactProperty);
                    //}

                    //Properties properties = new Properties() { properties = new List<Models.Property>() };
                    //properties.properties.Add(new Models.Property() { property = "crm_project_description", value = "testing 12345" });
                    //plg.UpdateContactProperties("bh@hubspot.com", properties);

                    //properties.properties.Clear();
                    //properties.properties.Add(new Models.Property() { property = "crm_project_description", value = "testing kb new" });
                    //plg.UpdateorCreateContact("kb@hubspot.com", properties);

                    //var contact = plg.GetUserProperties("kb@hubspot.com");
                    //if (contact.properties.crm_project_description != null)
                    //{
                    //    var value = contact.properties.crm_project_description;
                    //}

                    var listContacts = plg.GetContactPropertyGroups();
                    
                    if (listContacts != null && listContacts.Any())
                    {
                        Message.Text = $"<br><label style=\"color:green\">Found {listContacts.Count} contact property groups</label>";
                    }
                    else
                    {
                        Message.Text = "<br><label style=\"color:red\">Failed to read contact property groups</label>";
                    }
                }
                catch (Exception ex)
                {
                    Message.Text = $"<br><label style=\"color:red\">Error while trying to read contact property groups</label><br><small>{ex.Message}</small><br><small>{((ex.InnerException != null) ? ": " + ex.InnerException.Message + " : " : "")}</small>";
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