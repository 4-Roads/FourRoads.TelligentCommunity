using System.IO;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.Common.TelligentCommunity.Services.Interfaces
{
    public interface IFileService
    {
        ICentralizedFile AddFile(int entityId, Stream contentStream, long contentLength, string fileName);

        ICentralizedFile AddFile(int entityId, string fileUploadContext, string fileName);

        ICentralizedFile GetFile(int entityId, string fileName);
    }
}
