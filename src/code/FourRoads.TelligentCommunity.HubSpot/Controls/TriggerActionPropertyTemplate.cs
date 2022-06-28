using System.IO;
using Telligent.Evolution.Extensibility.Configuration.Version1;

namespace FourRoads.TelligentCommunity.HubSpot.Controls
{
    public class TriggerActionPropertyTemplate : IPropertyTemplate
    {
        public string[] DataTypes => new string[] { "custom", "string" };

        public string TemplateName => "hubspot_triggerAction";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options
        {
            get
            {
                return new PropertyTemplateOption[6]
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
                    new PropertyTemplateOption("buttonLabel", "")
                    {
                        Description = "The label for the button."
                    },
                    new PropertyTemplateOption("actionSuccessMessage", "")
                    {
                        Description = "The message to show if the action succeeds."
                    },
                    new PropertyTemplateOption("actionFailureMessage", "")
                    {
                        Description = "The message to show if the action fails."
                    }

                };
            }
        }

        public string Name => "Hubspot - Trigger Action Property Template";

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
            string buttonLabel = options.Property.Options["buttonLabel"] ?? "Click";
            string actionSuccessMessage = options.Property.Options["actionSuccessMessage"] ?? "Action requested";
            string actionFailureMessage = options.Property.Options["actionFailureMessage"] ?? "Action request failed";

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
                writer.Write($"<a href ='#' class='button' id='{options.UniqueId}'>{buttonLabel}</a>");
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
                            $@"}},
                	success : function (response) {{
                            if(response === '') {{
                                $.telligent.evolution.notifications.show('{actionFailureMessage}', {{ type: 'error' }});
                            }} else {{
                                $.telligent.evolution.notifications.show('{actionSuccessMessage}', {{ type: 'success' }});
                            }}
                    }},
                    error : function(xhr, desc, ex) {{
                            var error = desc;

                           var errorTitleMatch = xhr.responseText.match(/(?<=<title.*?>)(.*)(?=<\/title>)/);
                           if (errorTitleMatch != null) {{
                                error = errorTitleMatch[0];
                           }}

                           $.telligent.evolution.notifications.show('{actionFailureMessage}: ' + error, {{ type: 'error' }});
                    }}
                    }});";
                }

                writer.Write(
                        $@"<script type=""text/javascript"">
                                $(document).ready(function() {{
                                    var api = {(object)options.JsonApi};
                                    var i = $('#{(object)options.UniqueId}');
                                    api.register({{
                                        val: function(val) {{ return (typeof val == 'undefined') ? i.val() : i.val(val); }},
                                        hasValue: function() {{ return i.val() != null; }}
                                    }});
                                    i.on('click', function(e) {{ e.preventDefault(); {action} }});
                                    i.on('change', function() {{ api.changed(i.val()); }});
                               }});
                          </script>
                ");
            }
        }

    }
}
