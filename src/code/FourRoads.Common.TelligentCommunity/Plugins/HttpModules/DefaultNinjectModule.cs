using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using Ninject.Modules;
using Telligent.Evolution.Components;

namespace FourRoads.Common.TelligentCommunity.Plugins.HttpModules
{
    public class DefaultNinjectModule : NinjectModule
    {
        public override void Load()
        {
            // Only include one instance of common bindings
            Type type = typeof(IBindingsLoader);
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().ToList().SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch (Exception ex)
                        {
                            if (ex is ReflectionTypeLoadException)
                            {
                                ReflectionTypeLoadException rtl = ex as ReflectionTypeLoadException;

                                foreach (Exception rtlEx in rtl.LoaderExceptions)
                                {
                                    new TCException(CSExceptionType.UnknownError, string.Format("Failed to load bindings extension from {0}, because of:", a.FullName), rtlEx).Log();
                                }
 
                            }
                            else
                            {
                                new TCException(CSExceptionType.UnknownError, string.Format("Failed to load bindings extension from {0}", a.FullName), ex).Log();
                            }
                        }
                        return new Type[0];
                    }
            ).
            Where(t => type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            List<IBindingsLoader> bindingsLoaders = new List<IBindingsLoader>();

            foreach (var bindingsType in types)
            {
                try
                {
                    bindingsLoaders.Add(AppDomain.CurrentDomain.CreateInstanceAndUnwrap(bindingsType.Assembly.FullName, bindingsType.FullName) as IBindingsLoader);
                }
                catch (Exception ex)
                {
					new TCException(CSExceptionType.UnknownError, string.Format("Failed to load {0} bindings extension", bindingsType.FullName), ex).Log();
                }
            }

            bindingsLoaders.Sort((a, b) => a.LoadOrder - b.LoadOrder);

            foreach (IBindingsLoader bindingsLoader in bindingsLoaders)
            {
                bindingsLoader.LoadBindings(this);
            }
        }
    }
}