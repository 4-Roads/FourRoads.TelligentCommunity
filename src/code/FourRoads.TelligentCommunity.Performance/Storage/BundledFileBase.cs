using System.IO;
using System.Web;
using System.Web.Hosting;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.Performance.Storage
{
    public class BundledFileBase
    {
        private string _applicationPath;
        private FilestoreCache _fsCache = new FilestoreCache();

        protected string ResolveVirtual(string physicalPath)
        {
            return physicalPath.Substring(_applicationPath.Length).Replace('\\', '/').Insert(0, "~/");
        }

        public static string MakeAppRelative(string path)
        {
            return Globals.FullPath(path).ToLower().Replace(Globals.FullPath("~/").ToLower(), "~/");
        }

        public BundledFileBase()
        {
            _applicationPath = HostingEnvironment.MapPath("~/");
            IsValid = false;
        }


        protected string LocalPerformanceCachePath()
        {
            return _fsCache.CachePath;
        }

        public bool IsValid { get; protected set; }
        public string Type { get; protected set; }
        public string OrignalUri { get; protected set; }
        public string RelativeUri { get; protected set; }
        public string LocalPath { get; protected set; }
    }
}