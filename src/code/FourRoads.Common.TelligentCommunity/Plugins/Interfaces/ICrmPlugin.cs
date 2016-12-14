using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Interfaces
{
    public interface ICrmPlugin : IPlugin
    {
        void SynchronizeUser(User u);
    }
}
