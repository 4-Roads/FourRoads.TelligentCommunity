using System;
using System.Data;
using System.Data.SqlClient;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Model;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.Mfa.DataProvider
{
    public class MfaDataProvider : IMfaDataProvider
    {
        private static SqlConnection GetSqlConnection()
        {
            return Apis.Get<IDatabaseConnections>().GetConnection("SiteSqlServer");
        }

        private static SqlCommand CreateSprocCommand(string sprocName, SqlConnection connection)
        {
            return new SqlCommand("dbo." + sprocName, connection) { CommandType = CommandType.StoredProcedure };
        }

        public bool RedeemCode(int userId, string encryptedCode)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaOTCodes_VerifyUnused]", connection))
                {
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@code", SqlDbType.Char).Value = encryptedCode;

                    connection.Open();
                    var reader = command.ExecuteScalar();
  
                    if (reader != null)
                    {
                        var codeId = Guid.Parse(Convert.ToString(reader));
                        using (var updateCommand = CreateSprocCommand("[fr_MfaOTCodes_Redeem]", connection))
                        {
                            updateCommand.Parameters.Add("@codeId", SqlDbType.UniqueIdentifier).Value = codeId;
                            updateCommand.Parameters.Add("@redeemedAtUtc", SqlDbType.DateTime).Value = DateTime.UtcNow;
                            updateCommand.ExecuteNonQuery();
                        }
                        return true;
                    }
                    connection.Close();
                }
            }

            return false;
        }

        public void ClearCodes(int userId)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaOTCodes_RemoveAll]", connection))
                {
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public OneTimeCode CreateCode(int userId, string encryptedCode)
        {
            var code = new OneTimeCode {
                UserId = userId
            };

            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaOTCodes_Create]", connection))
                {
                    command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = code.Id;
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = code.UserId;
                    command.Parameters.Add("@code", SqlDbType.Char).Value = encryptedCode;
                    command.Parameters.Add("@generatedOnUtc", SqlDbType.DateTime).Value = code.CreatedOnUtc;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return code;
        }

        public int CountCodesLeft(int userId)
        {
            int count;
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaOTCodes_CountUsableCodes]", connection))
                {
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                    connection.Open();
                    var res = command.ExecuteScalar();
                    connection.Close();
                    count = int.Parse(res.ToString());
                }
            }
            return count;
        }

        public void SetUserKey(int userId, Guid key)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaKeys_Update]", connection))
                {
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@userKey", SqlDbType.UniqueIdentifier).Value = key;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public Guid GetUserKey(int userId)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaKeys_Get]", connection))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    connection.Open();
                    var result = command.ExecuteScalar();
                    connection.Close();

                    if(result != null)
                    {
                        return Guid.Parse(result.ToString());
                    }
                    return Guid.Empty;
                }
            }
        }

        public void ClearUserKey(int userId)
        {
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[fr_MfaKeys_Clear]", connection))
                {
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }
    }
}
