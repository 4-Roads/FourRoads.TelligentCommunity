using System;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FourRoads.TelligentCommunity.MigratorFramework.Entities
{
    public class MigrationContext
    {
        public DateTime Started { get; set; }
        public DateTime LastUpdated { get; set; }
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public MigrationState State { get; set; }
        public decimal RowsProcessingTimeAvg { get; set; }
        public string CurrentObjectType { get; set; }
    }
}