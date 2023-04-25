using System;
using System.IO;
using System.Web;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class DatePropertyTemplate : IPlugin, IPropertyTemplate
    {
        public string Name => "MFA Date Property Template";

        public string Description => "Enables rendering of date configuration properties";

        public void Initialize()
        {
        }

        public string[] DataTypes => new string[] { "Date" };

        public string TemplateName => "mfadate";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options => (PropertyTemplateOption[])null;

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            DateTime? nullable = options.Value == null ? new DateTime?() : (DateTime?)options.Value;
            if (options.Property.Editable)
            {
                writer.Write("<input type=\"text\" size=\"9\" id=\"");
                writer.Write(options.UniqueId);
                writer.Write("\"");
                if (nullable.HasValue)
                {
                    writer.Write(" value=\"");
                    writer.Write(DateTimeUtil.ToUtc(nullable.Value).ToString("o"));
                    writer.Write("\"");
                }

                writer.Write("/>");
                writer.Write($"<script type=\"text/javascript\">" +
                             $"$(function() {{\r\n    var api = {options.JsonApi};\r\n    var i = $('#{options.UniqueId}');\r\n    " +
                             $"i.glowDateTimeSelector($.extend({{}}, $.fn.glowDateTimeSelector.dateDefaults, " +
                             $"{{ showPopup: true, allowBlankValue: true }}));\r\n    api.register({{\r\n   " +
                             $"     val: function(val) {{ return i.val(); }},\r\n     " +
                             $"   hasValue: function() {{ return i.glowDateTimeSelector('val') != null; }}\r\n    }});\r\n  " +
                             $"  i.on('glowDateTimeSelectorChange', function() {{ \r\n" +
                             $"    api.changed(i.val()); \r\n " +
                             $"   }});\r\n}});\r\n</script>\r\n");
            }
            else
            {
                if (!nullable.HasValue)
                    return;
                writer.Write(HttpUtility.HtmlEncode(nullable.Value.ToString(options.DateFormatString)));
            }
        }
    }
}