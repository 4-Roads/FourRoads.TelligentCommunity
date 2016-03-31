using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Controls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Plugins.Base
{

    public abstract class SqlScriptsInstaller : IInstallablePlugin , IConfigurablePlugin
    {
        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }
        public readonly static string DefaultConnectionString = Telligent.Common.DataProvider.GetConnectionString();
        private string _connectionString = null;

        #region IPlugin Members

        public string Name
        {
            get { return ProjectName + " - SQL Installer"; }
        }

        public string Description
        {
            get { return "Defines the SQL to be installed for " + ProjectName + "."; }
        }

        public void Initialize()
        {
        }

        #endregion

        public void Install(Version lastInstalledVersion)
        {
            if (lastInstalledVersion < Version)
            {
                Uninstall();
                EmbeddedResources.EnumerateReosurces(BaseResourcePath + "Sql.", ".install.sql", resourceName =>
                {
                    using (var connection = GetSqlConnection())
                    {
                        connection.Open();
                        foreach (string statement in GetStatementsFromSqlBatch(EmbeddedResources.GetString(resourceName)))
                        {
                            using (var command = new SqlCommand(statement, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        connection.Close();
                    }
                });
            }
        }

        public void Uninstall()
        {
            if (!Diagnostics.IsDebug(GetType().Assembly))
            {
                EmbeddedResources.EnumerateReosurces(BaseResourcePath + "Sql.", ".uninstall.sql", resourceName =>
                {
                    using (var connection = GetSqlConnection())
                    {
                        connection.Open();
                        foreach (string statement in GetStatementsFromSqlBatch(EmbeddedResources.GetString(resourceName)))
                        {
                            using (var command = new SqlCommand(statement, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        connection.Close();
                    }
                });
            }
        }

        private SqlConnection GetSqlConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        private static IEnumerable<string> GetStatementsFromSqlBatch(string sqlBatch)
        {
            // This isn't as reliable as the SQL Server SDK, but works for most SQL batches and prevents another assembly reference
            foreach (string statement in Regex.Split(sqlBatch, @"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                string sanitizedStatement = Regex.Replace(statement, @"(?:^SET\s+.*?$|\/\*.*?\*\/|--.*?$)", "\r\n", RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();
                if (sanitizedStatement.Length > 0)
                    yield return sanitizedStatement;
            }
        }

        public Version Version { get { return GetType().Assembly.GetName().Version; } }

        protected string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                    _connectionString = DefaultConnectionString;

                return _connectionString;
            }
            set { _connectionString = value; }
        }

        public void Update(IPluginConfiguration configuration)
        {
            string configuredConnectionString = configuration.GetString("privConnectionString");

            ConnectionString = configuredConnectionString;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup sqlSettings = new PropertyGroup("installation" , "Connection" , 0);

                sqlSettings.Properties.Add(new Property("privConnectionString", "Privileged Connection String",
                    PropertyType.String, 0, string.Empty)
                {
                    ControlType = typeof(PasswordPropertyControl)
                });


                return new[] {sqlSettings};
            }
        }
    }
}
