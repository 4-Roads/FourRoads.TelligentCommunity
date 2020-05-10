// //  ------------------------------------------------------------------------------
// //  <copyright company="4 Roads LTD">
// //  Copyright (c) 4 Roads LTD 2019.  All rights reserved.
// //  </copyright>
// //  ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DryIoc;
using FourRoads.Common.Interfaces;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using Telligent.Common;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api;
using Telligent.Evolution.Extensibility.Version1;
using FourRoads.Common.Sql;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public class Injector : IObjectFactory
    {
        public static T Get<T>()
        {
            return DependencyContainer.Container().Resolve<T>();
        }

        public object Get(Type type)
        {
            return DependencyContainer.Container().Resolve(type);
        }

        T IObjectFactory.Get<T>()
        {
            return DependencyContainer.Container().Resolve<T>();
        }
    }


    public static class DependencyContainer
    {
        internal static Container _container;
        internal static object _lock = new object();

        static DependencyContainer()
        {
        }

        public static Container Container()
        {
            lock (_lock)
            {
                if (_container == null)
                {
                    // Only include one instance of common bindings
                    Type type = typeof(IBindingsLoader);
                    var containingAssembly = type.Assembly.GetName().FullName;

                    var assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
                    
                    var assemblies = assemblyArray.Where(asm => {

                        // Only interested in assemblies which reference this one.
                        // If they don't then they can't possibly contain any IBindingsLoader implementors.
                        // This filters hundreds of assemblies (which take minutes to scan during startup) to usually a handful:
                        var refs = asm.GetReferencedAssemblies();
                        
                        foreach (var reference in refs)
                        {
                            if (reference.FullName == containingAssembly)
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                    var asmList = assemblies.ToList();

                    //Add yourself as you contain bindings
                    asmList.Add(typeof(DependencyContainer).Assembly);

                    IEnumerable<Type> types = assemblies.SelectMany(a =>
                        {
                            try
                            {
                                return a.ExportedTypes.Where(t => type.IsAssignableFrom(t) && !t.IsInterface() && !t.IsAbstract());
                            }
                            catch (Exception ex)
                            {
                                if (ex is ReflectionTypeLoadException)
                                {
                                    ReflectionTypeLoadException rtl = ex as ReflectionTypeLoadException;

                                    foreach (Exception rtlEx in rtl.LoaderExceptions)
                                    {
                                        new TCException(string.Format("Failed to load bindings extension from {0}, because of:", a.FullName), rtlEx).Log();
                                    }

                                }
                                else
                                {
                                    new TCException(string.Format("Failed to load bindings extension from {0}", a.FullName), ex).Log();
                                }
                            }
                            return new Type[0];
                        }
                    );
                    List<IBindingsLoader> bindingsLoaders = new List<IBindingsLoader>();

                    foreach (var bindingsType in types)
                    {
                        try
                        {
                            bindingsLoaders.Add(AppDomain.CurrentDomain.CreateInstanceAndUnwrap(bindingsType.Assembly.FullName, bindingsType.FullName) as IBindingsLoader);
                        }
                        catch (Exception ex)
                        {
                            new TCException(string.Format("Failed to load {0} bindings extension", bindingsType.FullName), ex).Log();
                        }
                    }

                    bindingsLoaders.Sort((a, b) => a.LoadOrder - b.LoadOrder);

                    _container = new Container(rules => rules.WithAutoConcreteTypeResolution().With(FactoryMethod.ConstructorWithResolvableArguments).WithUnknownServiceResolvers(request =>
                        new DelegateFactory(_ =>
                        {
                            if (request.ServiceType.GetInterfaces().Contains(typeof(IApi)))
                            {
                                // Get the generic type definition
                                MethodInfo method = typeof(Apis).GetMethod("Get", BindingFlags.Public | BindingFlags.Static);

                                // Build a method with the specific type argument you're interested in
                                method = method.MakeGenericMethod(request.ServiceType);
                                // The "null" is because it's a static method
                                return method.Invoke(null, null);
                            }

                            var results = Services.GetAll(request.ServiceType);

                            return results.FirstOrDefault();
                        }
                            )));

                    foreach (IBindingsLoader bindingsLoader in bindingsLoaders)
                    {
                        bindingsLoader.LoadBindings(_container);
                    }

                    _container.Register<FourRoads.Common.Interfaces.ICache, TCCache>();
                    _container.Register<FourRoads.Common.Interfaces.IObjectFactory, Injector>();
                    _container.Register<FourRoads.Common.Interfaces.IPagedCollectionFactory, PagedCollectionFactory>();
                    _container.Register<FourRoads.Common.Sql.IDataHelper, SqlDataHelper>();
                    _container.Register<FourRoads.Common.Sql.IDBFactory, SqlDataLayer>();
                }

            }
            
            return _container;
        }
    }

    public class DependencyInjectionPlugin : ISingletonPlugin
    {
        public DependencyInjectionPlugin()
        {
            DependencyContainer.Container();
        }

        public void Initialize()
        {

        }

        public string Name => "4-Roads DI";

        public string Description => "This plugin ensures that dependency injection has been initialized";
    }
}