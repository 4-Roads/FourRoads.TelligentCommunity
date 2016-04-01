using Ninject;

namespace FourRoads.Common.TelligentCommunity.Plugins.HttpModules
{
    public class DefaultIocHandler : Settings<DefaultIocHandler>
    {
        public override void LoadConfiguration()
        {
            InjectionModules = new InjectionModules
            {
                Modules = new[] { new InjectionModule { Type = typeof(DefaultNinjectModule).AssemblyQualifiedName } }
            };
        }

        public override IKernel ParentKernel
        {
            get { return new WrappedKernel(); }
        }
    }
}
