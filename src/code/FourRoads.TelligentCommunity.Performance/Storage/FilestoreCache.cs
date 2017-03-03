using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.Performance.Storage
{
    public class FilestoreCache
    {
        protected static object _lock = new object();

        protected string Path {
            get
            {
                return HostingEnvironment.MapPath("~/filestorage/performancecache");
            }
        }

        public string CachePath
        {
            get
            {
                lock (_lock)
                {
                    string localFsPath = Path;

                    if (!string.IsNullOrWhiteSpace(localFsPath))
                    {
                        if (!Directory.Exists(localFsPath))
                        {
                            Directory.CreateDirectory(localFsPath);
                        }
                    }
                    return localFsPath;
                }
            }
        }

        public void ClearCache()
        {
            lock (_lock)
            {
                if (Directory.Exists(Path))
                {
                    try
                    {
                        Directory.Delete(Path, true);
                    }
                    catch (Exception ex)
                    {
                        new TCException( "Failed to clean up performance directory", ex).Log();
                    }
                }
            }
        }
    }
}
