// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Components.Logic;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.InlineContent.CentralizedFileStore;
using FourRoads.TelligentCommunity.InlineContent.Controls;
using FourRoads.TelligentCommunity.InlineContent.HeaderExtensions;
using FourRoads.TelligentCommunity.InlineContent.Security;
using Telligent.Common;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;

namespace FourRoads.TelligentCommunity.InlineContent.ScriptedContentFragments
{
    public class InlineContentPart : ConfigurableContentFragmentBase, ITranslatablePlugin, IPluginGroup, IFileEmbeddableContentType,ISingletonPlugin
    {
        private ITranslatablePluginController _translatablePluginController;
        private IFileEmbeddableContentTypeController _ftController;

        public override bool HasRequiredContext(Control control)
        {
            if (PluginManager.IsEnabled(this))
            {
                return base.HasRequiredContext(control);
            }

            return false;
        }

        public override bool ShowHeaderByDefault
        {
            get { return false; }
        }

        public override string FragmentName
        {
            get { return TranslatablePluginController.GetLanguageResourceValue("fragment_name"); }
        }

        public override string FragmentDescription
        {
            get { return TranslatablePluginController.GetLanguageResourceValue("fragment_description"); }
        }

        public override string GetAdditionalCssClasses(Control control)
        {
            string results = base.GetAdditionalCssClasses(control);

            return results + " inlinecontent";
        }

        protected override bool IsCacheable
        {
            get
            {
                return false;
            }
        }

        public override string GetFragmentHeader(Control control)
        {
            string headerText = GetStringValue("headerTitle", string.Empty);

            if (headerText == "${resource:default_property_title}" && ContextualMode != ContextMode.Name)
            {
                ContextualItem(a =>
                {
                    headerText = a.HtmlName("web");

                }, c =>
                {
                    headerText = c.HtmlName("web");
                },
                ta =>
                {
                        
                });
            }

            return headerText;
        }

        private enum ContextMode
        {
            Name=1,
            Context=0,
            GroupContext=2
        }

        public override PropertyGroup[] GetPropertyGroups()
        {
            PropertyGroup group = new PropertyGroup("GeneralSettings",TranslatablePluginController.GetLanguageResourceValue("fragment_propertygroup_general"), 0);

            Property headerTitle = new Property("headerTitle",
                TranslatablePluginController.GetLanguageResourceValue("fragment_property_title"), PropertyType.String, 0, "${resource:default_property_title}") 
                                            {ControlType = typeof (ContentFragmentTokenStringControl)};

            group.Properties.Add(headerTitle);

            PropertyGroup defaultContent = new PropertyGroup("DefaultContent", TranslatablePluginController.GetLanguageResourceValue("fragment_propertygroup_defaultContent"), 0);

        	Property property = new Property("default_content",TranslatablePluginController.GetLanguageResourceValue(
        	                                 	"fragment_property_content"), PropertyType.Html, 1, ""){ControlType = typeof (HtmlEditorStringControl)};

        	property.Attributes["EnableHtmlScrubbing"] = "false";
            property.Attributes["width"] = "100%";
            property.Attributes["height"] = "300px";
            property.Attributes["enablehtmlediting"] = "true";
            property.Attributes["ContentTypeId"] = InlineContentPart.InlineContentContentTypeId.ToString();
            defaultContent.Properties.Add(property);


            PropertyGroup anoymousContent = new PropertyGroup("AnonymousContent", TranslatablePluginController.GetLanguageResourceValue("fragment_propertygroup_anonymousContent"), 0);

            property = new Property("anonymous_content", TranslatablePluginController.GetLanguageResourceValue(
                                    "fragment_property_content"), PropertyType.Html, 1, "") { ControlType = typeof(HtmlEditorStringControl) };

            property.Attributes["EnableHtmlScrubbing"] = "false";
            property.Attributes["width"] = "100%";
            property.Attributes["height"] = "300px";
            property.Attributes["enablehtmlediting"] = "true";
            property.Attributes["ContentTypeId"] = InlineContentPart.InlineContentContentTypeId.ToString();
            anoymousContent.Properties.Add(property);


            property = new Property("inlinecontenttype",
                TranslatablePluginController.GetLanguageResourceValue("fragment_property_inlinecontent_type"),
                PropertyType.String, 2, "Contextual")
            {
                DescriptionResourceName ="fragment_property_inlinecontent_type_description"
            };

            property.SelectableValues.Add(new PropertyValue("Contextual", TranslatablePluginController.GetLanguageResourceValue("fragment_property_inlinecontent_contextual"), (int)ContextMode.Context));
            property.SelectableValues.Add(new PropertyValue("GroupContextual", TranslatablePluginController.GetLanguageResourceValue("fragment_property_inlinecontent_groupcontextual"), (int)ContextMode.GroupContext));
            property.SelectableValues.Add(new PropertyValue("ByName", TranslatablePluginController.GetLanguageResourceValue("fragment_property_inlinecontent_byname"), (int)ContextMode.Name));

            group.Properties.Add(property);

            property = new Property("dynamicname", TranslatablePluginController.GetLanguageResourceValue("fragment_property_inlinecontent_dynamicname"), PropertyType.Bool, 3, bool.FalseString)
            {
                DescriptionResourceName ="fragment_property_inlinecontent_dynamicname_description"
            };

            group.Properties.Add(property);

            property = new Property("inlinecontentname", TranslatablePluginController.GetLanguageResourceValue("fragment_property_inlinecontent_name"), PropertyType.String, 4, string.Empty);
            group.Properties.Add(property);

            return new[] { group , defaultContent , anoymousContent };
        }

