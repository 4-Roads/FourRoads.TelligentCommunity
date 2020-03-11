// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using DryIoc;
using FourRoads.Common.TelligentCommunity.Components.Logic;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.InlineContent.CentralizedFileStore;
using FourRoads.TelligentCommunity.InlineContent.Security;
using Microsoft.Web.Infrastructure;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.UI.Version2;
using IContainer = Telligent.Evolution.Extensibility.Content.Version1.IContainer;
using IContent = Telligent.Evolution.Extensibility.Content.Version1.IContent;
using PropertyGroup = Telligent.Evolution.Extensibility.Configuration.Version1.PropertyGroup;
using Property = Telligent.Evolution.Extensibility.Configuration.Version1.Property;

namespace FourRoads.TelligentCommunity.InlineContent.ScriptedContentFragments
{
    public class InlineContentContext
    {
        public InlineContentContext(string inlineContentName, string defaultContent, string defaultAnonymousContent, string currentCOntent, string currentAnonymousContent, bool canEdit)
        {
            InlineContentName = inlineContentName;
            DefaultContent = defaultContent;
            DefaultAnonymousContent = defaultAnonymousContent;
            CurrentContent = currentCOntent;
            CurrentAnonymousContent = currentAnonymousContent;
            CanEdit = canEdit;
        }

        public string InlineContentName { get; private set; }
        public string DefaultContent { get; private set; }
        public string DefaultAnonymousContent { get; private set; }
        public string CurrentContent { get; private set; }
        public string CurrentAnonymousContent { get; private set; }
        public bool CanEdit { get; private set; }
        public string ContentTypeId => InlineContentPart.InlineContentContentTypeId.ToString();

        public void UpdateContent(string inlineCOntentName, string content, string anonymousContent)
        {
            Injector.Get<InlineContentLogic>().UpdateInlineContent(inlineCOntentName, content, anonymousContent);
        }
    }

    public class InlineContentPanel : IScriptablePlugin
    {
        public static Guid _scriptedFragmentGuid = new Guid("a8ec6c5fe7c045d3848d25930503e153");
        private static Guid _instanceIdentifier = new Guid("17af3cb782e44f8e8903ba64404cd913");
        private IScriptedContentFragmentController _controller;

        public void Initialize()
        {
           
        }

        public string Name => "Inline Content Panel";
        public string Description => "Inline content rendering";
        public Guid ScriptedContentFragmentFactoryDefaultIdentifier => _scriptedFragmentGuid;
        public void Register(IScriptedContentFragmentController controller)
        {
            var options = new ScriptedContentFragmentOptions(_instanceIdentifier)
            {
                CanBeThemeVersioned = false,
                CanHaveHeader = false,
                CanHaveWrapperCss = false,
                CanReadPluginConfiguration = false,
                IsEditable = true,
                AdjustCallbackContext = (c) =>
                {
                    c.Remove("AnonymousContent");
                    c.Remove("DefaultContent");

                    return c;
                }

            };
            options.Extensions.Add(new PanelContext());

            controller.Register(options);

            _controller = controller;
        }

        public string Render(NameValueCollection nv)
        {
            return _controller.RenderContent(_instanceIdentifier, nv);
        }

        public class PanelContext : IContextualScriptedContentFragmentExtension
        {
            public string ExtensionName
            {
                get { return "context"; }
            }

            public object GetExtension(NameValueCollection context)
            {
                if (!string.IsNullOrWhiteSpace(context["InlineContentName"]))
                    return new InlineContentContext(context["InlineContentName"], context["DefaultContent"], context["AnonymousContent"], context["CurrentContent"], context["CurrentAnonymousContent"], bool.Parse(context["CanEdit"]));

                return null;
            }
        }
    }

    public class InlineContentPart : Telligent.Evolution.Extensibility.UI.Version2.ConfigurableContentFragmentBase, ITranslatablePlugin, IPluginGroup, IFileEmbeddableContentType//, IScriptedContentFragmentExtension
    {
        private ITranslatablePluginController _translatablePluginController;
        private IFileEmbeddableContentTypeController _ftController;

