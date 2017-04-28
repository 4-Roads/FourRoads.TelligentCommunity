using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using FourRoads.TelligentCommunity.ForumThreadViews.Entities;
using FourRoads.TelligentCommunity.ForumThreadViews.Interfaces;

namespace FourRoads.TelligentCommunity.ForumThreadViews.DataProvider
{
    public class ThreadViewDataProvider : IThreadViewDataProvider
    {
        public readonly static string DefaultConnectionString = Telligent.Common.DataProvider.GetConnectionString();

        public void Create(Guid appicationId, Guid contentId, int userId, DateTime viewDate, int status = 1)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_Forum_ThreadView_Create]", connection))
                {
                    command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = appicationId;
                    command.Parameters.Add("@ContentId", SqlDbType.UniqueIdentifier).Value = contentId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@ViewDate", SqlDbType.DateTime).Value = viewDate;
                    command.Parameters.Add("@Status", SqlDbType.Int).Value = 1;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public List<ThreadViewInfo> GetNewList(int threshold)
        {
            List<ThreadViewInfo> tvi = new List<ThreadViewInfo>();
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_Forum_ThreadView_NewList]", connection))
                {
                    command.Parameters.Add("@Threshold", SqlDbType.Int).Value = threshold;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                tvi.Add(new ThreadViewInfo()
                                {
                                    ApplicationId = new Guid(Convert.ToString(reader["ApplicationId"])),
                                    ContentId = new Guid(Convert.ToString(reader["ContentId"]))
                                }
                                );
                            }
                        }
                    }
                    connection.Close();
                }
            }
            return tvi;
        }

        private static SqlConnection GetSqlConnection()
        {
            return GetSqlConnection(DefaultConnectionString);
        }

        private static SqlConnection GetSqlConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        private static SqlCommand CreateSprocCommand(string sprocName, SqlConnection connection)
        {
            return new SqlCommand("dbo." + sprocName, connection) { CommandType = CommandType.StoredProcedure };
        }

    }
}
