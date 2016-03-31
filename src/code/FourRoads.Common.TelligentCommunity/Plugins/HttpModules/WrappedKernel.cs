using System;
using System.Collections.Generic;
using System.Reflection;
using Ninject;
using Ninject.Activation;
using Ninject.Activation.Blocks;
using Ninject.Components;
using Ninject.Modules;
using Ninject.Parameters;
using Ninject.Planning.Bindings;
using Ninject.Syntax;
using Telligent.Common;

namespace FourRoads.Common.TelligentCommunity.Plugins.HttpModules
{
    public class WrappedKernel : IKernel
    {
        #region IResolutionRoot

        public bool CanResolve(IRequest request)
        {
            return CanResolve(request, false);
        }

        public bool CanResolve(IRequest request, bool ignoreImplicitBindings)
        {
            try
            {
                return Services.Get(request.Service) != null;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<object> Resolve(IRequest request)
        {
            return Services.GetAll(request.Service);
        }

        public IRequest CreateRequest(Type service, Func<IBindingMetadata, bool> constraint, IEnumerable<IParameter> parameters, bool isOptional, bool isUnique)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IKernel

        public IBindingToSyntax<T> Bind<T>()
        {
            throw new NotImplementedException();
        }

        public IBindingToSyntax<object> Bind(params Type[] services)
        {
            throw new NotImplementedException();
        }

        public void Unbind<T>()
        {
            throw new NotImplementedException();
        }

        public void Unbind(Type service)
        {
            throw new NotImplementedException();
        }

        public IBindingToSyntax<T> Rebind<T>()
        {
            throw new NotImplementedException();
        }

        public IBindingToSyntax<object> Rebind(Type service)
        {
            throw new NotImplementedException();
        }

        public void AddBinding(IBinding binding)
        {
            throw new NotImplementedException();
        }

        public void RemoveBinding(IBinding binding)
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool IsDisposed { get; private set; }
        public IEnumerable<INinjectModule> GetModules()
        {
            throw new NotImplementedException();
        }

        public bool HasModule(string name)
        {
            throw new NotImplementedException();
        }

        public void Load(IEnumerable<INinjectModule> m)
        {
            throw new NotImplementedException();
        }

        public void Load(IEnumerable<string> filePatterns)
        {
            throw new NotImplementedException();
        }

        public void Load(IEnumerable<Assembly> assemblies)
        {
            throw new NotImplementedException();
        }

        public void Unload(string name)
        {
            throw new NotImplementedException();
        }

        public void Inject(object instance, params IParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public bool Release(object instance)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IBinding> GetBindings(Type service)
        {
            throw new NotImplementedException();
        }

        public IActivationBlock BeginBlock()
        {
            throw new NotImplementedException();
        }

        public INinjectSettings Settings { get; private set; }
        public IComponentContainer Components { get; private set; }

        #endregion

        #region IBindingRoot Members


        public IBindingToSyntax<object> Bind(Type service)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}