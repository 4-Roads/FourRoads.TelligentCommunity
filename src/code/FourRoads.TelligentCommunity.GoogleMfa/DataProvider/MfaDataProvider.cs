using System;
using System.Data;
using System.Data.SqlClient;
using FourRoads.TelligentCommunity.GoogleMfa.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.ForumLastPost.DataProvider
{
    public class MfaDataProvider : IMfaDataProvider
    {
        public void SetUserState(int userId, string sessionId="", bool passed=false)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaSession_Update]", connection))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@SessionId", SqlDbType.NVarChar,10).Value = sessionId;
                    command.Parameters.Add("@Valid", SqlDbType.Bit).Value = passed? 1:0;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public bool GetUserState(int userId, string sessionId)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaSession_Get]", connection))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var sessionIdResult = Convert.ToString(reader["SessionId"]);
                            var valid = Convert.ToBoolean(reader["Valid"]);

                            if (valid && String.CompareOrdinal(sessionIdResult, sessionId) == 0)
                                return true;
                        }
                    }
                    connection.Close();
                }
            }

            return false;
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
