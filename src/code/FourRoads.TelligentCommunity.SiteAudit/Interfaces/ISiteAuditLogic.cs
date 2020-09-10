using FourRoads.TelligentCommunity.SiteAudit.Models;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;

namespace FourRoads.TelligentCommunity.SiteAudit.Interfaces
{
    public interface ISiteAuditLogic
    {
        void Initialize();

        void RegisterUrls(IUrlController controller);

        IList<ThemePagesWidgets> GetPages(bool forceDefault);

        IList<Widget> ListWidgets(System.Collections.IDictionary options);
    }
}
