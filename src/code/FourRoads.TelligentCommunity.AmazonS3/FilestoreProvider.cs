using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Configuration;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.AmazonS3
{
    public class FilestoreProvider :  ICentralizedFileStorageProvider, IEventEnabledCentralizedFileStorageProvider
    {
        private string _bucketName;
        private AmazonS3Client s3Client;
        private bool _acellerationEnabled;
        private const string PlaceHolderFilename = "__path__place__holder.cfs.s3";
        private static readonly object InitLock = new Object();
        private static HashSet<string> _fileSet;

        public void Initialize(string fileStoreKey, XmlNode node)
        {
            lock (InitLock)
            {
                if (s3Client != null)
                    return;

                _fileSet = new HashSet<string>();

                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                if (node.Attributes == null)
                {
                    throw new ArgumentNullException(nameof(node), "No attributes defined on xml configuration, please specify at least awsSecretAccessKey, awsAccessKeyId, bucket");
                }

                if (string.IsNullOrWhiteSpace(fileStoreKey))
                {
                    throw new ArgumentNullException(nameof(fileStoreKey));
                }

                FileStoreKey = fileStoreKey;

                _bucketName = node.Attributes["bucket"].Value;

                var credentials = new BasicAWSCredentials(node.Attributes["awsSecretAccessKey"].Value, node.Attributes["awsAccessKeyId"].Value);

                string regionString = node.Attributes["region"]?.Value;

                var configuration = new AmazonS3Config() {SignatureMethod = SigningAlgorithm.HmacSHA256};

                if (!string.IsNullOrWhiteSpace(regionString))
                {
                    var region = RegionEndpoint.GetBySystemName(regionString);

                    configuration.RegionEndpoint = region;
                }

                s3Client = new AmazonS3Client(credentials, configuration);

                bool enableAcceleration;

                bool.TryParse(node.Attributes["enableAcceleration"]?.Value, out enableAcceleration);

                if (enableAcceleration)
                {
                    EnableAccelerationAsync().Wait(2000);

                    if (_acellerationEnabled)
                    {
                        configuration.UseAccelerateEndpoint = _acellerationEnabled;
                        //replace the client with accellerated version
                        s3Client = new AmazonS3Client(credentials, configuration);
                    }
                }
            }
        }

       private async Task EnableAccelerationAsync()
        {
            try
            {
                if (! _acellerationEnabled)
                {
                    await TestAcceleration();

                    var putRequest = new PutBucketAccelerateConfigurationRequest
                    {
                        BucketName = _bucketName,
                        AccelerateConfiguration = new AccelerateConfiguration
                        {
                            Status = BucketAccelerateStatus.Enabled
                        }
                    };
                    await s3Client.PutBucketAccelerateConfigurationAsync(putRequest);

                    await TestAcceleration();
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    "Error occurred. Message:'{0}' when setting transfer acceleration",
                    ex).Log();
            }
        }

        private async Task TestAcceleration()
        {
            var getRequest = new GetBucketAccelerateConfigurationRequest
            {
                BucketName = _bucketName
            };
            var response = await s3Client.GetBucketAccelerateConfigurationAsync(getRequest);

            _acellerationEnabled = response.Status == BucketAccelerateStatus.Enabled;
        }

        public ICentralizedFile GetFile(string path, string fileName)
        {
            System.Diagnostics.Debug.WriteLine($"GetFile:{path}\\{fileName}");

            FileStorageFile file = new FileStorageFile(FileStoreKey, path, fileName);

            string hashKey = $"{FileStoreKey}-{path}-{fileName}";

            if (!_fileSet.Contains(hashKey))
            {
                //Since we don't know if it exists let's do a check
                if (!DoesFileExist(path , fileName))
                {
                    return null;
                }

                _fileSet.Add(hashKey);
            }

            return file;
        }

        private bool DoesFileExist(string path, string fileName)
        {
            var request = new ListObjectsRequest();

            request.BucketName = _bucketName;
            request.Prefix = MakeKey(path, fileName);
            request.MaxKeys = 1;

            ListObjectsResponse response = s3Client.ListObjects(request);

            if (response.S3Objects.Count == 0)
                return false;

            if (String.Compare(response.S3Objects[0].Key , request.Prefix, StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            return true;
        }

        public int GetContentLength(string path, string fileName)
        {
            string fileKey = MakeKey(path, fileName);

            var metaDataAsync = s3Client.GetObjectMetadataAsync(_bucketName, fileKey);

            var metaData = metaDataAsync.Result;

            return Convert.ToInt32(metaData.ContentLength);
        }

        public IEnumerable<ICentralizedFile> GetFiles(PathSearchOption searchOption)
        {
            return GetFiles("", searchOption);
        }

        public IEnumerable<ICentralizedFile> GetFiles(string path, PathSearchOption searchOption)
        {
            return GetFiles(path , searchOption , false);
        }

        public IEnumerable<ICentralizedFile> GetFiles(string path, PathSearchOption searchOption, bool includePlaceholders)
        {
            List<FileStorageFile> files = new List<FileStorageFile>();

            ListObjectsRequest request = new ListObjectsRequest();
            request.BucketName = _bucketName;
            request.Prefix = MakeKey(path, "");
            request.MaxKeys = int.MaxValue;

            if (searchOption == PathSearchOption.TopLevelPathOnly)
            {
                request.Delimiter = "/";
            }

            ListObjectsResponse response = s3Client.ListObjects(request);

            foreach (S3Object entry in response.S3Objects)
            {
                string fileName = GetFileName(entry.Key);

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    string filePath = GetPath(entry.Key);

                    if (includePlaceholders || string.Compare(fileName, PlaceHolderFilename, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        FileStorageFile result = new FileStorageFile(FileStoreKey, filePath, fileName, Convert.ToInt32(entry.Size));

                        files.Add(result);

                        string hashKey = $"{FileStoreKey}-{path}-{fileName}";

                        if (!_fileSet.Contains(hashKey))
                        {
                            _fileSet.Add(hashKey);
                        }
                    }

                }
            }

            return files;
        }

        public IEnumerable<string> GetPaths()
        {
            return GetPaths("/");
        }

        public IEnumerable<string> GetPaths(string path)
        {
            HashSet<string> paths = new HashSet<string>();

            foreach (var file in GetFiles(path, PathSearchOption.AllPaths))
            {
                if (!paths.Contains(file.Path))
                {
                    paths.Add(file.Path);
                }
            }

            return paths;
        }

        public void AddPath(string path)
        {
            AddUpdateFile(path, PlaceHolderFilename, new MemoryStream(Encoding.UTF8.GetBytes("Path Placeholder")));
        }

        public void Delete(string path, string fileName)
        {
            if (!CentralizedFileStorage.IsValid(FileStoreKey, path, fileName))
                throw CreateFilePathInvalidException(path, fileName);

            string key = MakeKey(path, fileName);

            EventExecutor?.OnBeforeDelete(FileStoreKey, path, fileName);

            s3Client.DeleteObject(_bucketName, key);

            if (_fileSet.Contains(key))
            {
                _fileSet.Remove(key);
            }

            EventExecutor?.OnAfterDelete(FileStoreKey, path, fileName);
        }

        public void Delete()
        {
            EventExecutor?.OnBeforeDelete(FileStoreKey, null, null);

            foreach (ICentralizedFile file in GetFiles("", PathSearchOption.AllPaths, true))
            {
                Delete(file.Path, file.FileName);
            }

            EventExecutor?.OnAfterDelete(FileStoreKey, null, null);
        }

        public void Delete(string path)
        {
            if (!CentralizedFileStorage.IsValidPath(path))
            {
                return;
            }

            EventExecutor?.OnBeforeDelete(FileStoreKey, path, null);

            foreach (ICentralizedFile file in GetFiles(path, PathSearchOption.AllPaths, true))
            {
                Delete(file.Path, file.FileName);
            }

            EventExecutor?.OnAfterDelete(FileStoreKey, path, null);
        }

        public ICentralizedFile AddUpdateFile(string path, string fileName, Stream contentStream)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (!CentralizedFileStorage.IsValid(FileStoreKey, path, fileName))
            {
                throw CreateFilePathInvalidException(path, fileName);
            }

            ICentralizedFile currentFile = GetFile(path, fileName);

            if (currentFile == null)
            {
                EventExecutor?.OnBeforeCreate(FileStoreKey, path, fileName);
            }
            else
            { 
                EventExecutor?.OnBeforeUpdate(currentFile);
            }

            string randomTempFileName = Path.GetTempFileName();
            //Copy the uploaded file to  a local filesystem so we can get it size etc
            FileInfo fileInfo = new FileInfo(randomTempFileName);

            fileInfo.Attributes = FileAttributes.Temporary;

            using (var tempFileStream = fileInfo.Open(FileMode.Truncate))
            {
                contentStream.CopyTo(tempFileStream);
            }

            s3Client.PutObject(new PutObjectRequest() {
                BucketName = _bucketName,
                Key = MakeKey(path, fileName),
                InputStream = new FileStream(randomTempFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose),
                ContentType = MimeTypeConfiguration.GetMimeType(fileName)});

            var resultFile = GetFile(path, fileName);

            if (currentFile == null)
            {
                EventExecutor?.OnAfterCreate(resultFile);
            }
            else
            {
                EventExecutor?.OnAfterUpdate(resultFile);
            }

            return resultFile;
        }

        public ICentralizedFile AddFile(string path, string fileName, Stream contentStream, bool ensureUniqueFileName)
        {
            if (ensureUniqueFileName)
                return AddUpdateFile(path, CentralizedFileStorage.GetUniqueFileName(this, path, fileName), contentStream);

            return AddUpdateFile(path, fileName, contentStream);
        }

        public string FileStoreKey { get; private set; }
 
        public ICentralizedFileEventExecutor EventExecutor { get; set; }

        public Stream GetContentStream(string path, string fileName)
        {
            System.Diagnostics.Debug.WriteLine($"GetContentStream:{path}\\{fileName}");

            string fileKey = MakeKey(path, fileName);

            if (DoesFileExist(path, fileName))
            {
                using (var responseObjectStranStream = s3Client.GetObject(new GetObjectRequest() {BucketName = _bucketName, Key = fileKey}).ResponseStream)
                {
                    //Put all content into local filecache avoids memory issues
                    string randomTempFileName = Path.GetTempFileName();

                    FileInfo fileInfo = new FileInfo(randomTempFileName);

                    fileInfo.Attributes = FileAttributes.Temporary;

                    using (var tempFileStream = fileInfo.Open(FileMode.Truncate))
                    {
                        responseObjectStranStream.CopyTo(tempFileStream);
                    }

                    return new FileStream(randomTempFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
                }
            }

            return null;
        }

        public string GetDownloadUrl(string path, string fileName)
        {
            System.Diagnostics.Debug.WriteLine($"GetDownloadUrl:{path}\\{fileName}");
            if (DoesFileExist(path, fileName))
            {
                string fileKey = MakeKey(path, fileName);

                return s3Client.GetPreSignedURL(new GetPreSignedUrlRequest() { BucketName = _bucketName, Key = fileKey, Expires = DateTime.Now.AddHours(1)});
            }

            return null;
        }

        private ApplicationException CreateFilePathInvalidException(string path, string fileName)
        {
            return new ApplicationException($"The provided path and/or file name is invalid. File store key {FileStoreKey}, path {path}, file name {fileName}");
        }

        private string GetPath(string key, bool includesFileName)
        {
            string path = key.Substring(MakeKey(string.Empty, string.Empty).Length);

            if (includesFileName)
            {
                path = !path.Contains("/") ? string.Empty : path.Substring(0, path.LastIndexOf('/'));
            }
            else if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }

            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            return path.Replace('/', CentralizedFileStorage.DirectorySeparator);
        }

        private string GetPath(string key)
        {
            return GetPath(key, true);
        }

        public string GetFileName(string key)
        {
            return key.Substring(key.LastIndexOf('/') + 1);
        }

        private string MakeKey(string path, string fileName)
        {
            var stringBuilder = new StringBuilder(path.Length + fileName.Length + 2);

            stringBuilder.Append(FileStoreKey);

            if (!string.IsNullOrEmpty(path))
            {
                stringBuilder.Append("/");
                stringBuilder.Append(path.Replace(CentralizedFileStorage.DirectorySeparator, '/'));
            }

            stringBuilder.Append("/");

            if (!string.IsNullOrEmpty(fileName))
            {
                stringBuilder.Append(fileName);
            }

            return stringBuilder.ToString();
        }

    }
}
