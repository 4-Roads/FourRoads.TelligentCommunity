using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Linq;
using FourRoads.TelligentCommunity.CustomEditor.Interfaces;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;

namespace FourRoads.TelligentCommunity.CustomEditor
{
    public class EditorWrapper : HtmlTextArea, ITextControl, ITextEditor, IScriptableTextEditor
    {
        private static IDictionary<String, String> _editorOptions = new Dictionary<String, String>();
        private bool _htmlModeEditingEnabled = true;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            //Editors in the control panel hack
            if (Page != null && Page.Master != null && Page.Master.Master != null)
            {
                ContentPlaceHolder placeholder = Page.Master.Master.FindControl("StyleRegion") as ContentPlaceHolder;
                if(placeholder == null && Page.Master.Master.Master != null)
                {
                    placeholder = Page.Master.Master.Master.FindControl("StyleRegion") as ContentPlaceHolder;
                }

                if (placeholder != null)
                {
                    placeholder.PreRender += (sender, b) =>
                    {
                        ICustomEditorPlugin plugin = PluginManager.GetSingleton<ICustomEditorPlugin>();

                        LiteralControl lit = new LiteralControl("<style type=\"text/css\">" + plugin.Css + "</style>");

                        ((Control)sender).Controls.Add(lit);

                    };
                }
            }
        }



        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);

            writer.Write(RenderEditor());
        }

        private string RenderEditor()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (EnableHtmlModeEditing)
            {
                ICustomEditorPlugin plugin = PluginManager.GetSingleton<ICustomEditorPlugin>();

                String name = new StringBuilder("EditorWrapper-").Append(plugin.EditorName).ToString();

                if (!Page.ClientScript.IsStartupScriptRegistered(GetType(), name))
                {
                    Page.ClientScript.RegisterStartupScript(GetType(), name, "");

                    foreach (ICentralizedFile file in plugin.Files)
                    {
                        stringBuilder.Append("<script type=\"text/javascript\" src='").Append(PublicApi.Html.EncodeAttribute(file.GetDownloadUrl())).Append("'></script>").AppendLine();
                    }
                }

                if (!Page.ClientScript.IsStartupScriptRegistered(GetType(), ClientID))
                {
                    HttpContext context = HttpContext.Current;

                    String uploaderId = Guid.NewGuid().ToString();

                    PageContext pageContext = PublicApi.Url.CurrentContext;
                    if (pageContext == null)
                    {
                        pageContext = PublicApi.Url.ParsePageContext(System.Web.HttpContext.Current.Request.Url.ToString());
                    }

                    var authCookie = context.Request.Cookies["AuthorizationCookie"];
                    string authValue = string.Empty;
                    if (authCookie != null)
                    {
                        authValue = authCookie.Value;
                    }

                    string callbackUrl = plugin.GetCallbackUrl(uploaderId, ClientID, pageContext.ApplicationTypeId.GetValueOrDefault(), pageContext.ContainerTypeId.GetValueOrDefault(), ContentTypeId.GetValueOrDefault(), authValue, pageContext.ContextItems.GetAllContextItems());

                    Telligent.Evolution.Urls.Routing.IContextItem group = pageContext.ContextItems.GetAllContextItems().FirstOrDefault<Telligent.Evolution.Urls.Routing.IContextItem>(g => g.TypeName == "Group");
                    bool sourceButton = true;
                    if (group != null)
                    {
                        sourceButton = PublicApi.Permissions.Get(PermissionRegistrar.CustomEditorSourceButton, PublicApi.Users.AccessingUser.Id.Value, group.ContentId.GetValueOrDefault(), pageContext.ContainerTypeId.Value).IsAllowed;
                    }
                    
                    stringBuilder.Append("<script type=\"text/javascript\">jQuery.fourroads.customEditor.Attach('").Append(ClientID).Append("','");
                    stringBuilder.Append(callbackUrl);
                    stringBuilder.Append("','").Append(callbackUrl).Append("&delete=true',").Append(SupportFileUpload.ToString().ToLower()).Append(",").Append(sourceButton.ToString().ToLower()).Append(")").Append("</script>");
                }
            }

            return stringBuilder.ToString();
        }

        string IScriptableTextEditor.Render()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("<textarea  id=\"");
            stringBuilder.Append(HttpUtility.HtmlAttributeEncode(ClientID));
            stringBuilder.Append("\" name=\"");
            stringBuilder.Append(HttpUtility.HtmlAttributeEncode(UniqueID));
            stringBuilder.Append("\"");
            if (Width != Unit.Empty || Height != Unit.Empty)
            {
                stringBuilder.Append(" style=\"");
                if (Width != Unit.Empty)
                {
                    stringBuilder.Append("width:");
                    stringBuilder.Append(Width);
                    stringBuilder.Append(";");
                }
                if (Height != Unit.Empty)
                {
                    stringBuilder.Append("height:");
                    stringBuilder.Append(Height);
                    stringBuilder.Append(";");
                }
                stringBuilder.Append("\"");
            }
            stringBuilder.Append(">");
            if (!string.IsNullOrEmpty(Text))
            {
                stringBuilder.Append(HttpUtility.HtmlEncode(Text));
            }
            stringBuilder.Append("</textarea>");

            stringBuilder.Append(RenderEditor());
   
            return stringBuilder.ToString();
        }

        /* ITextEditor implementation */

        public bool IsRichTextCapable
        {
            get { return true; }
        }

        private bool? supportsFileEmbeding;

        public bool SupportFileUpload
        {
            get
            {
                if (!supportsFileEmbeding.HasValue)
                {
                    supportsFileEmbeding = false;

                    IFileEmbeddableContentType fileEmbeddableContentType = PluginManager.Get<IFileEmbeddableContentType>().FirstOrDefault((f) => f.ContentTypeId == ContentTypeId);

                    if (fileEmbeddableContentType != null)
                    {

                        supportsFileEmbeding = fileEmbeddableContentType.CanAddFiles(Telligent.Evolution.Components.CSContext.Current.User.UserID);
                    }
                }

                return supportsFileEmbeding.Value;
            }
            set
            {
                supportsFileEmbeding = value;
            }
        }

        private int columns = 5;
        public int Columns 
        {
            get
            {
                return columns;
            }
            set
            {
                columns = value;
            }
        }

        private Unit height;
        public Unit Height 
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        private int rows;
        public int Rows 
        {
            get
            {
                return rows;
            }
            set
            {
                rows = value;
            }
        }

        private Unit width;
        public Unit Width 
        { 
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public String Text
        {
            get
            {
                return HttpUtility.HtmlDecode(this.InnerHtml);
            }
            set
            {
                this.InnerHtml = HttpUtility.HtmlEncode(value);
            }
        }

        public bool EnableHtmlModeEditing
        {
            get
            {
                ICustomEditorPlugin plugin = PluginManager.GetSingleton<ICustomEditorPlugin>();

                return _htmlModeEditingEnabled && (plugin != null && plugin.EditorEnabled);
            }
            set { _htmlModeEditingEnabled = value; }
        }

        public Guid? ContentTypeId { get; set; }

        public string GetContentScript()
        {
            if (EnableHtmlModeEditing)
            {
                return new StringBuilder("jQuery.fourroads.customEditor.GetContent('").Append(ClientID).Append("')").ToString();
            }

            return new StringBuilder("jQuery('#").Append(ClientID).Append("').val()").ToString();
        }

        public string GetBookmarkScript()
        {
            if (EnableHtmlModeEditing)
            {
                return string.Format("try {{ jQuery.fourroads.customEditor.UpdateBookmark(jQuery.fourroads.customEditor.GetEditor('{0}')); }} catch (e) {{ }}", ClientID);
            }

            return string.Empty;
        }

        public string GetContentInsertScript(string contentVariableName)
        {
            if (EnableHtmlModeEditing)
            {
                return new StringBuilder("jQuery.fourroads.customEditor.InsertContent('").Append(ClientID).Append("', ").Append(contentVariableName).Append(");").ToString();
            }

            return string.Empty;
        }

        public string GetContentUpdateScript(string contentVariableName)
        {
            if (EnableHtmlModeEditing)
            {
                return new StringBuilder("jQuery.fourroads.customEditor.UpdateContent('").Append(ClientID).Append("', ").Append(contentVariableName).Append(");").ToString();
            }

            return string.Empty;
        }

        public string GetFocusScript()
        {
            if (EnableHtmlModeEditing)
            {
                return new StringBuilder("jQuery.fourroads.customEditor.SetFocus('").Append(ClientID).Append("')").ToString();
            }
            return string.Empty;
        }

        public string GetAttachOnChangeScript(string function)
        {
            if (EnableHtmlModeEditing)
            {
                return string.Format("try {{ jQuery.fourroads.customEditor.AttachOnChangeHandler('{0}',{1}); }} catch(e) {{ }}", ClientID, function);
            }
            return string.Empty;
        }

        public bool IsSupported(HttpBrowserCapabilities browser)
        {
            return true;
        }

        public void ApplyConfigurationOption(string name, string value)
        {
            if (!_editorOptions.Keys.Contains(name))
            {
                _editorOptions.Add(name, value);
            }
        }

    }
}