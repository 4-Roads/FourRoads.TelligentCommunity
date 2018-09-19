using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public static class ThemeVersionHelper
    {
        private static object _threadLock = new object();
        private static readonly Regex MakeSafeFileNameRegEx = new Regex(CentralizedFileStorage.ValidFileNameRegexPattern, RegexOptions.Compiled);

        public static void LocalVersionCheck(string name, Version currentVersion, Action<Version> installFunction)
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

                        widgetVersionFileName = Path.Combine(widgetVersionFileName, MakeSafeFileName(name) + ".txt");

                        string version = null;

                        if (File.Exists(widgetVersionFileName))
                        {
                            version = File.ReadAllText(widgetVersionFileName, Encoding.UTF8);

                            Apis.Get<IEventLog>().Write($"Version {version}", new EventLogEntryWriteOptions() {Category = "4 Roads"});
                        }

                        if (string.IsNullOrWhiteSpace(version))
                        {
                            version = "0.0.0.1";
                        }

                        Apis.Get<IEventLog>().Write($"calling installl {version}", new EventLogEntryWriteOptions() { Category = "4 Roads" });

                        installFunction(new Version(version));

                        File.WriteAllText(widgetVersionFileName, currentVersion.ToString(), Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Apis.Get<IEventLog>().Write("Unable to store local widget version for plugins, aborting attempt:" + ex.ToString(), new EventLogEntryWriteOptions() { Category = "Theme", EventType = "Warning" });
                }
            }
        }

        private static string MakeSafeFileName(string name)
        {
            string result = string.Empty;

            foreach (Match match in MakeSafeFileNameRegEx.Matches(name))
            {
                result += match.Value;
            }

            return result;
        }
    }
}