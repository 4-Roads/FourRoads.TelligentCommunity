using System;
using System.IO;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.Performance.Interfaces;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.Performance.Storage
{
    public class BundleCfsFile : BundledFileBase, IBundledFile
    {
        public BundleCfsFile(string type, string uri, ContentFragmentPageControl contentFragmentPage, string additionalId)
        {
            try
            {
                ICentralizedFile file = CentralizedFileStorage.GetCentralizedFileByUrl(uri.Replace("cfs-filesystemfile.ashx", "cfs-file.ashx"));

                if (file != null)
                {
                    LocalPath = EnsureLocalCopyExists(file, string.Format("{0}-{1}-{2}-{3}", contentFragmentPage.ThemeName, contentFragmentPage.ThemeContextId, additionalId, file.FileName)); 
                    Type = type;
                    OrignalUri = uri;
                    RelativeUri = ResolveVirtual(LocalPath);
                    IsValid = true;
                }
            }
            catch (Exception ex)
            {
                new TCException(CSExceptionType.UnknownError, "Failed to build remote file lookup", ex).Log();
            }
        }

        protected string EnsureLocalCopyExists(ICentralizedFile file, string replacementFileName)
        {
            string localFsPath = LocalPerformanceCachePath();

            string localFileName = Path.Combine(localFsPath, replacementFileName);

            using (FileStream localFile = File.Open(localFileName, FileMode.Create, FileAccess.Write))
            {
                using (Stream readStream = file.OpenReadStream())
                {
                    Byte[] buffer = new Byte[readStream.Length];

                    readStream.Read(buffer, 0, (int)readStream.Length);

                    localFile.Write(buffer, 0, buffer.Length);
                }
            }

            return localFileName;
        }
    }
}