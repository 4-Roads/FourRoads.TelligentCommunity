using System.IO;
using System.Web;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class PasswordPropertyTemplate : IPlugin, IPropertyTemplate
    {
        public string Name => "Password Text Property Template";

        public string Description => "Enables rendering of password text input";

        public void Initialize()
        {
        }

        public string[] DataTypes => new string[] {"String"};

        public string TemplateName => "frPasswordProperty";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options => (PropertyTemplateOption[]) null;

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            var passwordValue = options.Value == null ? string.Empty : options.Value.ToString();
            if (options.Property.Editable)
            {
                writer.Write(
                    "<input type=\"password\" autocomplete=\"off\" minlength=\"32\" maxlength=\"255\" size=\"32\"  id=\"");
                writer.Write(options.UniqueId);
                writer.Write("\"");
                if (!string.IsNullOrWhiteSpace(passwordValue))
                {
                    writer.Write(" value=\"");
                    writer.Write(passwordValue);
                    writer.Write("\"");
                }

                writer.Write("/>");
                writer.Write($@"<script type=""text/javascript"">                
                $(function() {{
                    var api = {options.JsonApi};
                    var i = $('#{options.UniqueId}');
                      api.register({{
                        val: function(val) {{ return i.val(); }},
                        hasValue: function() {{ return i.val() !== ''; }}
                    }});
                    i.change(function() {{ 
                        api.changed(i.val()); 
                    }});
                }});
                </script>");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(passwordValue)) return;

                writer.Write(HttpUtility.HtmlEncode(new string('•', passwordValue.Length)));
            }
        }
    }
}