using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;


namespace FourRoads.TelligentCommunity.Nexus2
{
    public class FileInstaller : CfsFilestoreInstaller
    {
        private EmbeddedResources _resources = new EmbeddedResources();

        protected override string ProjectName
        {
            get { return "Nexus2"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.Nexus2.Resources"; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return _resources; }
        }
    }
}
