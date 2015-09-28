using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.ForumLastPost.Interfaces;
using FourRoads.TelligentCommunity.ForumLastPost.Logic;
using Telligent.Common;

namespace FourRoads.TelligentCommunity.ForumLastPost.DataProvider
{
    public class LastReadPostDataProvider : ILastReadPostDataProvider
    {
        public readonly static string DefaultConnectionString = Telligent.Common.DataProvider.GetConnectionString();


        public void UpdateLastReadPost(Guid appicationId, Guid contentId, int userId, Guid lastReadContentId , int replyCount , DateTime postDate)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_LastReadPost_Update]", connection))
                {
                    command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = appicationId;
                    command.Parameters.Add("@ContentId", SqlDbType.UniqueIdentifier).Value = contentId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@LastReadContentId", SqlDbType.UniqueIdentifier).Value = lastReadContentId;
                    command.Parameters.Add("@LastReadReplyCount", SqlDbType.Int).Value = replyCount;
                    command.Parameters.Add("@LastReadDate", SqlDbType.DateTime).Value = postDate;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public LastReadPostInfo GetLastReadPost(Guid appicationId, Guid contentId, int userId)
        {
            LastReadPostInfo lpi = new LastReadPostInfo();
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_LastReadPost_LastReadPost]", connection))
                {
                    command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = appicationId;
                    command.Parameters.Add("@ContentId", SqlDbType.UniqueIdentifier).Value = contentId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lpi.ContentId = new Guid(Convert.ToString(reader["LastReadContentId"]));
                            lpi.ReplyCount = Convert.ToInt32(reader["LastReadReplyCount"]);
                            lpi.PostDate = Convert.ToDateTime(reader["LastReadDate"]);
                        }
                    }
                    connection.Close();
                }
            }

            return lpi;
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
