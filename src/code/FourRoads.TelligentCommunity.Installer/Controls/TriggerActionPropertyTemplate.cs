using System.IO;
using Telligent.Evolution.Extensibility.Configuration.Version1;

namespace FourRoads.TelligentCommunity.Installer.Controls
{
    public class TriggerActionPropertyTemplate : IPropertyTemplate
    {
        public string[] DataTypes => new string[] { "custom", "string" };

        public string TemplateName => "installer_triggerAction";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options
        {
            get
            {
                return new PropertyTemplateOption[4]
                {
                    new PropertyTemplateOption("resturl", "")
                    {
                        Description = "The rest url to call via the button."
                    },
                    new PropertyTemplateOption("callback", "")
                    {
                        Description = "The callback to make via the button."
                    },
                    new PropertyTemplateOption("data", "")
                    {
                        Description = "The data payload for the call."
                    }
                    ,
                    new PropertyTemplateOption("label", "")
                    {
                        Description = "The label for the button."
                    }

                };
            }
        }

        public string Name => "Trigger Action Property Template";

        public string Description => "Allows an action to be trigger via a button";

        public void Initialize()
        {
        }

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            string value = options.Value == null ? string.Empty : options.Value.ToString();
            string resturl = options.Property.Options["resturl"] ?? "";
            string callback = options.Property.Options["callback"] ?? "";
            string data = options.Property.Options["data"] ?? "";
            string label = options.Property.Options["label"] ?? "Click";

            if (!string.IsNullOrWhiteSpace(callback))
            {
                resturl = $"'{callback}'";
            }
            else
            {
                resturl = $"$.telligent.evolution.site.getBaseUrl() + '{resturl}'";
            }

            if (options.Property.Editable)
            {
                writer.Write("<div style='display:inline-block'>");
                writer.Write($"<a href ='#' class='button' id='{options.UniqueId}'>{label}</a>");
                writer.Write("</br>");
                writer.Write("</div>");

                string action = string.Empty;
                if (!string.IsNullOrWhiteSpace(resturl))
                {
                    action = @"
                    $.telligent.evolution.get({
                	url : " + resturl + @",
                	data : {"
                            + data +
                            @"},
                	success : function (response) {
                           $.telligent.evolution.notifications.show('Action requested', { type: 'success' });
                    },
                    error : function(xhr, desc, ex) {
                           $.telligent.evolution.notifications.show('Action request failed ' + desc, { type: 'error' });
                    }
                    });";
                }

                writer.Write(
                    $"\r\n<script type=\"text/javascript\">\r\n$(document).ready(function() {{\r\n " +
                    $"var api = {(object)options.JsonApi};\r\n    var i = $('#{(object)options.UniqueId}');\r\n       api.register({{\r\n        val: function(val) {{ return (typeof val == 'undefined') ? i.val() : i.val(val); }},\r\n        hasValue: function() {{ return i.val() != null; }}\r\n    }});\r\n  i.on('click', function(e) {{ e.preventDefault(); {action} }});\r\n i.on('change', function() {{ api.changed(i.val()); }});\r\n}});\r\n</script>\r\n");

            }
        }

    }
}
