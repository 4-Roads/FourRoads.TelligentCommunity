using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using FourRoads.Common.TelligentCommunity.Components.Logic;
using FourRoads.TelligentCommunity.InlineContent.Controls;
using FourRoads.TelligentCommunity.InlineContent.ScriptedContentFragments;
using FourRoads.TelligentCommunity.InlineContent.Security;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Telligent.Common;
using Telligent.Common.Diagnostics.Tracing.Web;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.InlineContent.Controls
{
    public class InlineContentControl : TraceableControl ,  IPostBackEventHandler
    {
        private InlineContentLogic _inlineContentLogic = new InlineContentLogic();
        private HtmlGenericControl _editor;
        private HtmlGenericControl _editItem;
        private HtmlAnchor _editAnchor;
        private HtmlButton _updateButton,_revertButton,_cancelButton ;
        private HtmlEditorStringControl _editorContent,_editorAnonymousContent;
        private ConfigurationDataBase _configurationDataBase;

        public InlineContentControl(ConfigurationDataBase configurationDataBase)
        {
            _configurationDataBase = configurationDataBase;
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            Page.ClientScript.RegisterStartupScript(GetType(), "ContentInlineDataHighlight",
            @"$(window).load(function () { 
                $('.content-inline-editor').hover( 
              function () { 
                $(this).addClass('highlight'); 
              },  
              function () { 
                $(this).removeClass('highlight');
              } 
            );});", true);
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            var customContent = _inlineContentLogic.GetInlineContent(InlineContentName);
            string contentToDisplay = null;

            if (customContent != null)
            {
                if (IsAnonymous())
                {
                    contentToDisplay = string.IsNullOrWhiteSpace(customContent.AnonymousContent) ? customContent.Content : customContent.AnonymousContent;
                }
                else
                {
                    contentToDisplay = customContent.Content;
                }
            }

            //Add the dynamic content in here
            Controls.Add(new Literal() { Text = contentToDisplay ?? (IsAnonymous() ? DefaultAnonymousContent : DefaultContent) });
        }

        protected bool IsAnonymous()
        {
            return (PublicApi.Users.AnonymousUserName == PublicApi.Users.AccessingUser.Username);
        }

        protected override void EnsureChildControls()
        {
            base.EnsureChildControls();

            if (!ReadOnly)
            {
                var customContent = _inlineContentLogic.GetInlineContent(InlineContentName);

                //Is the any content that is waiting to be published with a newer date, if so add an extra class
                Control[] tempCollection = new Control[Controls.Count];
                Controls.CopyTo(tempCollection, 0);
                Controls.Clear();

                HtmlGenericControl c = new HtmlGenericControl("span");
                c.ID = "content";

                c.Attributes.Add("class", "content-inline-editor");

                HtmlGenericControl ul = new HtmlGenericControl("ul");
                ul.ID = "items";
                ul.Attributes.Add("class", "content-inline-editor-buttons");

                c.Controls.Add(ul);

                //Add some button controls that are hidden
                _editItem = new HtmlGenericControl("li");
                _editItem.Attributes.Add("class", "edit");

                _editAnchor = new HtmlAnchor();
                _editAnchor.ID = "edit";
                _editAnchor.InnerHtml = "<span>Edit</span>";
                _editAnchor.HRef = "#";

                _editItem.Controls.Add(_editAnchor);
                ul.Controls.Add(_editItem);

                //Build the editor modal div
                _editor = new HtmlGenericControl("div");
                c.Controls.Add(_editor);
                _editor.Attributes.Add("style", "display:none");
                _editor.Attributes.Add("class", "fourroads-inline-content");
                _editor.ID = "editor";

                _editor.Controls.Add(new LiteralControl("<label>Default Content</Label>"));

                _editorContent = new HtmlEditorStringControl();
                _editorContent.ConfigurationData = _configurationDataBase;
                _editorContent.ContentTypeId = InlineContentPart.InlineContentContentTypeId;

                _editor.Controls.Add(_editorContent);

                _editorContent.Text = customContent != null ? customContent.Content ?? DefaultContent : DefaultContent;
                _editorContent.CssClass = "editor";
                _editorContent.ID = "editorcontent";

                _editor.Controls.Add(new LiteralControl("<label>Anonymous Content Override</Label>"));

                _editorAnonymousContent= new HtmlEditorStringControl();
                _editorAnonymousContent.ConfigurationData = _configurationDataBase;
                _editorAnonymousContent.ContentTypeId = InlineContentPart.InlineContentContentTypeId;

                _editor.Controls.Add(_editorAnonymousContent);

                _editorAnonymousContent.Text = customContent != null ? customContent.AnonymousContent ?? DefaultAnonymousContent : DefaultAnonymousContent;
                _editorAnonymousContent.CssClass = "editor";
                _editorAnonymousContent.ID = "editoranonymouscontent";

                HtmlGenericControl actions = new HtmlGenericControl("div");
                actions.Attributes.Add("style", "float:right");
                _editor.Controls.Add(actions);

                _cancelButton = new HtmlButton();
                _cancelButton.ID = "cancel";
                _cancelButton.Attributes.Add("class", "button cancel");
                _cancelButton.InnerText = "Cancel";
                actions.Controls.Add(_cancelButton);

                _updateButton = new HtmlButton();
                _updateButton.ID = "update";
                _updateButton.Attributes.Add("class", "button update");
                _updateButton.InnerText = "Update";

                actions.Controls.Add(_updateButton);

                _revertButton = new HtmlButton();
                _revertButton.ID = "revert";
                _revertButton.Attributes.Add("class", "button revert");
                _revertButton.InnerText = "Revert";

                actions.Controls.Add(_revertButton);

                Controls.Add(c);

                c.Controls.Add(new HtmlGenericControl("div") { InnerText = "click to edit" });

                foreach (Control control in tempCollection)
                {
                    c.Controls.Add(control);
                }
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            if (_editAnchor != null && _editor != null && _cancelButton != null && _updateButton != null && _revertButton != null)
            {

                Page.ClientScript.RegisterClientScriptBlock(GetType(), "inlinecontent-center-func" , @"
                    jQuery.fn.inlineCenter = function ()
                    {{
                        this.css('position','fixed');
                        this.css('top', ($(window).height() / 2) - (this.outerHeight() / 2));
                        this.css('left', ($(window).width() / 2) - (this.outerWidth() / 2));
                        return this;
                    }}",true);

            Page.ClientScript.RegisterClientScriptBlock(GetType(), "inlinecontent-initialization" + ClientID, string.Format(@"
                    $(function(){{ 
                        $('#{0}').click(function(e){{
                            e.preventDefault();
                                var modal = $('#{1}');

                                var top, left;

                                top = Math.max($(window).height() - modal.outerHeight(), 0) / 2;
                                left = Math.max($(window).width() - modal.outerWidth(), 0) / 2;

                                modal.css({{
                                    top:top + $(window).scrollTop(), 
                                    left:left + $(window).scrollLeft(),
                                    height: Math.min($(window).height(), modal.outerHeight()),
                                    overflow: 'scroll'
                                }});

                                modal.show();
                        }})

                        $('#{2}').click(function(e){{
                            $('#{1}').hide();
                        }});

                        $('#{3}').click(function(e){{
                            e.preventDefault();
                            $(this).attr('disabled', true);
                            var args = ""{{'content':"" + JSON.stringify({5}) + "",'anonymousConent':"" + JSON.stringify({6}) + ""}}"";
                            {4};
                            return false;
                        }});

                        $('#{7}').click(function(e){{
                            e.preventDefault();
                            $(this).attr('disabled', true);
                            var args = ""{{'content':null,'anonymousConent':null}}"";
                            {4};
                            return false;
                        }});
                   }});
                ", _editAnchor.ClientID, _editor.ClientID, _cancelButton.ClientID, _updateButton.ClientID, Page.ClientScript.GetPostBackEventReference(this, "args", false).Replace("'args'", "args"), _editorContent.GetContentScript(), _editorAnonymousContent.GetContentScript() , _revertButton.ClientID), true);
            }

            base.OnPreRender(e);
        }

        protected override void Render(HtmlTextWriter writer)
        {
            StringBuilder sb = new StringBuilder();
            using (HtmlTextWriter tw = new HtmlTextWriter(new System.IO.StringWriter(sb)))
            {
                base.Render(tw);
            }

            writer.Write(PublicApi.UI.Render(sb.ToString() , new UiRenderOptions()));
        }

        public string DefaultContent
        {
            get { return (string)(ViewState["DefaultContent"] ?? string.Empty); }
            set { ViewState["DefaultContent"] = value; }
        }

        public string DefaultAnonymousContent
        {
            get { return (string)(ViewState["DefaultAnonymousContent"] ?? string.Empty); }
            set { ViewState["DefaultAnonymousContent"] = value; }
        }

        public string InlineContentName
        {
            get { return (string)(ViewState["InlineContentName"] ?? string.Empty); }
            set { ViewState["InlineContentName"] = value; }
        }

        /// <summary>
        /// Gets or sets the width of the modal.
        /// </summary>
        /// <value>The width of the modal.</value>
        public int ModalWidth
        {
            get { return (int)(ViewState["ModalWidth"] ?? 640); }
            set { ViewState["ModalWidth"] = value; }
        }

        /// <summary>
        /// Gets or sets the height of the modal.
        /// </summary>
        /// <value>The height of the modal.</value>
        public int ModalHeight
        {
            get { return (int)(ViewState["ModalHeight"] ?? 510); }
            set { ViewState["ModalHeight"] = value; }
        }

        protected virtual bool ReadOnly
        {
            get { return !_inlineContentLogic.CanEdit; }
        }

        public void RaisePostBackEvent(string eventArgument)
        {
            if (!string.IsNullOrEmpty(InlineContentName))
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();

                dynamic item = serializer.Deserialize<object>(eventArgument);

                _inlineContentLogic.UpdateInlineContent(InlineContentName, item["content"], item["anonymousConent"]);
            }
        }
    }
}