using System;
using System.Data;
using System.Data.SqlClient;
using FourRoads.TelligentCommunity.HubSpot.Interfaces;
using FourRoads.TelligentCommunity.HubSpot.Models;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.HubSpot
{
    public class AuthInfoAuthInfoDbConfiguration : IAuthInfoDbConfiguration
    {
        public AuthInfo Get(string clientId)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_HubSpotAuth_Get]", connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(clientId);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return new AuthInfo
                            {
                                AccessToken = Convert.ToString(reader["AccessToken"]),
                                RefreshToken = Convert.ToString(reader["RefreshToken"]),
                                ExpiryUtc = Convert.ToDateTime(reader["ExpiryUtc"])
                            };
                        }
                    }
                    connection.Close();
                }
            }

            return null;
        }

        public void Clear(string clientId)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_HubSpotAuth_Clear]", connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(clientId);

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public AuthInfo Update(string clientId, string accessToken, string refreshToken, DateTime expiryUtc)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_HubSpotAuth_Update]", connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(clientId);
                    command.Parameters.Add("@AccessToken", SqlDbType.NVarChar).Value = accessToken;
                    command.Parameters.Add("@RefreshToken", SqlDbType.NVarChar).Value = refreshToken;
                    command.Parameters.Add("@ExpiryUtc", SqlDbType.DateTime).Value = expiryUtc;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }

            return new AuthInfo
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiryUtc = expiryUtc
            };
        }
        
        private static SqlCommand CreateSprocCommand(string sprocName, SqlConnection connection)
        {
            return new SqlCommand("dbo." + sprocName, connection) { CommandType = CommandType.StoredProcedure };
        }
        
        private static SqlConnection GetSqlConnection()
        {
            return Apis.Get<IDatabaseConnections>().GetConnection("SiteSqlServer");
        }
    }
}