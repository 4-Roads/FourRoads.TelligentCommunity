using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Hosting;
using System.Xml;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.DynamicConfiguration.Controls;

using FourRoads.TelligentCommunity.CustomEditor.Interfaces;
using FourRoads.TelligentCommunity.CustomEditor.CentralizedFileStore;
using FourRoads.TelligentCommunity.CustomEditor.Controls;
using Telligent.Common;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using ThemeFiles = Telligent.Evolution.Extensibility.UI.Version1.ThemeFiles;
using System.Collections.Specialized;
using FourRoads.Common.TelligentCommunity.Components;


namespace FourRoads.TelligentCommunity.CustomEditor
{
    public class CustomEditorPlugin : ICustomEditorPlugin, IConfigurablePlugin, IInstallablePlugin, IPluginGroup, IHttpCallback
    {
        private const string CommunityserverOverrideConfig = "communityserver_override.config";
        private const string FourroadsTelligentcommunityCustomeditorResources = "FourRoads.TelligentCommunity.CustomEditor.Resources";
        private readonly string[] _resourceNames = typeof(CustomEditorPlugin).Assembly.GetManifestResourceNames();
        private IPluginConfiguration _configuration;
        private IHttpCallbackController _callbackController;

        [Serializable]
        private class UpdateCssCallBackRule : IPropertyRule
        {
            Action<string> _callback;

            public void SetCallback(Action<string> callback)
            {
                _callback = callback;
            }

            public void LoadConfiguration(PropertyRule rule, XmlNode node)
            {
         
            }

            public void ValueChanged(Property property, ConfigurationDataBase data)
            {
                _callback(data.GetStringValue(property));
            }
        }

        private IPluginConfiguration Configuration
        {
            get { return _configuration; }
        }

        public string Description
        {
            get {
                return "Enables the use of custom editors"; }
        }

        public string Name
        {
            get { return "4 Roads - Custom Editor Extension"; }
        }

        public void Initialize()
        {

        }

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string EditorName
        {
            get
            {
                return Configuration.GetString("EditorName");
            }
        }

        public string Css
        {
            get 
            {
                return Configuration.GetString("Css");
            }
        }

        public bool EditorEnabled
        {
            get
            {
                return Configuration.GetBool("Enabled");
            }
        }

        public string FileLink
        {
            get
            {
                return Configuration.GetString("FileLink");
            }
        }

        public int DefaultWidth
        {
            get
            {
                return Configuration.GetInt("DefaultWidth");
            }
        }

        public int DefaultHeight
        {
            get
            {
                return Configuration.GetInt("DefaultHeight");
            }
        }

        public IEnumerable<ICentralizedFile> Files
        {
            get
            {
                ICentralizedFileStorageProvider fileStore = CustomEditorFileStore.GetFileStoreProvider();

                return fileStore.GetFiles("", PathSearchOption.AllPaths);
            }
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup optionsGroup = new PropertyGroup("options", "Options", 0);

                optionsGroup.Properties.Add(new Property("Enabled", "Enable this editor", PropertyType.Bool, 0, bool.FalseString));
                optionsGroup.Properties.Add(new Property("EditorName", "The name the user will see", PropertyType.String, 0, "Rich Text Editor"));
                optionsGroup.Properties.Add(new Property("FileLink", "File Link Variable", PropertyType.String, 2, "filelink"));
                optionsGroup.Properties.Add(new Property("DefaultWidth", "Default Width", PropertyType.Int, 2, "300"));
                optionsGroup.Properties.Add(new Property("DefaultHeight", "Default Height", PropertyType.Int, 2, "300"));

                PropertyGroup cssGroup = new PropertyGroup("css", "Css", 0);
                string defaultCss = EmbeddedResources.GetString(FourroadsTelligentcommunityCustomeditorResources + ".redactor.css");
                Property cssProp = new Property("Css", "Css", PropertyType.String, 0, defaultCss) { ControlType = typeof(MultilineStringControl) };

                var callback = new PropertyRule(typeof(UpdateCssCallBackRule) , false);

                ((UpdateCssCallBackRule)callback.Rule).SetCallback(UpdateThemeCss);

                cssProp.Rules.Add(callback);

                cssGroup.Properties.Add(cssProp);

                PropertyGroup files = new PropertyGroup("files", "Files", 1);
                files.Properties.Add(new Property("Files", "Files", PropertyType.Custom, 0, "") { ControlType = typeof(Files)});

                return new[] { optionsGroup, cssGroup, files };
            }
        }

