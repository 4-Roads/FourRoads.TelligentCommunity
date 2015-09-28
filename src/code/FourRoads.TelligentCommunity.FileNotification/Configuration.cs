using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using FourRoads.Common;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Plugins;
using Ninject;
using Telligent.Common;

namespace FourRoads.TelligentCommunity.FileNotification
{
    public  class Configuration  : Settings<Configuration>, IConfiguration
    {
        public const string ConfigFile = "FileNotification.config";

        public Configuration()
        {
            FileName = ConfigFile;
        }

        public override IKernel ParentKernel
        {
            get { return (IKernel)Services.Provider; }
        }
    }
}