        protected void ContextualItem(Action<IApplication> applicationUse, Action<IContainer> containerUse, Action<string[]> tagsUse)
        {
            IApplication currentApplication = null;
            IContainer currentContainer = null;
            string[] tags = null;

            foreach (var contextItem in PublicApi.Url.CurrentContext.ContextItems.GetAllContextItems())
            {
                var app = PluginManager.Get<IApplicationType>().FirstOrDefault(a => a.ApplicationTypeId == contextItem.ApplicationTypeId);

                if (app != null && contextItem.ApplicationId.HasValue)
                {
                    IApplication application = app.Get(contextItem.ApplicationId.Value);

                    if (application != null)
                    {
                        if (application.Container.ContainerId != application.ApplicationId)
                            currentApplication = application;
                    }
                }
                
                var container = PluginManager.Get<IContainerType>().FirstOrDefault(a => a.ContainerTypeId == contextItem.ContainerTypeId);

                if (container != null && contextItem.ContainerId.HasValue && contextItem.ContainerTypeId == PublicApi.Groups.ContainerTypeId)
                {
                    currentContainer = container.Get(contextItem.ContainerId.Value);
                }

                if (contextItem.TypeName == "Tags")
                {
                    tags = contextItem.Id.Split(new[] {'/'});
                }
            }

            switch (ContextualMode)
            {
                case ContextMode.GroupContext:
                    if (currentContainer != null)
                        containerUse(currentContainer);
                    break;
                case ContextMode.Context:
                    {
                        if (currentApplication != null)
                            applicationUse(currentApplication);
                        else if (currentContainer != null)
                            containerUse(currentContainer);

                        if (tags != null)
                            tagsUse(tags);
                    }
                    break;
            }
        }

        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA1.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public override void AddContentControls(Control control)
        {
            try
            {
                InlineContentControl contentControl = new InlineContentControl(this);

                contentControl.ID = "inlinecontent";
                contentControl.DefaultContent = GetContentText();
                contentControl.DefaultAnonymousContent = GetAnonymousContentText();

                if (DynamicName)
                {
                    contentControl.InlineContentName  = PublicApi.Url.CurrentContext.PageName + "_";
                }

                if (ContextualMode == ContextMode.GroupContext || ContextualMode == ContextMode.Context)
                {
                    //This allows the control to have several instances on a single page
                    if (!string.IsNullOrWhiteSpace(InlineContentName))
                        contentControl.InlineContentName += InlineContentName + "_";

                    ContextualItem(a =>
                    {
                        contentControl.InlineContentName += a.ApplicationId.ToString();

                    }, c =>
                    {
                        if (c != null && c.ContainerId != PublicApi.Groups.Root.ContainerId)
                        {
                            contentControl.InlineContentName += c.ContainerId.ToString();
                        }
                        else
                        {
                            contentControl.InlineContentName += "SiteRoot";
                        }
                    }, ta =>
                    {
                        contentControl.InlineContentName += GetHashString(string.Join("", ta.OrderBy(s => s)));
                    });
                }
                else
                {
                    contentControl.InlineContentName += InlineContentName;    
                }
                
                control.Controls.Add(contentControl);
            }
            catch (CSException csEx)
            {
                csEx.Log();
                control.Controls.Add(new LiteralControl(csEx.Message));
            }
            catch (Exception ex)
            {
                new TCException( "Inline Content Exception", ex).Log();
                control.Controls.Add(new LiteralControl(ex.Message));
            }
        }

        private bool DynamicName
        {
            get
            {
                return GetBoolValue("dynamicName", false);
            }
        }

