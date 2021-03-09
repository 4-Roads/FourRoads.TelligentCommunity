using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MicroData
{
    public class MicroDataGrid : IPropertyTemplate, INamingContainer
    {
        public string[] DataTypes => new string[] { "custom", "string" };

        public string TemplateName => "microdata_grid";

        public bool SupportsReadOnly => true;
        
        public PropertyTemplateOption[] Options
        {
            get
            {
                return null;
            }
        }

        public string Name => "MicroData Grid Template";

        public string Description => "Provides and interface for managing schema.org micro data elements.";

        public void Initialize()
        {
            
        }

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            var insertLabel = options.Property.Options["insertlabel"] ?? "Insert";
            var saveLabel = options.Property.Options["updatelabel"] ?? "Update";
            var cancelLabel = options.Property.Options["cancellabel"] ?? "Cancel";

            if (options.Property.Editable)
            {
                var rows = GetRows(options);

                var borderStyle = "border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc";

                var contentTypeOptions = new StringBuilder();
                foreach (IWebContextualContentType contentType in PluginManager.Get<IWebContextualContentType>())
                {
                    contentTypeOptions.AppendLine($"<option value='{contentType.ContentTypeId.ToString()}'>{contentType.Name}</option>");
                }

                // html
                writer.Write(
                    $@"<div id='{options.UniqueId}_container' style='height:250px;overflow-x:scroll;overflow-y:auto;'>
                        <table style='width:100%' cellspacing='0'>
                           <thead>
                              <tr style='font-weight: bold;'>
                                 <th style='width:20%;{borderStyle};border-left:solid 1px #cccccc;'>Content Type</th>
                                 <th style='width:20%;{borderStyle};'>Type</th>
                                 <th style='width:30%;{borderStyle};'>Selector</th>
                                 <th style='width:30%;{borderStyle};'>Value</th>
                                 <th style='width:10%;{borderStyle};'></th>
                              </tr>
                            </thead>
                            <tbody>
                              {rows}
                           </tbody>
                        </table>
                        </div>
                        <div id='{options.UniqueId}_form' style='padding-top: 1rem;'>
                            <select name='{options.UniqueId}_form_contentType' id='{options.UniqueId}_form_contentType'>
                                {contentTypeOptions.ToString()}
                            </select>
                            <select name='{options.UniqueId}_form_type' id='{options.UniqueId}_form_type'>
                                <option value='itemscope' selected='selected'>itemscope</option>
                                <option value='itemprop'>itemprop</option>
                                <option value='boolean'>boolean</ option >
                            </select>
                            <input name='{options.UniqueId}_form_selector' type='text' id='{options.UniqueId}_form_selector' placeholder='Selector' />
                            <input name='{options.UniqueId}_form_value' type='text' id='{options.UniqueId}_form_value' placeholder='Value' />
                        </div>
                        <div id='{options.UniqueId}_addAction' style='padding-top: 1rem;'>
                            <button type='button' class='button' id='{options.UniqueId}_insert'>{insertLabel}</a>
                        </div>
                        <div id='{options.UniqueId}_editActions' style='padding-top: 1rem; display:none'>
                            <button type='button' class='button' id='{options.UniqueId}_save'>{saveLabel}</a>
                            <button type='button' class='button' id='{options.UniqueId}_cancel'>{cancelLabel}</a>
                        </div>
                        ");

                // javascript
                writer.Write($@"
                    <script type='text/javascript'>
                            $(document).ready(function() {{
                                var api = {options.JsonApi};
                                var i = $('#{options.UniqueId}');
                                var addBtn = $('#{options.UniqueId}_insert');
                                var saveBtn = $('#{options.UniqueId}_save');
                                var cancelBtn = $('#{options.UniqueId}_cancel');

                                addBtn.click(function(e){{
                                    e.preventDefault();
                                    var table = $('#{options.UniqueId}_container table tbody');
                                    var contentType = $('#{options.UniqueId}_form_contentType option:selected');
                                    var selector = $('#{options.UniqueId}_form_selector');
                                    var formValue = $('#{options.UniqueId}_form_value');
                                    var formType = $('#{options.UniqueId}_form_type');
                                    var rowId = '{options.UniqueId}_' + table.find('tr').length;
                                    var newRow = '<tr id=""' + rowId + '_row""><td id=""' + rowId + '_contentType"" data-obj=""ContentType"" data-val=""' + contentType.val() + '"" style=""padding:3px;border-right:solid 1px #cccccc;border-left:solid 1px #cccccc;border-right:solid 1px #cccccc;text-align:center;"">' + contentType.html() + '</td><td id=""' + rowId + '_symanticType"" data-obj=""Type"" style=""padding:3px;border-right:solid 1px #cccccc;"">' + formType.val() + '</td><td id=""' + rowId + '_selector"" data-obj=""Selector"" style=""padding:3px;border-right:solid 1px #cccccc;"">' + selector.val() + '</td><td id=""' + rowId + '_value"" data-obj=""Value"" style=""padding:3px;border-right:solid 1px #cccccc;"">' + formValue.val() + '</td><td id=""' + rowId + '_actions"" data-obj=""actions"" style=""padding:3px;border-right:solid 1px #cccccc;""><button type=""button"" class=""micro-data-edit"">Edit</button>  <button type=""button"" class=""micro-data-delete"">Delete</button></td></tr>';
                                    table.append(newRow);

                                    $('#{options.UniqueId}_form_selector').val('');
                                    $('#{options.UniqueId}_form_value').val('');

                                    initializeDelete();
                                    initializeEdit();

                                    api.changed();
                                    $.telligent.evolution.notifications.show('Entry added. Click Save to finalize changes.');
                                }});

                                saveBtn.click(function(e){{
                                    e.preventDefault();
                                    var tr = $('#{options.UniqueId}_container table tbody tr.selected');
                                    tr.find('td').each(function(i) {{
                                        var key = $(this).data('obj');
                                        if (key == 'ContentType'){{
                                            var contentType = $('#{options.UniqueId}_form_contentType option:selected');
                                            $(this).data('val', contentType.val());
                                            $(this).html(contentType.html());
                                        }}
                                        else if (key == 'Selector'){{
                                            $(this).html($('#{options.UniqueId}_form_selector').val());
                                        }}
                                        else if (key == 'Value'){{
                                            $(this).html($('#{options.UniqueId}_form_value').val());
                                        }}
                                        else if (key == 'Type'){{
                                            $(this).html($('#{options.UniqueId}_form_type').val());
                                        }}
                                    }});

                                    $('#{options.UniqueId}_container table tbody tr.selected').removeClass('selected');
                                    $('#{options.UniqueId}_form_selector').val('');
                                    $('#{options.UniqueId}_form_value').val('');
                                    $('#{options.UniqueId}_addAction').show();
                                    $('#{options.UniqueId}_editActions').hide();

                                    api.changed();
                                    $.telligent.evolution.notifications.show('Entry updated. Click Save to finalize changes.');
                                }});

                                cancelBtn.click(function(e){{
                                    e.preventDefault();
                                    $('#{options.UniqueId}_container table tbody tr.selected').removeClass('selected');
                                    $('#{options.UniqueId}_form_selector').val('');
                                    $('#{options.UniqueId}_form_value').val('');

                                    $('#{options.UniqueId}_addAction').show();
                                    $('#{options.UniqueId}_editActions').hide();
                                }});

                                function tableToJson(){{
                                    var tableRows = $('#{options.UniqueId}_container table tbody tr');
                                    var rows = [];
                                    tableRows.each(function () {{
                                        var row = {{}};
                                        var _tableRows = $(this);
                                        _tableRows.find('td').each(function(i) {{
                                            var _td = $(this);
                                            var key = _td.data('obj'),
                                                value = _td.html();
                                            if(key !== 'actions'){{
                                                if(key === 'ContentType' || key === 'Type'){{
                                                    value = _td.data('val');
                                                }}

                                                row[key] = value;
                                            }}
                                        }});

                                        rows.push(row);
                                    }});

                                    return rows;
                                }}

                                api.register({{
                                    val: function(val) {{ 
                                        if(typeof val == 'undefined'){{
                                            var json = tableToJson();
                                            var jsonString = JSON.stringify(json);
                                            i.val(jsonString);
                                            return jsonString;
                                        }}
                                        else {{
                                            i.val(val);
                                            return val;
                                        }}
                                    }},
                                    hasValue: function() {{ 
                                        var json = tableToJson();
                                        return json.length > 0; 
                                    }}
                                }});

                                function initializeDelete(){{
                                    var deleteBtn = $('#{options.UniqueId}_container table tbody .micro-data-delete');
                                    deleteBtn.click(function(e){{
                                        e.preventDefault();
                                        var tr = $(this).closest('tr');
                                        tr.remove();
                                        api.changed();
                                        $.telligent.evolution.notifications.show('Entry removed. Click Save to finalize changes.');
                                    }});
                                }}

                                function initializeEdit(){{
                                    var editBtn = $('#{options.UniqueId}_container table tbody .micro-data-edit');
                                    editBtn.click(function(e){{
                                        e.preventDefault();

                                        $('#{options.UniqueId}_container table tbody tr.selected').removeClass('selected');
                                        $('#{options.UniqueId}_form_selector').val('');
                                        $('#{options.UniqueId}_form_value').val('');

                                        var row = $(this).closest('tr');
                                        row.addClass('selected');

                                        var cells = row.find('td');
                                        var data = {{ }};
                                        row.find('td').each(function(i) {{
                                            var key = $(this).data('obj'),
                                                value = $(this).html();

                                            data[key] = value;
                                        }});

                                        $('#{options.UniqueId}_form_contentType option:contains(""' + data['ContentType'] + '"")').attr('selected', 'selected');
                                        $('#{options.UniqueId}_form_type option:contains(""' + data['Type'] + '"")').attr('selected', 'selected');
                                        $('#{options.UniqueId}_form_selector').val(data['Selector']);
                                        $('#{options.UniqueId}_form_value').val(data['Value']);

                                        $('#{options.UniqueId}_addAction').hide();
                                        $('#{options.UniqueId}_editActions').show();
                                    }});
                                }}

                                initializeDelete();
                                initializeEdit();
                ");

                writer.Write("});\r\n</script>");
            }
        }

        private string GetRows(IPropertyTemplateOptions options)
        {
            var borderStyle = "solid 1px #cccccc";
            var tdStyle = $"padding:3px;border-right:{borderStyle}";
            var rows = new StringBuilder();
            var entries = JsonConvert.DeserializeObject<MicroDataEntry[]>(options.Value.ToString());

            var index = 0;
            foreach (var entry in entries)
            {
                rows.AppendLine(RowTemplate(index, options.UniqueId, entry, tdStyle, borderStyle));
                index++;
            }

            return rows.ToString();
        }

        private string RowTemplate(int index, string uniqueId, MicroDataEntry entry, string tdStyle, string borderStyle)
        {
            var rowId = $"{uniqueId}_{index}";

            return
                $@"<tr id='{rowId}_row'><td id='{rowId}_contentType' data-obj='ContentType' data-val='{entry.ContentType}' style='{tdStyle};border-left:{borderStyle};border-right:{borderStyle};text-align:center;'>{FindContentType(entry.ContentType)}</td><td id='{rowId}_symanticType' data-obj='Type' style='{tdStyle};'>{entry.Type.ToString()}</td><td id='{rowId}_selector' data-obj='Selector' style='{tdStyle};'>{entry.Selector}</td><td id='{rowId}_value' data-obj='Value' style='{tdStyle};'>{entry.Value}</td><td id='{rowId}_actions' data-obj='actions' style='{tdStyle};'><button type='button' class='micro-data-edit'>Edit</button>  <button type='button' class='micro-data-delete'>Delete</button></td></tr>";
        }

        private string FindContentType(Guid? guidContentType)
        {
            IWebContextualContentType contentItem = null;

            if (guidContentType.HasValue)
            {
                contentItem = PluginManager.Get<IWebContextualContentType>().FirstOrDefault(f => f.ContentTypeId == guidContentType.Value);
            }

            return contentItem != null ? contentItem.Name : "Global";
        }
    }
}