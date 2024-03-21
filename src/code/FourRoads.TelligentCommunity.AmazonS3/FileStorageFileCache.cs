using FourRoads.Common;
using FourRoads.Common.Interfaces;

namespace FourRoads.TelligentCommunity.AmazonS3
{
    internal class FileStorageFileCache : SimpleCachedCollection<FileStorageFile>
    {
        // private FilestoreProvider _provider;

        public FileStorageFileCache(ICache cacheProvider , FilestoreProvider provider)
            : base(cacheProvider)
        {
            //Cache provider does not automatically get file
            GetDataSingle = id =>
            {
                string[] parts = id.Replace($"$$S3Cache-{provider.FileStoreKey}-", "").Split('|');

                return provider.GetInternal(parts[0], parts[1]);
            };
        }

        public static string CreateCacheId(string fileStoreKey, string path, string file)
        {
            return $"$$S3Cache-{fileStoreKey}-{path}|{file}";
        }
    }
}