        public override bool RenderContent(TextWriter writer, ContentFragmentRenderOptions options)
        {
            if (PluginManager.IsEnabled(this))
            {
                string inlineContentName = string.Empty;

                if (DynamicName)
                {
                    inlineContentName = Apis.Get<IUrl>().CurrentContext.PageName + "_";
                }

                if (ContextualMode == ContextMode.GroupContext || ContextualMode == ContextMode.Context)
                {
                    //This allows the control to have several instances on a single page
                    if (!string.IsNullOrWhiteSpace(InlineContentName))
                        inlineContentName += InlineContentName + "_";

                    ContextualItem(
                        a => { inlineContentName += a.ApplicationId.ToString(); },
                        c =>
                        {
                            if (c != null && c.ContainerId != Apis.Get<IGroups>().Root.ContainerId)
                            {
                                inlineContentName += c.ContainerId.ToString();
                            }
                            else
                            {
                                inlineContentName += "SiteRoot";
                            }
                        },
                        ta => { inlineContentName += GetHashString(string.Join("", ta.OrderBy(s => s))); });
                }
                else
                {
                    inlineContentName += InlineContentName;
                }

                var logic = Injector.Get<InlineContentLogic>();
                var inlineCOnetntObject = logic.GetInlineContent(inlineContentName);

                NameValueCollection nv = new NameValueCollection()
                {
                    {"InlineContentName", inlineContentName},
                    {"DefaultContent", GetContentText()},
                    {"AnonymousContent", GetAnonymousContentText()},
                    {"CurrentContent", inlineCOnetntObject?.Content ?? string.Empty},
                    {"CurrentAnonymousContent", inlineCOnetntObject?.AnonymousContent ?? string.Empty},
                    {"CanEdit", logic.CanEdit.ToString()}
                };

                writer.Write(PluginManager.Get<InlineContentPanel>().FirstOrDefault().Render(nv));
            }

            return true;
        }

        private bool DynamicName
        {
            get
            {
                return GetBoolValue("dynamicName", false);
            }
        }

        private ContextMode ContextualMode
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

        public string GetContentText()
        {
            return GetHtmlValue("default_content", "");
        }

        public string GetAnonymousContentText()
        {
            return GetHtmlValue("anonymous_content", "");
        }

        public override string FragmentName
        {
            get { return "4 Roads - Inline Content"; }
        }

        public override string FragmentDescription
        {
            get { return "Provides a simpler content editor experience that is decoupled from the main theme editing"; }
        }

        private enum ContextMode
        {
            Name = 1,
            Context = 0,
            GroupContext = 2
        }

