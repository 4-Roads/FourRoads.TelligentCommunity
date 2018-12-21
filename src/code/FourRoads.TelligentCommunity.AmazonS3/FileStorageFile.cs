using System;
using System.IO;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.AmazonS3
{
    [Serializable]
    public class FileStorageFile : ICentralizedFile
    {
        private int? _contentLength = null;

        #region Constructors

        public FileStorageFile()
        {

        }

        public FileStorageFile(string fileStoreKey, string path, string fileName, int? contentLength = null)
        {
            _contentLength = contentLength;
            FileName = fileName;
            Path = path;
            FileStoreKey = fileStoreKey;
        }

        #endregion

        #region ICentralizedFile Members

        public int ContentLength
        {
            get
            {
                if (!_contentLength.HasValue)
                {
                    if (CentralizedFileStorage.GetFileStore(FileStoreKey) is FilestoreProvider fileStore)
                    {
                        _contentLength =  fileStore.GetContentLength(Path, FileName);
                    }
                    _contentLength = 0;
                }
                return _contentLength.Value;
            }
        }

        public string FileName { get; set; }

        public string Path { get; set; }

        public string FileStoreKey { get; set; }

        public Stream OpenReadStream()
        {
            if (CentralizedFileStorage.GetFileStore(FileStoreKey) is FilestoreProvider fileStore)
            {
                return fileStore.GetContentStream(Path, FileName);
            }

            return null;
        }

        public string GetDownloadUrl()
        {
            if (CentralizedFileStorage.GetFileStore(FileStoreKey) is FilestoreProvider fileStore)
            {
                return fileStore.GetDownloadUrl(Path, FileName);
            }

            return string.Empty;
        }
        #endregion
    }
}