        private  ContextMode ContextualMode
        {
            get
            {
                switch (GetStringValue("inlinecontenttype", "Contextual"))
                {
                    case "ByName":
                        return ContextMode.Name;

                    case "Contextual":
                        return ContextMode.Context;
                }

                return ContextMode.GroupContext;
            }
        }

        protected string InlineContentName
        {
            get { return GetStringValue("inlinecontentname", string.Empty); }
        }

        public override void AddPreviewContentControls(Control control)
        {
            string html = GetContentText();

            control.Controls.Add(new Literal() {Text = html});
        }

        public string GetContentText()
        {
            return GetHtmlValue("default_content", "");
        }

        public string GetAnonymousContentText()
        {
            return GetHtmlValue("anonymous_content", "");
        }

        public void Initialize()
        {
            

        }

        public string Name
        {
            get { return "4 Roads - Inline Content Plugin"; }
        }

        public string Description
        {
            get { return "This plugin allows a user with ManageContent permission to edit content on the page"; }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translatablePluginController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation[] defaultTranslation = new[] {new Translation("en-us")};

                defaultTranslation[0].Set("fragment_name", "4 Roads - Inline Content");
                defaultTranslation[0].Set("fragment_description", "Provides an inline content widget for users with the Manage Content permission");
                defaultTranslation[0].Set("fragment_propertygroup_general", "General");
                defaultTranslation[0].Set("fragment_propertygroup_defaultContent", "Default Content");
                defaultTranslation[0].Set("fragment_propertygroup_anonymousContent", "Anonymous Override Content");
                defaultTranslation[0].Set("fragment_property_content", "Default Content Markup");
                defaultTranslation[0].Set("fragment_property_inlinecontent_name", "Inline Content Name");
                defaultTranslation[0].Set("fragment_property_css", "CSS Markup");
                defaultTranslation[0].Set("fragment_property_title", "<Context Based>");
                defaultTranslation[0].Set("default_property_title", "Title");

                defaultTranslation[0].Set("fragment_property_inlinecontent_type_description", "Select the type of context used to categorize the configuration data");
                defaultTranslation[0].Set("fragment_property_inlinecontent_type", "Mode");
                defaultTranslation[0].Set("fragment_property_inlinecontent_contextual", "Current Context");
                defaultTranslation[0].Set("fragment_property_inlinecontent_groupcontextual", "Current Group Context");
                defaultTranslation[0].Set("fragment_property_inlinecontent_byname", "By Name");
                defaultTranslation[0].Set("fragment_property_inlinecontent_dynamicname", "Use Page Name");
                defaultTranslation[0].Set("fragment_property_inlinecontent_dynamicname_description", "Use the current theme page name as part of the context");

                return defaultTranslation;
            }
        }

        protected ITranslatablePluginController TranslatablePluginController
        {
            get
            {
                if (_translatablePluginController == null)
                {
                    _translatablePluginController = new TranslatablePluginController(this, Services.Get<ITranslatablePluginService>());
                }

                return _translatablePluginController;
            }
        }


        public IEnumerable<Type> Plugins {
            get { return new[]
                {
                    typeof(InlineContentHeaderExtension),
                    typeof (InlineContentStore),
                    typeof (DependencyInjectionPlugin),
                     typeof (PermissionRegistrar)
                }; }
        }

        public bool CanAddFiles(int userId)
        {
            return true;
        }

        public void SetController(IFileEmbeddableContentTypeController controller)
        {
            _ftController = controller;
        }

        public string UpdateInlineContentFiles(string sourceContent)
        {
           var fs =  CentralizedFileStorage.GetFileStore(InlineContentLogic.FILESTORE_KEY);

            return _ftController.SaveFilesInHtml(sourceContent, file =>
            {
                using (Stream contentStream = file.OpenReadStream())
                {
                    ICentralizedFile centralizedFile = fs.AddFile(file.Path, file.FileName, contentStream, true);
                    if (centralizedFile != null)
                        return centralizedFile;

                    _ftController.InvalidFile(file, string.Empty);
                }
                return (ICentralizedFile)null;
            });
        }

        public Guid[] ApplicationTypes
        {
            get {return new Guid[0];}
        }

        public void AttachChangeEvents(IContentStateChanges stateChanges)
        {
            

        }
        public static Guid InlineContentContentTypeId = new Guid("{80F2C530-6183-4277-BE65-A0D771F13ABE}");

        public Guid ContentTypeId
        {
            get { return InlineContentContentTypeId; }
        }

        public string ContentTypeName
        {
            get { return "InlineContentType"; }
        }

        public Telligent.Evolution.Extensibility.Content.Version1.IContent Get(Guid contentId)
        {
            return null;
        }
    }
}