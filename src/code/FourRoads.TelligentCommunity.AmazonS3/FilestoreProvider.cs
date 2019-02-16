using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FourRoads.Common.Interfaces;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using Telligent.Evolution.Configuration;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;


namespace FourRoads.TelligentCommunity.AmazonS3
{
    public class AmazonS3FilterRequest : IHttpRequestFilter
    {
        public void Initialize()
        {

        }

        public string Name => "Amazon S3 Patch for 301";
        public string Description => "Amazon S3 Patch for 301, Telligent returns a 301 for CFS filestores where the path is redirected, the problem is that with extenal providers that use signed access this means the signed access must match the redirect period.  Also an expiring 301 is bad parctice, 301 are forever.";
        public void FilterRequest(IHttpRequest request)
        {
            if (request.PageContext.UrlName == "cfs-file")
            {
                if (request.HttpContext.Response.StatusCode == 301)
                {
                    if (request.HttpContext.Response.RedirectLocation.Contains("amazonaws.com"))
                    {
                        request.HttpContext.Response.StatusCode = 302;
                    }
                }
            }
        }
    }

    public class FilestoreProvider :  IEventEnabledCentralizedFileStorageProvider
    {
        private string _bucketName;
        private AmazonS3Client s3Client;
        private bool _acellerationEnabled;
        private const string PlaceHolderFilename = "__path__place__holder.cfs.s3";
        private FileStorageFileCache _fileSet;
        private int _cacheTime;
        private Dictionary<string, DateTime> _knownFiles = new Dictionary<string, DateTime>();

        public void Initialize(string fileStoreKey, XmlNode node)
        {
            if (s3Client != null)
                return;

            _fileSet = new FileStorageFileCache(Injector.Get<ICache>() , this);

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (string.IsNullOrWhiteSpace(fileStoreKey))
            {
                throw new ArgumentNullException(nameof(fileStoreKey));
            }

            FileStoreKey = fileStoreKey;

            _bucketName = node.Attributes?["bucket"].Value ?? throw new ArgumentNullException(nameof(node), "No attributes defined on xml configuration, please specify at least awsSecretAccessKey, awsAccessKeyId, bucket");

            var credentials = new BasicAWSCredentials(node.Attributes["awsAccessKeyId"].Value, node.Attributes["awsSecretAccessKey"].Value);

            string regionString = node.Attributes["region"]?.Value;

            var configuration = new AmazonS3Config {SignatureMethod = SigningAlgorithm.HmacSHA256};

            if (!string.IsNullOrWhiteSpace(regionString))
            {
                var region = RegionEndpoint.GetBySystemName(regionString);

                configuration.RegionEndpoint = region;
            }

            _cacheTime = Convert.ToInt32(node.Attributes["cacheTimeSeconds"]?.Value);

            s3Client = new AmazonS3Client(credentials, configuration);

            bool enableAcceleration;

            bool.TryParse(node.Attributes["enableAcceleration"]?.Value, out enableAcceleration);

            Debug.WriteLine($"Initialize Filestore: {fileStoreKey}");

            TestAcceleration();

            //if (enableAcceleration && !_acellerationEnabled)
            //{
            //    EnableAcceleration();
            //}

            if (_acellerationEnabled)
            {
                configuration.UseAccelerateEndpoint = _acellerationEnabled;
                //replace the client with accellerated version
                s3Client = new AmazonS3Client(credentials, configuration);
            }
        }

       private void EnableAcceleration()
        {
            try
            {
                if (! _acellerationEnabled)
                {
  
                    var putRequest = new PutBucketAccelerateConfigurationRequest
                    {
                        BucketName = _bucketName,
                        AccelerateConfiguration = new AccelerateConfiguration
                        {
                            Status = BucketAccelerateStatus.Enabled
                        }
                    };
                    s3Client.PutBucketAccelerateConfiguration(putRequest);

                    TestAcceleration();
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    "Error occurred. Message:'{0}' when setting transfer acceleration",
                    ex).Log();
            }
        }

        private void TestAcceleration()
        {
            try
            {
                var getRequest = new GetBucketAccelerateConfigurationRequest
                {
                    BucketName = _bucketName
                };

                var response = s3Client.GetBucketAccelerateConfiguration(getRequest);

                _acellerationEnabled = response.Status == BucketAccelerateStatus.Enabled;
            }
            catch (Exception ex)
            {
                new TCException("Failed to test for Amazon S3 Acceleration", ex).Log();
            }
        }

