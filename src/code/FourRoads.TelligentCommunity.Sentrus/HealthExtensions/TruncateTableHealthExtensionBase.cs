namespace FourRoads.TelligentCommunity.Sentrus.HealthExtensions
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Data.SqlTypes;
    using Telligent.Evolution.Extensibility.Configuration.Version1;

    public abstract class TruncateTableHealthExtensionBase : HealthExtensionBase
    {
        public override PropertyGroup[] ConfigurationOptions
        {
            get { return new[] {GetConfiguration()}; }
        }

        public abstract string GetAgeSqlStatement();
        public abstract string GetRowsSqlStatement();

        public override void InternalExecute()
        {
            if (!IsEnabled)
                return;

            string sqlCommand;


            if (Configuration.GetString("PurgeRecordsType") == PurgeRecords.AfterAgeInDays.ToString())
            {
                var days = Configuration.GetInt("PurgeRecordsCount").HasValue ? Configuration.GetInt("PurgeRecordsCount").Value : 180;
                sqlCommand = string.Format(GetAgeSqlStatement(),
                    new SqlDateTime(
                        DateTime.Now.Subtract(new TimeSpan(days, 0, 0, 0))).Value
                        .ToString("s"));
            }
            else
            {
                var rows = Configuration.GetInt("PurgeRecordsCount").HasValue ? Configuration.GetInt("PurgeRecordsCount").Value : 10000;
                sqlCommand = string.Format(GetRowsSqlStatement(), rows);
            }

            using (
                SqlConnection connection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                {
                    connection.Open();

                    command.ExecuteNonQuery();
                }
            }
        }

        public override PropertyGroup GetConfiguration()
        {
            PropertyGroup group = base.GetConfiguration();

            Property truncationType = new Property
            {
                Id = "PurgeRecordsType",
                LabelText = "Purge Records",
                DataType = "String",
                DefaultValue = PurgeRecords.AfterAgeInDays.ToString()
            };

            truncationType.SelectableValues.Add(new PropertyValue {
                Value = PurgeRecords.AfterAgeInDays.ToString(),
                LabelText = "Older Than Days",
                OrderNumber = 0
            });
            
            truncationType.SelectableValues.Add(new PropertyValue {
                Value = PurgeRecords.AtCount.ToString(),
                LabelText = "Database Rows",
                OrderNumber = 1
            });
            group.Properties.Add(truncationType);

            group.Properties.Add(new Property
            {
                Id = "PurgeRecordsCount",
                LabelText = "Purge Records Count",
                DataType = "int",
                Template = "int",
                DefaultValue = "10000",
                Options = new NameValueCollection
                {
                    { "presentationDivisor", "1" },
                    { "inputType", "number" },
                }
            });

            return group;
        }
    }
}