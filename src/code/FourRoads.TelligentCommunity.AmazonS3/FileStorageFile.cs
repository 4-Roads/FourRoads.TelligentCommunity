using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Amazon.S3.Model;
using FourRoads.Common.Interfaces;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.AmazonS3
{
    [Serializable]
    public class FileStorageFile : ICentralizedFile, ICacheable, IDisposable
    {
        private int? _contentLength = null;
        private static string[] _tags = new string[0];
        private object _fileAccessLock = new object();

        #region Constructors

        public FileStorageFile()
        {
            CacheRefreshInterval = 30;
        }

        public FileStorageFile(string fileStoreKey, string path, string fileName, int? contentLength = null)
            : this()
        {
            _contentLength = contentLength;
            FileName = fileName;
            Path = path;
            FileStoreKey = fileStoreKey;
            _downloadExpires = DateTime.UtcNow;
        }

        #endregion

        public void Dispose()
        {

        }

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

        /// <summary>
        /// This object contains a MMF handle, this is the best comprimise between performance and memory
        /// </summary>
        private Byte[] _cachedData;

        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        public Stream OpenReadStream()
        {
            if (CentralizedFileStorage.GetFileStore(FileStoreKey) is FilestoreProvider fileStore)
            {
                lock (_fileAccessLock)
                {
                    if (_cachedData == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"OpenReadStream(Miss):{Path}\\{FileName}");

                        GetObjectResponse response = fileStore.GetContentStream(Path, FileName);

                        using (Stream source = response.ResponseStream)
                        {
                            _cachedData = ReadFully(source);
                            _contentLength = _cachedData.Length;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"OpenReadStream(Hit):{Path}\\{FileName}");
                    }

                    return new MemoryStream(_cachedData);
                }
            }

            return null;
        }

        private string _downloadUrl;

        public string GetDownloadUrl()
        {
            //This is not cached 
            if (CentralizedFileStorage.GetFileStore(FileStoreKey) is FilestoreProvider fileStore)
            {
                if (_downloadUrl == null || _downloadExpires < DateTime.UtcNow)
                {
                    _downloadExpires = DateTime.UtcNow.AddMinutes(fileStore.PreSignExpiresMins);
                    _downloadUrl = fileStore.GetDownloadUrl(Path, FileName);
                }

                return _downloadUrl;
            }

            return string.Empty;
        }
        #endregion

        private DateTime _downloadExpires;
        public string CacheID => FileStorageFileCache.CreateCacheId(FileStoreKey,Path,FileName);
        public int CacheRefreshInterval { get; set; }
        public string[] CacheTags => _tags;
        public CacheScopeOption CacheScope => CacheScopeOption.Local;
    }
}