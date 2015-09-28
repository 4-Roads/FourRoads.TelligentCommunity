using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace FourRoads.TelligentCommunity.MicroData
{
    public static class MicroDataSerializer
    {
        public static string Serialize(IEnumerable<MicroDataEntry> entries)
        {
            StringBuilder sb = new StringBuilder(3000);
            using (StringWriter sw = new StringWriter(sb))
            {
                using (XmlTextWriter xw = new XmlTextWriter(sw))
                {
                    xw.WriteStartElement("microData");

                    foreach (MicroDataEntry microDataEntry in entries)
                    {
                        xw.WriteStartElement("entry");

                        if (microDataEntry.ContentType.HasValue)
                        {
                            xw.WriteAttributeString("contentType", microDataEntry.ContentType.Value.ToString());
                        }

                        xw.WriteAttributeString("entryType", microDataEntry.Type.ToString());
                        xw.WriteAttributeString("selector", microDataEntry.Selector);
                        xw.WriteAttributeString("value", microDataEntry.Value);

                        xw.WriteEndElement();
                    }

                    xw.WriteEndElement();
                }
            }

            return sb.ToString();
        }

        public static IEnumerable<MicroDataEntry> Deserialize(string data)
        {
            List<MicroDataEntry> results = new List<MicroDataEntry>();

            using (StringReader rd = new StringReader(data))
            {
                using (XmlReader nodereader = XmlReader.Create(rd))
                {
                    nodereader.MoveToContent();
                    while (!nodereader.EOF)
                    {
                        if (nodereader.Name == "entry")
                        {
                            try
                            {
                                MicroDataEntry entry = new MicroDataEntry();

                                XElement el = XNode.ReadFrom(nodereader) as XElement;

                                if (el != null)
                                {
                                    IEnumerable<XAttribute> attr = el.Attributes();

                                    foreach (XAttribute xAttribute in attr)
                                    {

                                        if (!string.IsNullOrWhiteSpace(xAttribute.Value))
                                        {
                                            switch (xAttribute.Name.LocalName)
                                            {
                                                case "contentType":
                                                {
                                                    Guid contentType;
                                                    Guid.TryParse(xAttribute.Value, out contentType);
                                                    entry.ContentType = contentType;
                                                }
                                                    break;
                                                case "entryType":
                                                    entry.Type = (MicroDataType) Enum.Parse(typeof (MicroDataType), xAttribute.Value);
                                                    break;
                                                case "value":
                                                    entry.Value = xAttribute.Value;
                                                    break;
                                                case "selector":
                                                    entry.Selector = xAttribute.Value;
                                                    break;
                                            }
                                        }
                                    }

                                    results.Add(entry);
                                }
                            }
// ReSharper disable once EmptyGeneralCatchClause
                            catch
                            {
                                
                            }
                        }
                        else
                        {
                            nodereader.Read();
                        }
                    }
                }
            }

            return results.OrderBy(o => o.ContentType).ThenByDescending(o => o.Selector);
        }
    }
}