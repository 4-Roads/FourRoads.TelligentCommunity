using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.CustomEditor.CentralizedFileStore
{
    public class CustomEditorFileStore: ICentralizedFileStore
    {
        private const string CustomEditorFilestore = "custom-editor-filestore";

        public static ICentralizedFileStorageProvider GetFileStoreProvider()
        {
            return CentralizedFileStorage.GetFileStore(CustomEditorFilestore);
        }

        public void Initialize()
        {
        }

        public string Name
        {
            get { return "Filestore"; }
        }

        public string Description
        {
            get { return "Handles the filestorage for inline content"; }
        }

        public string FileStoreKey
        {
            get { return CustomEditorFilestore; }
        }

    }
}
