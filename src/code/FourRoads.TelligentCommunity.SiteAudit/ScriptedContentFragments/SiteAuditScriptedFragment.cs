using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.SiteAudit.Interfaces;
using FourRoads.TelligentCommunity.SiteAudit.Models;
using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.SiteAudit.ScriptedContentFragments
{
    public class SiteAuditScriptedFragment
    {
        private ISiteAuditLogic siteAuditLogic;

        private ISiteAuditLogic SiteAuditLogic
        {
            get
            {
                if (siteAuditLogic == null)
                {
                    siteAuditLogic = Injector.Get<ISiteAuditLogic>();
                }

                return siteAuditLogic;
            }
        }


        public IList<ThemePagesWidgets> GetPages()
        {
            var input = SiteAuditLogic.GetPages(false);

            return input;
        }

        public IList<Widget> ListWidgets(System.Collections.IDictionary options)
        {
            var widgets = SiteAuditLogic.ListWidgets(options);

            return widgets;
        }
    }
}
