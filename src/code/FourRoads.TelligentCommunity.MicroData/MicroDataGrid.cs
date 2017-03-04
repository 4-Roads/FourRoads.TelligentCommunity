using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.XPath;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MicroData
{
    public class AddEditRowControl : Control
    {
        private DropDownList _contentTypes;
        private DropDownList _symanticType;
        private TextBox _selector;
        private TextBox _value;
        private Button _submit;
        private Button _cancel;
        private MicroDataGrid _dataRepeater;
        private HiddenField _postBackData;

        public AddEditRowControl(MicroDataGrid dataRepeater)
        {
            _dataRepeater = dataRepeater;
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            _contentTypes = new DropDownList() { ID = "contentType" };
            _symanticType = new DropDownList() { ID = "symanticType" };
            _selector = new TextBox() { ID = "selector" };
            _value = new TextBox(){ID= "value"};
            _submit = new Button() {ID = "submit"};
            _cancel = new Button() { ID = "cancel" };
            _postBackData = new HiddenField() { ID = "gridData" };

            Controls.Add(_contentTypes);
            Controls.Add(_symanticType);
            Controls.Add(_selector);
            Controls.Add(_value);
            Controls.Add(_submit);
            Controls.Add(_cancel);
            Controls.Add(_postBackData);
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            EnsureChildControls();

            if (_contentTypes.Items.Count == 0)
            {
                _contentTypes.Items.Add(new ListItem("Global", ""));

                foreach (IWebContextualContentType contentType in PluginManager.Get<IWebContextualContentType>())
                {
                    _contentTypes.Items.Add(new ListItem(contentType.Name, contentType.ContentTypeId.ToString()));
                }
            }

            if (_symanticType.Items.Count == 0)
            {
                _symanticType.Items.Add(MicroDataType.itemscope.ToString());
                _symanticType.Items.Add(MicroDataType.itemprop.ToString());
                _symanticType.Items.Add(MicroDataType.boolean.ToString());
            }

            _submit.Text = "Insert";
            _cancel.Text = "Cancel";
            _cancel.Attributes.Add("style" , "display:none");

            Page.ClientScript.RegisterClientScriptBlock(GetType(), "Insert", string.Format(@"
            $(function(){{
                var rebuildHighlighting = function(){{
                    $('#{5} table tr:even').css('background-color','transparent');    
                    $('#{5} table tr:odd').css('background-color','#eeeeee');    
                }};

                var clearSelection = function(){{
                    $('#{1} option:nth(0)').attr('selected', 'selected');
                    $('#{2} option:nth(0)').attr('selected', 'selected');
                    $('#{3}').val('');
                    $('#{4}').val('');

                    $('#{8}').hide();

                    rebuildHighlighting();
                }};

                //Clean up the plugin table
                $('.CommonFormFieldName' , $('#{0}').closest('table')).remove();

                $('#{0}').click(function(e){{
                    e.preventDefault();

                    var contentType = $('#{1} option:selected').text();
                    var type = $('#{2} option:selected').text();
                    var selector = $('#{3}').val();
                    var value = $('#{4}').val();

                    var selectedRow = $('#{5} table tr.selected');
                    var newRow = '<tr><td style=""width:10em;border-right:solid 1px #cccccc;border-left:solid 1px #cccccc;border-right:solid 1px #cccccc;"">'+ contentType +'</td><td style=""width:10em;border-right:solid 1px #cccccc;"">'+ type +'</td><td style=""border-right:solid 1px #cccccc;"">'+ selector +'</td><td style=""width:10em;border-right:solid 1px #cccccc;"">'+ value +'</td><td style=""width:60px;border-right:solid 1px #cccccc;""><input class=""micro-data-edit"" style=""padding:5px"" type=""image"" src=""{6}"" ><input class=""micro-data-delete"" style=""padding:5px"" type=""image"" src=""{7}"" ></td></tr>';

                    if (selectedRow.length > 0){{
                        selectedRow.after(newRow);
                        selectedRow.remove();
                    }}else{{
                        $('#{5} table tr:last').after(newRow);
                    }}

                    clearSelection();

                    sumbitHandler_{0}();

                    return false;
                }});

                $('#{8}').click(function(e){{
                    e.preventDefault();
                   $('#{5} table tr.selected').removeClass('selected');
                   clearSelection();

                    sumbitHandler_{0}();

                   return false;
                }});

                $('#{5}').on( 'click', '.micro-data-delete' , function(e){{
                    e.preventDefault();
                    $(this).closest('tr').remove();
                    rebuildHighlighting();

                    sumbitHandler_{0}();

                    return false;
                }});

                $('#{5}').on( 'click' ,'.micro-data-edit' , function(e){{
                    e.preventDefault();
                    
                    $('#{5} table tr.selected').removeClass('selected');

                    var row = $(this).closest('tr');
                    row.addClass('selected');

                    var contentTypeText = $('td:nth(0)' , row).text();
                    var type = $('td:nth(1)' , row).text();

                    $('#{1} option:contains(""' + contentTypeText + '"")').attr('selected', 'selected');
                    $('#{2} option:contains(""' + type + '"")').attr('selected', 'selected');
                    $('#{3}').val($('td:nth(2)' , row).text());
                    $('#{4}').val($('td:nth(3)' , row).text());

                    $('#{8}').show();

                    sumbitHandler_{0}();

                    return false;
                }});
            }});

            function convertType_{0}(strType){{
                 return $('#{1} option:contains(""' + strType + '"")').val();
            }}


            function sumbitHandler_{0}(){{
                   var postBackData = '<microData>';

                    $('#{5} table tr').each(function(index ,row){{
                        if (index > 0){{
                            postBackData += '<entry contentType=  ""'+ convertType_{0}($('td:nth(0)' , row).text()) +'"" entryType=""'+ $('td:nth(1)' , row).text() +'"" selector=""'+ $('td:nth(2)' , row).text() +'"" value=""'+ $('td:nth(3)' , row).text() +'"" />';                       
                        }}
                    }});

                    postBackData += '</microData>';
                    $('#{9}').val(postBackData); 

                    return true;
            }}
            ", _submit.ClientID, _contentTypes.ClientID, _symanticType.ClientID, _selector.ClientID, _value.ClientID, _dataRepeater.ClientID ,
             Page.ClientScript.GetWebResourceUrl(GetType(), "FourRoads.TelligentCommunity.MicroData.edit.png"), Page.ClientScript.GetWebResourceUrl(GetType(), "FourRoads.TelligentCommunity.MicroData.delete.png"),
             _cancel.ClientID, _postBackData.ClientID), true);

            ScriptManager.RegisterOnSubmitStatement(Page, Page.GetType(), "sumbitHandler", string.Format("sumbitHandler_{0}()",_submit.ClientID));
        }

        public string GetPostBackData()
        {
            return Page.Request.Form[_postBackData.UniqueID];
        }

    }

    public class MicroDataItemTemplate : ITemplate
    {
        private ListItemType _type;

        public MicroDataItemTemplate(ListItemType type)
        {
            _type = type;
        }

        public void InstantiateIn(Control container)
        {
            switch (_type)
            {
                case ListItemType.Header:
                    container.Controls.Add(new LiteralControl("<div style=\"height:250px;overflow-x:scroll;overflow-y:auto;\"><table style=\"widthg:100%\" cellspacing=\"0\">"));
              
                    var li = new HtmlGenericControl("tr");
                    container.Controls.Add(li);
                    li.Attributes.Add("style", "font-weight: bold;");

                    var contentType = new HtmlGenericControl("td") {InnerText = "Content Type"};
                    contentType.Attributes.Add("style", "width:10em;border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;border-left:solid 1px #cccccc;;border-right:solid 1px #cccccc;");
                    li.Controls.Add(contentType);

                    var type = new HtmlGenericControl("td") {InnerText = "Type"};
                    type.Attributes.Add("style", "width:10em;border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(type);

                    var selector = new HtmlGenericControl("td") {InnerText = "Selector"};
                    selector.Attributes.Add("style", "border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(selector);

                    var value = new HtmlGenericControl("td") {InnerText = "Value"};
                    value.Attributes.Add("style", "border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(value);

                    var actions = new HtmlGenericControl("td") { InnerText = "" };
                    actions.Attributes.Add("style", "width:60px;border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(actions);
                    break;

                case ListItemType.Footer:
                    container.Controls.Add(new LiteralControl("</table></div>"));
                    break;

                case ListItemType.Item:
                case ListItemType.AlternatingItem:
                    li = new HtmlGenericControl("tr"){ ID = "row" };
                    container.Controls.Add(li);

                    if (ListItemType.AlternatingItem == _type)
                    {
                        li.Attributes.Add("style", "background-color:#eeeeee;");
                    }

                    contentType = new HtmlGenericControl("td") { ID = "contentType" };
                    contentType.Attributes.Add("style", "padding:3px;width:10em;border-right:solid 1px #cccccc;border-left:solid 1px #cccccc;;border-right:solid 1px #cccccc;");
                    li.Controls.Add(contentType);

                    type = new HtmlGenericControl("td") { ID = "symanticType" };
                    type.Attributes.Add("style", "padding:3px;width:10em;border-right:solid 1px #cccccc;");
                    li.Controls.Add(type);

                    selector = new HtmlGenericControl("td") { ID = "selector" };
                    selector.Attributes.Add("style", "padding:3px;border-right:solid 1px #cccccc;");
                    li.Controls.Add(selector);

                    value = new HtmlGenericControl("td") { ID = "value" };
                    value.Attributes.Add("style", "padding:3px;border-right:solid 1px #cccccc;");
                    li.Controls.Add(value);

                    actions = new HtmlGenericControl("td") { ID = "actions" };
                    actions.Attributes.Add("style", "padding:3px;border-right:solid 1px #cccccc;");
                    li.Controls.Add(actions);

                    //Need two buttons edit and delete
                    var editButton = new HtmlInputImage() {ID = "edit"};
                    editButton.Attributes.Add("class","micro-data-edit");
                    editButton.Attributes.Add("style", "padding:5px");
                    actions.Controls.Add(editButton);

                    var deleteButton = new HtmlInputImage() { ID = "delete" };
                    deleteButton.Attributes.Add("class", "micro-data-delete");
                    deleteButton.Attributes.Add("style", "padding:5px");
                    actions.Controls.Add(deleteButton);

                    break;
            }

            container.DataBinding += ContainerOnDataBinding;
        }

        private void ContainerOnDataBinding(object sender, EventArgs eventArgs)
        {
            var item = sender as RepeaterItem;
            if (item != null)
            {
                if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
                {
                    IXPathNavigable nav = item.DataItem as IXPathNavigable;

                    if (nav != null)
                    {
                        var navigator = nav.CreateNavigator();

                        var row = item.FindControl("row") as HtmlGenericControl;
                        var contentType = item.FindControl("contentType") as HtmlGenericControl;
                        var symanticType = item.FindControl("symanticType") as HtmlGenericControl;
                        var selector = item.FindControl("selector") as HtmlGenericControl;
                        var value = item.FindControl("value") as HtmlGenericControl;
                        var editButton = item.FindControl("edit") as HtmlInputImage;
                        var deleteButton = item.FindControl("delete") as HtmlInputImage;

                        editButton.Src = item.Page.ClientScript.GetWebResourceUrl(GetType(), "FourRoads.TelligentCommunity.MicroData.edit.png");
                        deleteButton.Src = item.Page.ClientScript.GetWebResourceUrl(GetType(), "FourRoads.TelligentCommunity.MicroData.delete.png");

                        row.Attributes.Add("data-index", item.ItemIndex.ToString());
                        contentType.InnerText = FindContentType(navigator.GetAttribute("contentType" , string.Empty));
                        symanticType.InnerText = navigator.GetAttribute("entryType", string.Empty);
                        selector.InnerText = navigator.GetAttribute("selector", string.Empty);
                        value.InnerText = navigator.GetAttribute("value", string.Empty);
                    }
                }
            }
        }

        private string FindContentType(string guidContentType)
        {
            IWebContextualContentType contentItem = null;

            if (!string.IsNullOrWhiteSpace(guidContentType))
            {
                Guid contentTypeId;
                if (Guid.TryParse(guidContentType, out contentTypeId))
                {
                    contentItem = PluginManager.Get<IWebContextualContentType>().FirstOrDefault(f => f.ContentTypeId == contentTypeId);
                }
            }

            return contentItem != null ? contentItem.Name : "Global";
        }
    }

    public class MicroDataGrid : Control, INamingContainer, IPropertyControl
    {
        private AddEditRowControl _addEditRowControl;
        private Repeater _repeater;
        private XmlDataSource _configurationDataSource;

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            if (_repeater == null)
            {
                _repeater = new Repeater();
                _repeater.ID = "repeater";
                _repeater.ViewStateMode = ViewStateMode.Disabled;

                _addEditRowControl = new AddEditRowControl(this) { ID = "addeditrow" };

                Controls.Add(_repeater);
                Controls.Add(_addEditRowControl);
            }
        }

        protected override void OnInit(EventArgs e)
        {
            EnsureChildControls();

            base.OnInit(e);

            _repeater.HeaderTemplate = new MicroDataItemTemplate(ListItemType.Header);
            _repeater.ItemTemplate = new MicroDataItemTemplate(ListItemType.Item);
            _repeater.AlternatingItemTemplate = new MicroDataItemTemplate(ListItemType.AlternatingItem);
            _repeater.FooterTemplate = new MicroDataItemTemplate(ListItemType.Footer);

        }

        protected override void Render(HtmlTextWriter writer)
        {
            writer.WriteBeginTag("div");
            writer.WriteAttribute("id" , ClientID);

            base.Render(writer);

            writer.WriteEndTag("div");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Page.IsPostBack)
            {
                CreateDataSource(_addEditRowControl.GetPostBackData());
            }

            if ( !string.IsNullOrWhiteSpace(_configurationDataSource.Data) )
            {
                //Set up the grid 
                _repeater.DataSource = _configurationDataSource;
                _repeater.DataBind();
            }
        }

        private void CreateDataSource(string data)
        {
            _configurationDataSource = new XmlDataSource();
            _configurationDataSource.EnableCaching = false;
            _configurationDataSource.Data = data;
            ViewState["SourceData"] = data;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            string xml = value as string;

            if (string.IsNullOrWhiteSpace(xml))
            {
                //Default values
                xml = MicroDataSerializer.Serialize(MicroDataDefaultData.Entries);
            }

            CreateDataSource(xml);
        }

        public object GetConfigurationPropertyValue()
        {
            return _configurationDataSource.Data;
        }

        public Property ConfigurationProperty { get; set; }
        public ConfigurationDataBase ConfigurationData { get; set; }
        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public Control Control
        {
            get { return this; }
        }
    }
}