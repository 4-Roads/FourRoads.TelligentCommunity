using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Plugins;

namespace FourRoads.TelligentCommunity.FileNotification
{
    public class ConfigurationFactory : IConfigurationFactory 
    {
        public IConfiguration GetConfiguration()
        {
            return Configuration.Instance();
        }
    }
}
