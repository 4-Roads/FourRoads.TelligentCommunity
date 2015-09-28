using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.CustomEditor.Interfaces
{
    public interface ICustomEditorPlugin: ISingletonPlugin
    {
        string EditorName { get; }
        string FileLink { get; }
        string Css { get; }
        IEnumerable<ICentralizedFile> Files { get; }
        bool EditorEnabled { get;  }
        int DefaultWidth { get; }
        int DefaultHeight { get; }

        string GetCallbackUrl(string uploaderId, string ClientID, Guid applicationTypeId, Guid containerTypeId, Guid contentTypeId, string authorizationId, IList<Telligent.Evolution.Urls.Routing.IContextItem> list);
    }
}
