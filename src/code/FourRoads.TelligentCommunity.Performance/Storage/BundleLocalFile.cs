using System;
using System.IO;
using System.Web;
using System.Web.UI;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Performance.Interfaces;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.Performance.Storage
{
    public class BundleLocalFile : BundledFileBase, IBundledFile
    {
        public BundleLocalFile(string type , string uri)
        {
            try
            {
                OrignalUri = uri;

                if (uri.StartsWith("http://") || uri.StartsWith("https://"))
                {
                    uri = uri.Replace(PublicApi.Url.Absolute("~"), "~");
                }

                string realFilePath = uri;

                int queryStart = realFilePath.IndexOf('?');

                if (queryStart != -1)
                {
                    realFilePath = realFilePath.Substring(0, queryStart);
                }

                Type = type;

                string localFsPath = LocalPerformanceCachePath();

                //Make a copy of the file (only need one version as it's the same everywhere, but want local so it can be altered to fix any path issues)
                LocalPath = Path.Combine(localFsPath, Path.GetFileName(realFilePath));

                if (!File.Exists(LocalPath))
                {
                    File.Copy(HttpContext.Current.Server.MapPath(realFilePath), LocalPath);
                }

                RelativeUri = ResolveVirtual(LocalPath);
                IsValid = true;
            }
            catch (Exception ex)
            {
                new TCException( "Failed to build remote file lookup", ex).Log();
            }
        }
    }
}

/*
     protected string EnsureLocalCopyExists(string file)
        {
            string localFsPath = LocalPerformanceCachePath();

            string localFileName = Path.Combine(localFsPath, System.IO.Path.GetFileName(file));

            using (FileStream localFile = File.Open(localFileName, FileMode.Create, FileAccess.Write))
            {
                using (Stream readStream = File.Open(HttpContext.Current.Server.MapPath(file) , FileMode.Open , FileAccess.Read))
                {
                    Byte[] buffer = new Byte[readStream.Length];

                    readStream.Read(buffer, 0, (int)readStream.Length);

                    localFile.Write(buffer, 0, buffer.Length);
                }
            }

            return localFileName;
        }
*/