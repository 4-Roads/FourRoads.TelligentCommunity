using FourRoads.TelligentCommunity.SiteAudit.ScriptedContentFragments;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.SiteAudit.Extensions
{
    public class SiteAuditExtension : IScriptedContentFragmentExtension
    {
        public void Initialize()
        {

        }

        public string Name => "Site Audit Extension";
        public string Description => "Lists pages and widgets information within the site";
        public string ExtensionName => "frcommon_v1_pageAudit";
        public object Extension => new SiteAuditScriptedFragment();

    }
}
