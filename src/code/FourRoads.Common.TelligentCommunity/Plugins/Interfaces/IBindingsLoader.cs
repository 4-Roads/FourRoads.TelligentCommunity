using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Interfaces
{
    public interface IBindingsLoader
    {
        void LoadBindings(Ninject.Modules.NinjectModule module);
        int LoadOrder { get; }
    }
}