        public override Telligent.Evolution.Extensibility.Configuration.Version1.PropertyGroup[] GetPropertyGroups()
        {
            if (HttpContext.Current == null || HttpContext.Current.Request.CurrentExecutionFilePath.EndsWith(".ashx"))
                return new PropertyGroup[0];

            PropertyGroup group = new PropertyGroup() {Id = "GeneralSettings", LabelText = "General" };

            Property headerTitle = new Property()
            {
                Id = "headerTitle",
                LabelText =  "Widget Title", DataType = "string", DefaultValue = "${resource:default_property_title}"
            };

            headerTitle.Options.Add("ControlType", "TokenString");

            group.Properties.Add(headerTitle);

            PropertyGroup defaultContent = new PropertyGroup() {Id = "DefaultContent", LabelText = "Default" };

            Property property = new Property()
            {
                Id = "default_content",
                LabelText = "Content",
                DataType = "Html"
            };

            property.Options.Add("rows", "40");
            property.Options.Add("sanitize", "false");
            property.Options.Add("enableRichEditing", "true");
            property.Options.Add("ContentTypeId", InlineContentPart.InlineContentContentTypeId.ToString());

            defaultContent.Properties.Add(property);

            PropertyGroup anoymousContent = new PropertyGroup() {Id = "AnonymousContent", LabelText ="Anonymous" };

            property = new Property()
            {
                Id = "anonymous_content",
                LabelText = "Content",
                DataType = "Html"
            };

            property.Options.Add("rows", "40");
            property.Options.Add("sanitize", "false");
            property.Options.Add("enableRichEditing", "true");
            property.Options.Add("ContentTypeId", InlineContentPart.InlineContentContentTypeId.ToString());

            anoymousContent.Properties.Add(property);


            property = new Property() {Id = "inlinecontenttype", LabelText = "Inline Type", DefaultValue = "Contextual", DataType = "string", DescriptionText = "Select the type of context used to categorize the configuration data" };

            property.SelectableValues.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.PropertyValue(){Value = "Contextual", LabelText ="Contextual" });
            property.SelectableValues.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.PropertyValue() { Value = "GroupContextual", LabelText ="Group Contextual" });
            property.SelectableValues.Add(new Telligent.Evolution.Extensibility.Configuration.Version1.PropertyValue() { Value = "ByName", LabelText ="By Name" });

            group.Properties.Add(property);

            property = new Property() {Id = "dynamicname", LabelText = "Use Dynamic Name", DataType = "bool", DefaultValue = bool.FalseString , DescriptionText = "Use a dynamic content name based on the current page context" };

            group.Properties.Add(property);

            property = new Property(){Id = "inlinecontentname", LabelText = "Inline Content Name" , DataType = "string"};
            group.Properties.Add(property);

            return new[] { group , defaultContent, anoymousContent };
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
                Translation[] defaultTranslation = new[] { new Translation("en-us") };

                return defaultTranslation;
            }
        }

        protected ITranslatablePluginController TranslatablePluginController
        {
            get
            {

                return _translatablePluginController;
            }
        }


        public IEnumerable<Type> Plugins
        {
            get
            {
                return new[]
                {
                    typeof (InlineContentStore),
                    typeof (DependencyInjectionPlugin),
                    typeof (PermissionRegistrar),
                    typeof(InlinePanelInstaller),
                    typeof(InlineContentPanel)
                };
            }
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
            var fs = CentralizedFileStorage.GetFileStore(InlineContentLogic.FILESTORE_KEY);

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
            get { return new Guid[0]; }
        }

        public IContent Get(Guid contentId)
        {
            return null;
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

        protected void ContextualItem(Action<IApplication> applicationUse, Action<IContainer> containerUse, Action<string[]> tagsUse)
        {
            IApplication currentApplication = null;
            IContainer currentContainer = null;
            string[] tags = null;

            foreach (var contextItem in Apis.Get<IUrl>().CurrentContext.ContextItems.GetAllContextItems())
            {
                var app = Apis.Get<IApplicationTypes>().List().FirstOrDefault(a => a.Id.Value == contextItem.ApplicationTypeId);

                if (app != null && contextItem.ApplicationId.HasValue)
                {
                    IApplication application = Apis.Get<IApplications>().Get(contextItem.ApplicationId.Value, contextItem.ApplicationTypeId.Value);

                    if (application != null)
                    {
                        if (application.Container.ContainerId != application.ApplicationId)
                            currentApplication = application;
                    }
                }

                var container = Apis.Get<IContainerTypes>().List().FirstOrDefault(a => a.Id.Value == contextItem.ContainerTypeId);

                if (container != null && contextItem.ContainerId.HasValue && contextItem.ContainerTypeId == Apis.Get<IGroups>().ContainerTypeId)
                {
                    currentContainer = Apis.Get<IContainers>().Get(contextItem.ContainerId.Value , contextItem.ContainerTypeId.Value);
                }

                if (contextItem.TypeName == "Tags")
                {
                    tags = contextItem.Id.Split(new[] { '/' });
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

    }

 
}