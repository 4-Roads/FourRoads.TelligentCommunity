using DryIoc;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.SiteAudit.Extensions;
using FourRoads.TelligentCommunity.SiteAudit.Interfaces;
using FourRoads.TelligentCommunity.SiteAudit.Logic;
using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.SiteAudit.Plugins
{
    public class SiteAuditPluginCore : IPluginGroup, IBindingsLoader, INavigable
    {
        private ISiteAuditLogic _siteAuditLogic;

        protected internal ISiteAuditLogic SiteAuditLogic
        {
            get
            {
                if (_siteAuditLogic == null)
                {
                    _siteAuditLogic = Injector.Get<ISiteAuditLogic>();
                }
                return _siteAuditLogic;
            }
        }

        public void LoadBindings(IContainer container)
        {
            container.Register<ISiteAuditLogic, SiteAuditLogic>(Reuse.Singleton);
        }

        public void Initialize()
        {

        }

        public string Name => "4 Roads - Site Audit";

        public string Description => "Displays site pages and widgets information";

        public void RegisterUrls(IUrlController controller)
        {
            SiteAuditLogic.RegisterUrls(controller);
        }

        public IEnumerable<Type> Plugins => new[]
        {
            typeof (DependencyInjectionPlugin),
            typeof (FactoryDefaultWidgetProviderInstaller),
            typeof (SiteAuditExtension)
        };

        public int LoadOrder => 0;
    }
}
