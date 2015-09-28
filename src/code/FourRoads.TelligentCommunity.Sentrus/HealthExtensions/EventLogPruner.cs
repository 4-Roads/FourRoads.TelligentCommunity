namespace FourRoads.TelligentCommunity.Sentrus.HealthExtensions
{
    using Interfaces;
    using Telligent.DynamicConfiguration.Components;
    using Telligent.Evolution.Extensibility.Version1;

    public class EventLogPruner : TruncateTableHealthExtensionBase, IHealthExtension
    {
        private readonly string sqlStatement = @"
            DECLARE @cuttoffDate datetime

            SET @cuttoffDate = '{0}'

            DELETE FROM  cs_EventLog WHERE EventLogID in 
	            (select EventLogID from  cs_EventLog 
	            where EventDate < @cuttoffDate)
            ";
        private readonly string sqlStatementRows = @"
            DECLARE @currentCount int
            DECLARE @maxCount int
            SELECT @currentCount = Count(*) FROM cs_EventLog

            SET @maxCount = {0}

            IF @currentCount > @maxCount
            BEGIN
            SET @currentCount = @currentCount - @maxCount

            DELETE FROM cs_EventLog WHERE EventLogID in 
	            (select top (@currentCount) EventLogID from cs_EventLog order by EventDate asc)

            END
        ";

        protected override string HealthName
        {
            get { return "Event Log Pruner"; }
        }

        public override PropertyGroup GetConfiguration()
        {
            PropertyGroup group = base.GetConfiguration();

            group.DescriptionText = "Prunes event log by date or number of rows.";

            return group;
        }

        public override void Initialize()
        {
        }

        public override string Description
        {
            get { return "Prunes the event log"; }
        }

        public override string GetAgeSqlStatement()
        {
            return sqlStatement;
        }

        public override string GetRowsSqlStatement()
        {
            return sqlStatementRows;
        }

        public override void InternalUpdate(IPluginConfiguration configuration)
        {
        }

        protected override PropertyGroup GetRootGroup()
        {
            return new PropertyGroup("events", "Event Log", 0);
        }
    }
}