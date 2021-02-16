using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using FourRoads.TelligentCommunity.Installer.Components.Utility;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;

namespace FourRoads.TelligentCommunity.Installer
{
    public abstract class SqlScriptsInstaller : IInstallablePlugin , IConfigurablePlugin
    {
        protected abstract string ProjectName { get; }
        protected abstract string BaseResourcePath { get; }
        protected abstract EmbeddedResourcesBase EmbeddedResources { get; }

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
                EmbeddedResources.EnumerateResources(BaseResourcePath + "Sql.", ".install.sql", resourceName =>
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
                EmbeddedResources.EnumerateResources(BaseResourcePath + "Sql.", ".uninstall.sql", resourceName =>
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
            return Apis.Get<IDatabaseConnections>().GetConnection(ConnectionString);
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

        public Version Version => GetType().Assembly.GetName().Version;

        protected string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                    _connectionString = "SiteSqlServer";

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
                PropertyGroup sqlSettings = new PropertyGroup() {Id="installation" , LabelText = "Connection"};

                sqlSettings.Properties.Add(new Property
                {
                    Id = "privConnectionString",
                    LabelText = "Privileged Connection String",
                    DataType = "string",
                    Template = "string",
                    OrderNumber = 0,
                    DefaultValue = "",
                    Options = new NameValueCollection
                    {
                        { "obscure", "true" }
                    }
                });

                return new[] {sqlSettings};
            }
        }
    }
}
