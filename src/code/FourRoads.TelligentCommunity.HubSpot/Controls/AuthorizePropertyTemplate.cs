using System.IO;
using System.Web;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.HubSpot.Controls
{
    public class AuthorizePropertyTemplate : IPropertyTemplate, IHttpCallback
    {
        public string[] DataTypes => new[] { "custom", "string" };

        public string TemplateName => "hubspot_authorize";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options
        {
            get
            {
                return new PropertyTemplateOption[2]
                {
                    new PropertyTemplateOption("innerButtonLabel", "")
                    {
                        Description = "The label for the inner text of the button."
                    },
                    new PropertyTemplateOption("outerButtonLabel", "")
                    {
                        Description = "The label for above the button."
                    }
                };
            }
        }

        public string Name => "Hubspot - Authorize Property Template";

        public string Description => "Allows user to authorize using oAuth code";

        private IHttpCallbackController _callbackController;

        private HubspotCrm _hubSpotPlugin;

        public void Initialize()
        {
            _hubSpotPlugin = Telligent.Evolution.Extensibility.Version1.PluginManager.GetSingleton<HubspotCrm>();
        }

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            string innerButtonLabel = options.Property.Options["innerButtonLabel"] ?? "";
            string outerButtonLabel = options.Property.Options["outerButtonLabel"] ?? "Authorize oAuth Code";
            string inputId = options.UniqueId + "-input";
            string restUrl = $"'{_callbackController.GetUrl()}'";
            string data = $"refresh:true, authCode: $('#{(object)inputId}').val()";

            if (options.Property.Editable)
            {
                writer.Write($"<input size=\"40\" id='{inputId}' style='padding: 6px; width: 100%;'>");
                writer.WriteLine("</br>");
                writer.WriteLine("</br>");

                writer.Write($"<label class=\"field-item-name\" style='padding: 6px 0px;'>{outerButtonLabel}</label>");

                writer.Write("<span>");
                writer.Write($"<a href ='#' class='button' id='{options.UniqueId}'>{innerButtonLabel}</a>");
                writer.Write("</span>");

                writer.WriteLine("</br>");
                writer.WriteLine("</br>");

                string action = string.Empty;
                if (!string.IsNullOrWhiteSpace(restUrl))
                {
                    action = @"
                    

                    $.telligent.evolution.get({
                	url : " + restUrl + @",
                	data : {"
                             + data +
                             @"},
                	success : function (response) {
                           $.telligent.evolution.notifications.show('oAuth Syncronized', { type: 'success' });
                    },
                    error : function(xhr, desc, ex) {
                           var error = desc;

                           var errorTitleMatch = xhr.responseText.match(/(?<=<title.*?>)(.*)(?=<\/title>)/);
                           if (errorTitleMatch != null) {
                                error = errorTitleMatch[0];
                           }

                           $.telligent.evolution.notifications.show('Error while trying to setup oauth credentials: ' + error, { type: 'error' });
                    }
                    });";
                }

                writer.Write(
                    $@"<script type=""text/javascript"">
                                $(document).ready(function() {{
                                    var api = {(object)options.JsonApi};
                                    var button = $('#{(object)options.UniqueId}');
                                    api.register({{
                                        val: function(val) {{ return (typeof val == 'undefined') ? button.val() : button.val(val); }},
                                        hasValue: function() {{ return button.val() != null; }}
                                    }});
                                    button.on('click', function(e) {{ e.preventDefault(); {action} }});
                                    button.on('change', function() {{ api.changed(button.val()); }});
                               }});
                          </script>
                ");
            }
        }

        public void ProcessRequest(HttpContextBase httpContext)
        {
            string authCode = httpContext.Request.Params.Get("authCode");
            string url = Apis.Get<IUrl>().Absolute(Apis.Get<IUrl>().ApplicationEscape("~"));
            url = $"{url}hubspot/authorize";
            _hubSpotPlugin.InitialLinkoAuth(authCode, url);
        }

        public void SetController(IHttpCallbackController controller)
        {
            this._callbackController = controller;
        }
    }
}