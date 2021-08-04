using System;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Installer
{

    public abstract class CfsFilestoreInstaller : IInstallablePlugin
    {
        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }

        #region IPlugin Members

        public string Name
        {
            get { return ProjectName + " - CFS Installer"; }
        }

        public string Description
        {
            get { return "Defines the CFS files to be installed for " + ProjectName + "."; }
        }

        public void Initialize()
        {
        }

        #endregion

        public void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version)
            {
                Uninstall();
                string basePath = BaseResourcePath + ".Filestore.";

                EmbeddedResources.EnumerateResources(basePath, "", resourceName =>
                {
                    string file;

                    var cfsStore = GetFileStorageProvider(basePath, resourceName, out file);

                    if (cfsStore != null)
                    {
                        cfsStore.AddUpdateFile("", file, EmbeddedResources.GetStream(resourceName));
                    }
                });
            }
        }

        protected ICentralizedFileStorageProvider GetFileStorageProvider(string basePath, string resourceName, out string fileName)
        {
            string cfsFolder = resourceName.Replace(basePath, "");

            //Assume all files are *.*
            int pos = cfsFolder.LastIndexOf('.');

            if (pos > 0)
            {
                int previousDot = cfsFolder.LastIndexOf('.', pos - 1);

                if (previousDot < 0)
                    previousDot = -1;

                string folder = cfsFolder.Substring(0, previousDot);

                fileName = cfsFolder.Substring(previousDot + 1);

                return  CentralizedFileStorage.GetFileStore(folder);
            }

            fileName = null;

            return null;
        }

        public void Uninstall()
        {
            if (!Diagnostics.IsDebug(GetType().Assembly))
            {
                string basePath = BaseResourcePath + ".Filestore.";

                EmbeddedResources.EnumerateResources(basePath, "", resourceName =>
                {
                    string file;

                    var cfsStore = GetFileStorageProvider(basePath, resourceName, out file);

                    if (cfsStore != null)
                    {
                        cfsStore.Delete("", file);
                    }
                });
            }
        }

 
        public Version Version { get { return GetType().Assembly.GetName().Version; } }

    }
}
