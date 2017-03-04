using System;
using System.IO;
using System.Web;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Performance.Interfaces;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.Performance.Storage
{
    public class BundledFileFactory
    {
        public IBundledFile GetBundleFile(string type, string url, ContentFragmentPageControl contentFragmentPage , string additionalId)
        {
            try
            {
                IBundledFile result;
                if (CentralizedFileStorage.IsCentralizedFileUrl(url))
                {
                    result = new BundleCfsFile(type, url, contentFragmentPage, additionalId);
                }
                else
                {
                    string localPath = url;

                    int queryStart = localPath.IndexOf('?');

                    if (queryStart != -1)
                    {
                        localPath = localPath.Substring(0, queryStart);
                    }

                    if (localPath.StartsWith("http://") || localPath.StartsWith("https://"))
                    {
                        localPath = localPath.Replace(PublicApi.Url.Absolute("~"), "~");
                    }

                    localPath = HttpContext.Current.Server.MapPath(localPath);

                    if (File.Exists(localPath))
                    {
                        result = new BundleLocalFile(type, url);
                    }
                    else
                    {
                        result = new BundleRemoteFile(type, url, contentFragmentPage, additionalId);
                    }
                }

                if (result.IsValid)
                    return result;
            }
            catch(Exception ex)
            {
                new TCException(string.Format("Unable to bundle file type:{0} url:{1} page:{2}", type, url, contentFragmentPage.ContentFragmentContainer.ContainerName), ex).Log();
            }

            return null;
        }
    }
}