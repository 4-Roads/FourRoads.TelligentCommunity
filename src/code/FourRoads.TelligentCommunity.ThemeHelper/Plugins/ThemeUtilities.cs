using System;
using System.Collections.Generic;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.DeveloperTools.Plugins
{
    public class ThemeUtilities : IThemeUtilities
    {
        #region IPlugin Members

        public string Description
        {
            get { return "A collection of plugins to assist theme development"; }
        }

        public void Initialize()
        {
        }

        public string Name
        {
            get { return "4 Roads - Theme utilities"; }
        }

        #endregion

        #region IConfigurablePlugin Members

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup group = new PropertyGroup("setup", "Setup", 1);

                group.Properties.Add(new Property("EnableSourceMap", "Enable Source Map Generation", PropertyType.Bool,2, bool.TrueString));
                group.Properties.Add(new Property("EnableFileSystemWatcher", "Enable File System Watcher", PropertyType.Bool,3, bool.TrueString));
                group.Properties.Add(new Property("EnableThemePageControls", "Enable Theme Page Controls", PropertyType.Bool, 4, bool.TrueString));

                return new[] { group };
            }
        }

        public bool EnableSourceMap { get; set; }
        public bool EnableFileSystemWatcher { get; set; }
        public bool EnableThemePageControls { get; set; }

        public void Update(IPluginConfiguration configuration)
        {
            EnableSourceMap = configuration.GetBool("EnableSourceMap");
            EnableFileSystemWatcher = configuration.GetBool("EnableFileSystemWatcher");
            EnableThemePageControls = configuration.GetBool("EnableThemePageControls");
        }

        #endregion

        #region IPluginGroup Members

        public IEnumerable<Type> Plugins
        {
            get
            {
                // FileSystemUtility must be loaded after ThemeUtility
                return new[]
                {
                    typeof(ThemeUtility),
                    typeof(SourceMapUtility),
                    typeof(FileSystemUtility),
                    typeof(RestEndpoints)
                };
            }
        }

        #endregion
    }
}