        public FileStorageFile GetInternal(string path, string fileName)
        {
            return new FileStorageFile(FileStoreKey, path, fileName) { CacheRefreshInterval = _cacheTime };
        }

        public ICentralizedFile GetFile(string path, string fileName)
        {
            Debug.WriteLine($"GetFile:{path}\\{fileName}");

            //Since we don't know if it exists let's do a check
            if (!DoesFileExist(path, fileName))
            {
                return null;
            }

            return _fileSet.Get(FileStorageFileCache.CreateCacheId(FileStoreKey, path, fileName));
        }

        private bool DoesFileExist(string path, string fileName)
        {
            string key = MakeKey(path, fileName);

            if (_knownFiles.ContainsKey(key))
            {
                if (DateTime.Now.Subtract(new TimeSpan(0, 0, 0, _cacheTime)) >= _knownFiles[key])
                {
                    _knownFiles.Remove(key);
                }
                else
                {
                    return true;
                }
            }

            var request = new ListObjectsRequest
            {
                BucketName = _bucketName,
                Prefix = MakeKey(path, fileName),
                MaxKeys = 1
            };

            ListObjectsResponse response = s3Client.ListObjects(request);

            if (response.S3Objects.Count == 0)
                return false;

            bool exists = string.Compare(response.S3Objects[0].Key , request.Prefix, StringComparison.OrdinalIgnoreCase) == 0;

            if (exists)
            {
                if (_knownFiles.ContainsKey(key))
                {
                    _knownFiles[response.S3Objects[0].Key] = DateTime.Now;
                }
                else
                {
                    _knownFiles.Add(response.S3Objects[0].Key, DateTime.Now);
                }
            }

            return exists;
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
            Debug.WriteLine($"GetFiles:{path}\\{searchOption}");

            List <FileStorageFile> files = new List<FileStorageFile>();

            ListObjectsV2Request request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = MakeKey(path, ""),
                MaxKeys = int.MaxValue
            };

            if (searchOption == PathSearchOption.TopLevelPathOnly)
            {
                request.Delimiter = "/";
            }

            do
            {
                ListObjectsV2Response response = s3Client.ListObjectsV2(request);

                foreach (S3Object entry in response.S3Objects)
                {
                    string fileName = GetFileName(entry.Key);

                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        string filePath = GetPath(entry.Key);

                        if (includePlaceholders || string.Compare(fileName, PlaceHolderFilename, StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            string cacheKey = FileStorageFileCache.CreateCacheId(FileStoreKey, filePath, fileName);

                            FileStorageFile file = _fileSet.Get(cacheKey);

                            if (file != null)
                            {
                                files.Add(file);
                            }
                        }

                    }
                }

                if (response.IsTruncated)
                {
                    request.ContinuationToken = response.NextContinuationToken;
                }
                else
                {
                    request = null;
                }

            } while (request != null);

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

            var fileToDelete = (FileStorageFile) GetFile(path, fileName);

            if (fileToDelete != null)
                _fileSet.Remove(fileToDelete);

            EventExecutor?.OnAfterDelete(FileStoreKey, path, fileName);

            if (_knownFiles.ContainsKey(key))
            {
                _knownFiles.Remove(key);
            }
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

            var files = GetFiles(path, PathSearchOption.AllPaths, true);

            if (files.Any())
            {
                EventExecutor?.OnBeforeDelete(FileStoreKey, path, null);

                foreach (ICentralizedFile file in files)
                {
                    Delete(file.Path, file.FileName);
                }

                EventExecutor?.OnAfterDelete(FileStoreKey, path, null);
            }
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

            s3Client.PutObject(new PutObjectRequest
            {
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

        public GetObjectResponse GetContentStream(string path, string fileName)
        {
            string fileKey = MakeKey(path, fileName);

            if (DoesFileExist(path, fileName))
            {
                return s3Client.GetObject(new GetObjectRequest {BucketName = _bucketName, Key = fileKey});
            }

            return null;
        }

        public string GetDownloadUrl(string path, string fileName)
        {
            Debug.WriteLine($"GetDownloadUrl:{path}\\{fileName}");
            if (DoesFileExist(path, fileName))
            {
                string fileKey = MakeKey(path, fileName);

                return s3Client.GetPreSignedURL(new GetPreSignedUrlRequest { BucketName = _bucketName, Key = fileKey, Expires = DateTime.Now.AddMinutes(30)});
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
