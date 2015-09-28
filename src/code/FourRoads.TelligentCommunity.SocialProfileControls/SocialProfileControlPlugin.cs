using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using System.Linq.Expressions;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;

namespace FourRoads.TelligentCommunity.SocialProfileControls
{
    public class SocialProfileControlPlugin : IPlugin , IInstallablePlugin , IPluginGroup
    {
        public readonly static string DefaultConnectionString = Telligent.Common.DataProvider.GetConnectionString();
        private PluginGroupLoader _pluginGroupLoader;

        public void Initialize()
        {
            
        }

        public string Name
        {
            get { return "4 Roads - Social Profile Field Controls"; }
        }

        public string Description
        {
            get { return "This plugin extends Zimbra Community to include additional profile types that are social friendly"; }
        }

        private void EnumeratePluginsAndAction(Func<Type , string, string , string> commandString)
        {
            //Add the profile fields to 
            var plugins = PluginManager.Get<IProfilePlugin>();

            foreach (IProfilePlugin socialProfilePlugin in plugins)
            {
                Type type = socialProfilePlugin.GetType();

                using (var connection = new SqlConnection(DefaultConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand(commandString(type, socialProfilePlugin.FieldName, socialProfilePlugin.FieldType), connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

        }

        public void Install(Version lastInstalledVersion)
        {
            EnumeratePluginsAndAction((t , n , ft) => string.Format(
                @" IF (NOT EXISTS(SELECT 1 FROM dbo.cs_Profile_FieldTypes WHERE  Name='{1}'))  INSERT INTO dbo.cs_Profile_FieldTypes (Name , IsMultipleChoice , DataType , ControlType , IsSearchable ) VALUES (N'{1}' , 0 , N'{2}' , '{0}' , 1) ELSE UPDATE dbo.cs_Profile_FieldTypes SET ControlType= N'{0}' WHERE Name='{1}' ", t.AssemblyQualifiedName, n , ft));
        }

        public void Uninstall()
        {
            //Can never delete as the field might be in use
            EnumeratePluginsAndAction((t, n , ft) => string.Format("UPDATE dbo.cs_Profile_FieldTypes SET ControlType= N'' WHERE Name='{0}'" , n));
        }

        public Version Version
        {
            get
            {
                AssemblyVersionAttribute version =  Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyVersionAttribute)).FirstOrDefault() as AssemblyVersionAttribute;

                if (version != null)
                {
                    return new Version(version.Version);
                }
                return new Version("0.0.0.1");
            }
        }

        private class PluginGroupLoaderTypeVisitor : FourRoads.Common.TelligentCommunity.Plugins.Base.PluginGroupLoaderTypeVisitor
        {
            public override Type GetPluginType()
            {
                return typeof(IProfilePlugin);
            }
        }

        public IEnumerable<Type> Plugins
        {
            get
            {
                if (_pluginGroupLoader == null)
                {
                    _pluginGroupLoader = new PluginGroupLoader();

                    _pluginGroupLoader.Initialize(new PluginGroupLoaderTypeVisitor());
                }

                return _pluginGroupLoader.GetPlugins();
            }
        }

    }
}
