using DryIoc;

namespace FourRoads.Common.TelligentCommunity.Plugins.Interfaces
{
    public interface IBindingsLoader
    {
        void LoadBindings(IContainer container);
        int LoadOrder { get; }
    }
}
