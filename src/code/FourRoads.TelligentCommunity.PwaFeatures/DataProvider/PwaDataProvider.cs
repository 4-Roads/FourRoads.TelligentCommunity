using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.PwaFeatures.DataProvider
{
    public class PwaDataProvider 
    {
        public void StoreToken(int userId, string token)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_PwaSession_StoreToken]", connection))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@Token", SqlDbType.NVarChar, 4000).Value = token;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public void RevokeToken(int userId, string token)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_PwaSession_RevokeToken]", connection))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@Token", SqlDbType.NVarChar, 4000).Value = token;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public List<string> ListUserTokens(int userId)
        {
            List<string> tokens = new List<string>();
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_PwaSession_ListTokens]", connection))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tokens.Add(reader.GetString(0));
                        }
                    }

                }
            }

            return tokens;
        }

        private static SqlConnection GetSqlConnection()
        {
            return Apis.Get<IDatabaseConnections>().GetConnection("SiteSqlServer");
        }

        private static SqlCommand CreateSprocCommand(string sprocName, SqlConnection connection)
        {
            return new SqlCommand("dbo." + sprocName, connection) { CommandType = CommandType.StoredProcedure };
        }
    }
}
