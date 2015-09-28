namespace FourRoads.TelligentCommunity.Sentrus.HealthExtensions
{
    using System;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Data.SqlTypes;
    using Telligent.DynamicConfiguration.Components;

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
                sqlCommand = string.Format(GetAgeSqlStatement(),
                    new SqlDateTime(
                        DateTime.Now.Subtract(new TimeSpan(Configuration.GetInt("PurgeRecordsCount"), 0, 0, 0))).Value
                        .ToString("s"));
            }
            else
            {
                sqlCommand = string.Format(GetRowsSqlStatement(), Configuration.GetInt("PurgeRecordsCount"));
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

            Property truncationType = new Property("PurgeRecordsType", "Purge Records", PropertyType.String, 1,
                PurgeRecords.AfterAgeInDays.ToString());
            truncationType.SelectableValues.Add(new PropertyValue(PurgeRecords.AfterAgeInDays.ToString(),
                "Older Than Days", 0));
            truncationType.SelectableValues.Add(new PropertyValue(PurgeRecords.AtCount.ToString(), "Database Rows", 1));
            group.Properties.Add(truncationType);

            Property count = new Property("PurgeRecordsCount", "", PropertyType.Int, 2, "10000");
            group.Properties.Add(count);

            return group;
        }
    }
}