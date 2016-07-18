using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Components;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{
    public abstract class PluginGroupLoaderTypeVisitor
    {
        public abstract Type GetPluginType();
    }

    public class PluginGroupLoader
    {
        private IEnumerable<Type> _plugins = null;

        public void Initialize(PluginGroupLoaderTypeVisitor visitor, Type[] priorityPlugins = null)
        {
            _plugins = GetPlugins(visitor, priorityPlugins);
        }

        public IEnumerable<Type> GetPlugins()
        {
            if (_plugins == null)
                throw new InvalidOperationException("Must call Initialize first");

            return _plugins;
        }

        private IEnumerable<Type> GetPlugins(PluginGroupLoaderTypeVisitor visitor , Type[] priorityPlugins)
        {
            Type type = visitor.GetPluginType();

            IList<Type> types = AppDomain.CurrentDomain.GetAssemblies().ToList().SelectMany(a =>
            {
                Type pluginType = type;

                try
                {
                    return a.GetTypes().Where(t => pluginType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                }
                catch (Exception ex)
                {
                    if (ex is ReflectionTypeLoadException)
                    {
                        ReflectionTypeLoadException rtl = ex as ReflectionTypeLoadException;

                        foreach (Exception rtlEx in rtl.LoaderExceptions)
                        {
                            new TCException(CSExceptionType.UnknownError,
                                string.Format("Failed to load IApplicationPlugin from {0}, because of:", a.FullName), rtlEx).Log();
                        }
                    }
                    else
                    {
                        new TCException(CSExceptionType.UnknownError,
                            string.Format("Failed to load IApplicationPlugin implementation from {0}", a.FullName), ex).Log();
                    }
                }

                return new Type[0];
            }).ToList();

            // Ensure priority plugins are loaded first, if they are not application plugins then they just get added
            if (priorityPlugins != null)
            {
                for (int i = priorityPlugins.Length - 1; i >= 0; i--)
                {
                    type = priorityPlugins[i];

                    if (types.Contains(type))
                    {
                        types.Remove(type);
                    }

                    types.Insert(0, type);
                }
            }

            return types;
        }

    }
}
