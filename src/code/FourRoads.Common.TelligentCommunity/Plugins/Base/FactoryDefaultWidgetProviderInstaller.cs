using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public abstract class FactoryDefaultWidgetProviderInstaller :  IScriptedContentFragmentFactoryDefaultProvider, IInstallablePlugin
    {
        public abstract Guid ScriptedContentFragmentFactoryDefaultIdentifier { get; }
        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }

        #region IPlugin Members

        public string Name
        {
            get { return ProjectName + " - Factory Default Widget Provider"; }
        }

        public string Description
        {
            get { return "Defines the default widget set for " + ProjectName+ "."; }
        }

        public void Initialize()
        {
        }

        #endregion

        #region IInstallablePlugin Members

        public virtual void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version)
            {
                Uninstall();

                string basePath = BaseResourcePath + "Widgets.";

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
                                FactoryDefaultScriptedContentFragmentProviderFiles
                                    .AddUpdateSupplementaryFile(this, instanceId, supplementName, stream);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        new CSException(CSExceptionType.UnknownError,string.Format("Couldn't load widget from '{0}' embedded resource.", resourceName), exception).Log();
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
                    new CSException(CSExceptionType.UnknownError, string.Format("Couldn't delete factory default widgets from provider ID: '{0}'.", ScriptedContentFragmentFactoryDefaultIdentifier), exception).Log();
                }
            }
        }

        public Version Version
        {
            get { return GetType().Assembly.GetName().Version; }
        }

        #endregion
    }
}