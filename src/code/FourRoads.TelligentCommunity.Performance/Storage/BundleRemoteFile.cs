using System;
using System.IO;
using System.Net;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Performance.Interfaces;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;

namespace FourRoads.TelligentCommunity.Performance.Storage
{
    public class BundleRemoteFile : BundledFileBase, IBundledFile
    {
        public BundleRemoteFile(string type, string uri, ContentFragmentPageControl contentFragmentPage, string additionalId)
        {
            IsValid = false;

            try
            {
                WebRequest request = WebRequest.Create(Globals.FullPath(uri));

                string localFsPath = LocalPerformanceCachePath();

                LocalPath = Path.Combine(localFsPath, string.Format("{0}-{1}-{2}-{3}", contentFragmentPage.ThemeName, contentFragmentPage.ThemeContextId, additionalId, "axd.js"));

                using (WebResponse wr = request.GetResponse())
                {
                    if (string.Compare(wr.ContentType, "text/javascript", true) == 0)
                    {
                        using (Stream s = wr.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                using (FileStream localFile = File.Open(LocalPath, FileMode.Create, FileAccess.Write))
                                {
                                    using (StreamWriter sw = new StreamWriter(localFile))
                                    {
                                        sw.Write(sr.ReadToEnd());

                                        Type = type;
                                        OrignalUri = uri;
                                        RelativeUri = ResolveVirtual(LocalPath);
                                        IsValid = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException( "Failed to build remote file lookup", ex).Log();
            }
        }
    }
}