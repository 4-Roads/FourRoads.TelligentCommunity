namespace FourRoads.TelligentCommunity.Sentrus.HealthExtensions
{
    using Interfaces;
    using Telligent.DynamicConfiguration.Components;
    using Telligent.Evolution.Extensibility.Version1;

    public class ExceptionLogPruner : TruncateTableHealthExtensionBase, IHealthExtension
    {
        private readonly string sqlStatement = @"
            DECLARE @cuttoffDate datetime

            SET @cuttoffDate = '{0}'

            DELETE FROM  cs_exceptions WHERE ExceptionID in 
	            (select ExceptionID from  cs_exceptions 
	            where DateLastOccurred < @cuttoffDate)";
        private readonly string sqlStatementRows = @"
            DECLARE @currentCount int
            DECLARE @maxCount int
            SELECT @currentCount = Count(*) FROM cs_exceptions

            SET @maxCount = {0}

            IF @currentCount > @maxCount
            BEGIN
            SET @currentCount = @currentCount - @maxCount

            DELETE FROM cs_exceptions WHERE ExceptionID in 
	            (select top (@currentCount) ExceptionID from cs_exceptions order by DateLastOccurred asc)

            END";

        protected override string HealthName
        {
            get { return "Exception Log Pruner"; }
        }

        public override PropertyGroup GetConfiguration()
        {
            PropertyGroup group = base.GetConfiguration();

            group.DescriptionText = "Prunes exception log by date or number of rows.";

            return group;
        }

        public override void Initialize()
        {
        }

        public override string Description
        {
            get { return "Prunes the exception log"; }
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
            return new PropertyGroup("exceptions", "Exceptions", 0);
        }
    }
}