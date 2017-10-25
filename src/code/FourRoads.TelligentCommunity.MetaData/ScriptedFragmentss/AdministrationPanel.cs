using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.Linq;
using FourRoads.Common.TelligentCommunity;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using Telligent.Evolution.Extensibility.Administration.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.MetaData.ScriptedFragmentss
{
    public class AdministrationPanel : IScriptablePlugin, IApplicationPlugin, IInstallablePlugin,IApplicationPanel, IContainerPanel
    {
        //private static Guid _panelGuid = new Guid("{794F9086-EDE9-4313-B90B-61198ADB59AA}");
        private static Guid _scriptedFragmentGuid = new Guid("{4405A321-03D9-4601-81F3-81B84F6EFB01}");
        private static Guid _instanceIdentifier = new Guid("411f7656968348e1906a695a01b5f56c");
        private IScriptedContentFragmentController _controller;

        public void Initialize()
        {

        }

        public string Name
        {
            get { return "MetaData Administration"; }
        }

        public string Description
        {
            get { return "Builds a administration interface for the metadata plugin"; }
        }

        public Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return _scriptedFragmentGuid; }
        }

        public void Register(IScriptedContentFragmentController controller)
        {
            var options = new ScriptedContentFragmentOptions(_instanceIdentifier)
            {
                CanBeThemeVersioned = false,
                CanHaveHeader = false,
                CanHaveWrapperCss = false,
                CanReadPluginConfiguration = false,
                IsEditable = true,
                
            };
            options.Extensions.Add(new PanelContext());

            controller.Register(options);

            _controller = controller;
        }

            public Guid PanelId
        {
            get { return _instanceIdentifier; }
        }

        public string CssClass
        {
            get { return _controller.GetMetadata(_instanceIdentifier).CssClass; } 
        }

        public int? DisplayOrder
        {
            get { return 0; }
        }

        public bool IsCacheable
        {
            get { return _controller.GetMetadata(_instanceIdentifier).IsCacheable; }
        }

        public bool VaryCacheByUser
        {
            get { return _controller.GetMetadata(_instanceIdentifier).VaryCacheByUser; }
        }

        public string GetPanelName(Guid type, Guid id)
        {
            return "Meta Data Configuration";
        }

        public string GetPanelDescription(Guid type, Guid id)
        {
            return "Allows you to configure metadata ";
        }

        public bool HasAccess(int userId, Guid type, Guid id)
        {
            return Injector.Get<IMetaDataLogic>().CanEdit;
        }

        public string GetViewHtml(Guid type, Guid id)
        {
            return _controller.RenderContent(_instanceIdentifier, new NameValueCollection() { { "TypeId", type.ToString()} ,{ "Id" , id.ToString() }});
        }

        public Guid[] ContainerTypes
        {
            get { return new [] { Apis.Get<IGroups>().ContainerTypeId }; }
        }

        public Guid[] ApplicationTypes
        {
            get { return Apis.Get<IApplicationTypes>().List().Where(c => Telligent.Evolution.Extensibility.Version1.PluginManager.Get<IWebContextualApplicationType>().Any(a => a.ApplicationTypeId == c.Id.GetValueOrDefault(Guid.Empty))).Select(c => c.Id.Value).ToArray(); }
        }

        protected  string BaseResourcePath {
            get { return "FourRoads.TelligentCommunity.MetaData.Resources."; }
        }
        protected EmbeddedResourcesBase EmbeddedResources {
            get { return new EmbeddedResources();}
        }

        public class PanelContext : IContextualScriptedContentFragmentExtension
        {
            public string ExtensionName
            {
                get { return "MetaData Context"; }
            }

            public object GetExtension(NameValueCollection context)
            {
                return null;
            }
        }

        public virtual void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version)
            {
                Uninstall();

                string basePath = BaseResourcePath + "ScriptablePlugin.";

                EmbeddedResources.EnumerateReosurces(basePath, ".xml", resourceName =>
                {
                    try
                    {
                        string widgetName = resourceName.Substring(basePath.Length);

                        using (var stream = EmbeddedResources.GetStream(resourceName))
                        {
                            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(this, widgetName, stream);
                        }

                        // Get widget identifier
                        XDocument xdoc = XDocument.Parse(EmbeddedResources.GetString(resourceName));
                        XElement root = xdoc.Root;

                        if (root == null)
                            return;

                        XElement element = root.Element("scriptedContentFragment");

                        if (element == null)
                            return;

                        XAttribute attribute = element.Attribute("instanceIdentifier");

                        if (attribute == null)
                            return;

                        Guid instanceId = new Guid(attribute.Value);

                        string widgetBasePath = string.Concat(basePath, char.IsNumber(attribute.Value[0]) ? "_" : "", instanceId.ToString("N"), ".");
                        IEnumerable<string> supplementaryResources =
                            GetType().Assembly.GetManifestResourceNames().Where(r => r.StartsWith(widgetBasePath)).ToArray();

                        if (!supplementaryResources.Any())
                            return;

                        foreach (string supplementPath in supplementaryResources)
                        {
                            string supplementName = supplementPath.Substring(widgetBasePath.Length);

                            using (var stream = EmbeddedResources.GetStream(supplementPath))
                            {
                                FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(this, instanceId, supplementName, stream);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        new TCException( string.Format("Couldn't load widget from '{0}' embedded resource.", resourceName), exception).Log();
                    }
                });
            }
        }

        public virtual void Uninstall()
        {
            if (!Diagnostics.IsDebug(GetType().Assembly))
            {
                //Only in release do we want to uninstall widgets, when in development we don't want this to happen
                try
                {
                    FactoryDefaultScriptedContentFragmentProviderFiles.DeleteAllFiles(this);
                }
                catch (Exception exception)
                {
                    new TCException( string.Format("Couldn't delete factory default widgets from provider ID: '{0}'.", ScriptedContentFragmentFactoryDefaultIdentifier), exception).Log();
                }
            }
        }

        public Version Version
        {
            get { return GetType().Assembly.GetName().Version; }
        }
    }
}