        public IEnumerable<Type> Plugins
        {
            get
            {
                return new[]
                {
                    typeof(CustomEditorFileStore),
                    typeof (PermissionRegistrar)
                };
            }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        private void RemoveThemeCss()
        {

            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                if (theme.IsConfigurationBased)
                {
                    ThemeFiles.Remove(theme, ThemeContexts.Site, ThemeProperties.CssFiles, "custom-editor.css");
                    ThemeFiles.RemoveFactoryDefault(theme, ThemeProperties.CssFiles, "custom-editor.css");
                }
            }
        }

        private void UpdateThemeCss(string getString)
        {
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
	            if (theme.IsConfigurationBased)
	            {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(getString)))
		            {
		                ThemeFiles.AddUpdateFactoryDefault(theme, ThemeProperties.CssFiles, "custom-editor.css", stream, (int) stream.Length);
		                stream.Seek(0,  SeekOrigin.Begin);
                        ThemeFiles.AddUpdate(theme, ThemeContexts.Site, ThemeProperties.CssFiles, "custom-editor.css", stream, (int)stream.Length);
		            }
	            }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastInstalledVersion"></param>
        public void Install(Version lastInstalledVersion)
        {
            //TODO:Code clean-up
            Uninstall();

            try
            {
                string root = RootFsPath;

                if (root != null)
                {
                    string filename;

                    XmlDocument doc = LoadOverrideDocument(root, out filename);

                    const string overrideString = @"
                                          <Override  xpath=""/CommunityServer/Core/editors/editor[@name='Enhanced']"" mode=""remove"" product=""FourRoads.TelligentCommunity.CustomEditor"" />
                                          <Override xpath=""/CommunityServer/Core/editors"" mode=""add"" product=""FourRoads.TelligentCommunity.CustomEditor"">
                                            <editor name=""Enhanced"" type=""FourRoads.TelligentCommunity.CustomEditor.EditorWrapper, FourRoads.TelligentCommunity.CustomEditor"" default=""true"" resourceName=""EditorType_Enhanced_Name"">
                                              <editorOption name=""imageUpload"" value=""/customeditor.ashx""/>
                                            </editor>
                                          </Override>";

                    var docFragment = doc.CreateDocumentFragment();
                    docFragment.InnerXml = overrideString;

                    XmlNode node = doc.SelectSingleNode("Overrides");

                    if (node != null)
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        foreach (XmlNode nodeAdd in docFragment.SelectNodes("/Override"))
                            node.AppendChild(nodeAdd);
                    }

                    doc.Save(filename);
                }

                //Add the default files into the filestore
                string basePath = FourroadsTelligentcommunityCustomeditorResources + ".JavascriptFiles.";

                ICentralizedFileStorageProvider fileStore = CustomEditorFileStore.GetFileStoreProvider();

                foreach (string resourceName in _resourceNames.Where(r => r.StartsWith(basePath)))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    fileStore.AddFile("", Path.GetFileName(resourceName).Replace(basePath, ""), EmbeddedResources.GetStream(resourceName), false);
                }

                //Install CSS into theme's
                UpdateThemeCss(EmbeddedResources.GetString(FourroadsTelligentcommunityCustomeditorResources + ".redactor.css"));
            }
            catch (Exception ex)
            {
                new TCException(Telligent.Evolution.Components.CSExceptionType.UnknownError, "Failed to install Custom Editor", ex).Log();
            }

            HttpRuntime.UnloadAppDomain();
        }

        private static string RootFsPath
        {
            get { return HostingEnvironment.MapPath("~/") ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        private static XmlDocument LoadOverrideDocument(string root, out string comOverride)
        {
            XmlDocument doc = new XmlDocument();

            comOverride = Path.Combine(root, CommunityserverOverrideConfig);
            if (File.Exists(comOverride))
            {
                doc.Load(comOverride);
            }
            else
            {
                doc.LoadXml("<?xml version=\"1.0\"?><Overrides></Overrides>");
            }
            return doc;
        }

        public void Uninstall()
        {
            string root = RootFsPath;
            if (root != null)
            {
                string comOverride;

                XmlDocument doc = LoadOverrideDocument(root, out comOverride);
                XmlNodeList nodes = doc.SelectNodes("//Override[@product='FourRoads.TelligentCommunity.CustomEditor']");

                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        if (node.ParentNode != null)
                            node.ParentNode.RemoveChild(node);
                        else
                            doc.RemoveChild(node);
                    }
                }

                doc.Save(comOverride);

                string handler = Path.Combine(root, "customEditor.ashx");
                if (File.Exists(handler))
                {
                    File.Delete(handler);
                }

                RemoveThemeCss();
            }
        }

        private void UploadToApplication(HttpContextBase context, HttpPostedFileBase uploadedFile, string jsonId, int defaultHeight, int defaultWidth, StringBuilder returnString)
        {
            string contextId = context.Request.QueryString["uploaderId"] ?? string.Empty;

            if (string.IsNullOrEmpty(contextId))
            {
                throw new Exception("No file was received.");
            }

            string fname = Path.GetFileName(uploadedFile.FileName);

            ICentralizedFile uploadedCfsFile = null;

            Telligent.Evolution.Components.MultipleUploadFileManager.AddFile(fname, uploadedFile.InputStream, contextId);

            uploadedCfsFile = Telligent.Evolution.Components.MultipleUploadFileManager.GetCfsFile(fname, contextId);

            if (uploadedCfsFile != null)
            {
                string resizedHtml = PublicApi.UI.GetResizedImageHtml(CentralizedFileStorage.GetGenericDownloadUrl(uploadedCfsFile), defaultWidth, defaultHeight, new UiGetResizedImageHtmlOptions());

                returnString.Append(string.Concat("{\"" + jsonId + "\":\"", PublicApi.Javascript.Encode(uploadedCfsFile.GetDownloadUrl()), "\",\"resizedMarkup\":\"", PublicApi.Javascript.Encode(resizedHtml), "\",\"filename\":\"" + fname + "\"}"));
            }
        }

        public void ProcessRequest(HttpContextBase context)
        {
            HttpRequestBase request = context.Request;
            HttpResponseBase response = context.Response;
            StringBuilder retval = new StringBuilder();

            ICustomEditorPlugin plugin = PluginManager.GetSingleton<ICustomEditorPlugin>();

            if (plugin != null && plugin.EditorEnabled)
            {
                if (request.HttpMethod.Equals("POST"))
                {
                    HttpFileCollectionBase upload = request.Files;

                    for (int i = 0; i < upload.Count; i++)
                    {
                        UploadToApplication(context, upload[i], plugin.FileLink, plugin.DefaultWidth, plugin.DefaultHeight, retval);
                    }

                }
                response.Clear();
                response.AddHeader("Content-type", "application/json");
                response.Write(retval.ToString());
            }
            else
            {
                response.Clear();
                response.StatusCode = 403;
                response.Status = "Access denied";
            }
            response.End();
        }

        public void SetController(IHttpCallbackController controller)
        {
            _callbackController = controller;
        }


        public string GetCallbackUrl(string uploaderId, string clientID, Guid applicationTypeId, Guid containerTypeId, Guid contentTypeId, string authorizationId,  IList<Telligent.Evolution.Urls.Routing.IContextItem> list)
        {
            string callbackUrl = string.Empty;

            if (_callbackController != null)
            {
                NameValueCollection mvc = new NameValueCollection();

                mvc.Add("clientID", clientID);
                mvc.Add("uploaderId", uploaderId);
                mvc.Add("appTypeId", applicationTypeId.ToString());
                mvc.Add("containerTypeId", containerTypeId.ToString());
                mvc.Add("Authorization-Code", authorizationId);

                callbackUrl = _callbackController.GetUrl(mvc);
            }

            return callbackUrl;
        }
    }

}
