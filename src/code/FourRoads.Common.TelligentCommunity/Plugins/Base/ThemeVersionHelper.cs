using System;
using System.IO;
using System.Text;
using System.Web;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public static class ThemeVersionHelper
    {
        static object _threadLock = new object();


        public static void LocalVersionCheck(string fileName,Version currentVersion, Action<Version> installFunction)
        {
            lock (_threadLock)
            {
                try
                {
                    //Check to see if this instance has installed widgets at the right version
                    if (HttpContext.Current != null)
                    {
                        string widgetVersionFileName = HttpContext.Current.Server.MapPath("~/App_Data");

                        Directory.CreateDirectory(widgetVersionFileName);

                        widgetVersionFileName = Path.Combine(widgetVersionFileName, fileName);

                        using (var versionFile = System.IO.File.Open(widgetVersionFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            using (TextReader tr = new StreamReader(versionFile))
                            {
                                string version = tr.ReadToEnd();

                                if (string.IsNullOrWhiteSpace(version))
                                {
                                    version = "0.0.0.0";
                                }

                                installFunction(new Version(version));

                                versionFile.Seek(0, SeekOrigin.Begin);

                                byte[] bytes = Encoding.UTF8.GetBytes(currentVersion.ToString());

                                versionFile.Write(bytes, 0, bytes.Length);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Apis.Get<IEventLog>().Write("Unable to store local widget version for plugins, aborting attempt:" + ex.ToString(), new EventLogEntryWriteOptions() {Category = "Theme", EventType = "Warning"});
                }
            }
        }
    }
}