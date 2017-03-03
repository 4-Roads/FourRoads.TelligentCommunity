using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DryIoc;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Interfaces
{
    public interface IBindingsLoader
    {
        void LoadBindings(IContainer container);
        int LoadOrder { get; }
    }
}
