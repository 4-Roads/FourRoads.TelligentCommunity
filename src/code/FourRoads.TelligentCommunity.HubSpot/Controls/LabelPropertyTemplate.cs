using System;
using System.IO;
using Telligent.Evolution.Extensibility.Configuration.Version1;

namespace FourRoads.TelligentCommunity.HubSpot.Controls
{
    public class LabelPropertyTemplate : IPropertyTemplate
    {
        public void Initialize()
        {
        }

        public string Name => "Hubspot - Plain text label in a 'message' div";
        
        public string Description => "Renders default configuration value inside a div with class 'message'";
        
        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            var text = options.Property.DefaultValue;
            writer.WriteLine($@"<div class=""message"">{text}</div>");
        }

        public string[] DataTypes => new[] { "string", "custom" };
        
        public string TemplateName => "message_label";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options => Array.Empty<PropertyTemplateOption>();
    }
}