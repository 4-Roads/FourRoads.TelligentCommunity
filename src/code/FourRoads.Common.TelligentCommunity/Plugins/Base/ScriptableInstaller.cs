using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public abstract class ScriptableInstaller : IInstallablePlugin
    {

        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }
        public virtual void Initialize()
        {

        }

        public string Name => ProjectName + " - Scriptable Plugin";

        public string Description => "Defines the scriptable plugin for " + ProjectName + ".";

        public void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version)
            {
                Uninstall();

                string basePath = BaseResourcePath + "ScriptedPanels.";

                EmbeddedResources.EnumerateReosurces(basePath, ".xml", resourceName =>
                {
                    try
                    {
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

                        XAttribute providerAttribute = element.Attribute("provider");

                        if (providerAttribute == null)
                            return;

                        Guid providerGuid = new Guid(providerAttribute.Value);

                        Guid instanceId = new Guid(attribute.Value);

                        IScriptablePlugin plugin = PluginManager.Get<IScriptablePlugin>().FirstOrDefault(p => p.ScriptedContentFragmentFactoryDefaultIdentifier == providerGuid);

                        string widgetBasePath = string.Concat(basePath, char.IsNumber(attribute.Value[0]) ? "_" : "", instanceId.ToString("N"), ".");

                        string widgetName = resourceName.Substring(basePath.Length);

                        using (var stream = EmbeddedResources.GetStream(resourceName))
                        {
                            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(plugin, widgetName, stream);
                        }

                        IEnumerable<string> supplementaryResources =GetType().Assembly.GetManifestResourceNames().Where(r => r.StartsWith(widgetBasePath)).ToArray();

                        if (!supplementaryResources.Any())
                            return;

                        foreach (string supplementPath in supplementaryResources)
                        {
                            string supplementName = supplementPath.Substring(widgetBasePath.Length);

                            using (var stream = EmbeddedResources.GetStream(supplementPath))
                            {
                                FactoryDefaultScriptedContentFragmentProviderFiles
                                    .AddUpdateSupplementaryFile(plugin, instanceId, supplementName, stream);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        new TCException($"Couldn't load widget from '{resourceName}' embedded resource.", exception).Log();
                    }
                });
            }
        }

        public void Uninstall()
        {
            if (!Diagnostics.IsDebug(GetType().Assembly))
            {
                //Only in release do we want to uninstall widgets, when in development we don't want this to happen
                try
                {
                    string basePath = BaseResourcePath + "ScriptedPanels.";

                    EmbeddedResources.EnumerateReosurces(
                        basePath,
                        ".xml",
                        resourceName =>
                        {
                                // Get widget identifier
                                XDocument xdoc = XDocument.Parse(EmbeddedResources.GetString(resourceName));
                                XElement root = xdoc.Root;

                                if (root == null)
                                    return;

                                XElement element = root.Element("scriptedContentFragment");

                                if (element == null)
                                    return;

                                XAttribute providerAttribute = element.Attribute("provider");

                                if (providerAttribute == null)
                                    return;

                                Guid providerGuid = new Guid(providerAttribute.Value);

                                IScriptablePlugin plugin = PluginManager.Get<IScriptablePlugin>().FirstOrDefault(p => p.ScriptedContentFragmentFactoryDefaultIdentifier == providerGuid);

                                FactoryDefaultScriptedContentFragmentProviderFiles.DeleteAllFiles(plugin);

                        });

                }
                catch (Exception exception)
                {
                    new TCException($"Couldn't delete factory default widgets.", exception).Log();
                }
            }
        }

        public Version Version => GetType().Assembly.GetName().Version;
    }
}