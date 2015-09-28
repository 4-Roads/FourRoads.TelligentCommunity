using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Entities;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Data;
using Telligent.Common;
using Telligent.Common.Diagnostics.Tracing;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.FileNotification.Data
{
    public class FileSubscriptionDataService : IFileSubscriptionDataService
    {
         protected string DatabaseOwner = "dbo";
         public string ConnectionString { get; set; }

         public FileSubscriptionDataService()
         {
             ConnectionString = DataProvider.GetConnectionString();
         }
       
        protected SqlConnection GetSqlConnection()
        {
            try
            {
                return new SqlConnection(ConnectionString);
            }
            catch
            {
                throw new CSException(CSExceptionType.DataProvider, "SQL Connection String is invalid.");
            }
        }

        protected SqlCommand CreateSprocCommand(string sprocName, SqlConnection connection)
        {
            var sqlCommand = new SqlCommand(DatabaseOwner + "." + sprocName, connection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            return sqlCommand;
        }


        public void UnsubscribeFromFile(Guid fileSubscriptionId, int fileId)
        {
            using (new TracePoint("[sproc] te_File_FileSubscription_Unsubscribe"))
            {
                using (SqlConnection sqlConnection = GetSqlConnection())
                {
                    using (SqlCommand sprocCommand = CreateSprocCommand("te_File_FileSubscription_Unsubscribe", sqlConnection))
                    {
                        sprocCommand.Parameters.Add("@FileSubscriptionId", SqlDbType.UniqueIdentifier).Value = (object)fileSubscriptionId;
                        sprocCommand.Parameters.Add("@FileId", SqlDbType.Int).Value = fileId;
                        sqlConnection.Open();
                        sprocCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                       
                    }
                }
            }
        }

        public Guid SubscribeToFile(FileSubscription fileSubscription)
        {
            Guid response = Guid.Empty;
            using (new TracePoint("[sproc] te_File_FileSubscription_Subscribe"))
            {
                using (SqlConnection sqlConnection = GetSqlConnection())
                {
                    using (SqlCommand sprocCommand = CreateSprocCommand("te_File_FileSubscription_Subscribe", sqlConnection))
                    {
                        sprocCommand.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = (object)fileSubscription.Email;
                        sprocCommand.Parameters.Add("@FileId", SqlDbType.Int, 4).Value = (object)fileSubscription.FileId;
                        sprocCommand.Parameters.Add("@UserId", SqlDbType.Int, 4).Value = (object)fileSubscription.UserId;
                        sprocCommand.Parameters.Add("@SubscriptionType", SqlDbType.Int).Value = (object)(fileSubscription.SubscriptionType)?? EmailSubscriptionType.Post;
                        sprocCommand.Parameters.Add("@IsConfirmed", SqlDbType.Bit).Value = (object)fileSubscription.IsConfirmed ?? 1;
                        //sprocCommand.Parameters.Add("@FileSubscriptionId", SqlDbType.UniqueIdentifier).Value = (object)fileSubscription.FileSubscriptionId;

                        var outputParameter = new SqlParameter();
                        outputParameter.ParameterName = "@FileSubscriptionId";
                        outputParameter.SqlDbType = SqlDbType.UniqueIdentifier;
                        outputParameter.Direction = ParameterDirection.Output;
                        sprocCommand.Parameters.Add(outputParameter);
                        sqlConnection.Open();
                        sprocCommand.ExecuteNonQuery();
                        sqlConnection.Close();

                        string g = outputParameter.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(g))
                             Guid.TryParse(g, out response);
                        
                    }
                    return response;
                }
               
            }
        }

        public bool IsSubscribed(Guid fileSubscriptionId, int userId)
        {
            bool response = false;
            using (new TracePoint("[sproc] te_File_FileSubscription_IsSubscribed"))
            {
                using (SqlConnection sqlConnection = GetSqlConnection())
                {
                    using (SqlCommand sprocCommand = CreateSprocCommand("te_File_FileSubscription_IsSubscribed",sqlConnection))
                    {
                        sprocCommand.Parameters.Add("@UserId", SqlDbType.Int, 4).Value = userId;
                        sprocCommand.Parameters.Add("@FileSubscriptionId", SqlDbType.UniqueIdentifier).Value =(object)fileSubscriptionId;

                        var outputParameter = new SqlParameter();
                        outputParameter.ParameterName = "@IsConfirmed";
                        outputParameter.SqlDbType = SqlDbType.Bit;
                        outputParameter.Direction = ParameterDirection.Output;
                        sprocCommand.Parameters.Add(outputParameter);

                        sqlConnection.Open();
                        sprocCommand.ExecuteNonQuery();
                        sqlConnection.Close();

                        response = (bool)outputParameter.Value;
                    }
                }
            }
            return response;
        }

       public  Guid GetSubscriptionId(int fileId, int userId)
        {
            Guid response = Guid.Empty;
            using (new TracePoint("[sproc] te_File_FileSubscription_GetSubscriptionId"))
            {
                using (SqlConnection sqlConnection = GetSqlConnection())
                {
                    using (SqlCommand sprocCommand = CreateSprocCommand("te_File_FileSubscription_GetSubscriptionId", sqlConnection))
                    {

                        sprocCommand.Parameters.Add("@FileId", SqlDbType.Int, 4).Value = (object)fileId;
                        sprocCommand.Parameters.Add("@UserId", SqlDbType.Int, 4).Value = (object)userId;

                        // The output parameter 
                        var outputParameter = new SqlParameter();
                        outputParameter.ParameterName = "@FileSubscriptionId";
                        outputParameter.SqlDbType = SqlDbType.UniqueIdentifier;
                        outputParameter.Direction = ParameterDirection.Output;
                        sprocCommand.Parameters.Add(outputParameter);

                        sqlConnection.Open();
                        sprocCommand.ExecuteNonQuery();
                        sqlConnection.Close();

                        string g = sprocCommand.Parameters["@FileSubscriptionId"].Value.ToString();
                        if (!string.IsNullOrEmpty(g))
                            response = new Guid(g);
                      
                    }
                }
                return response;
            }
        }

       public  PagedSet<User> GetEmailsFileSubscriptions(int fileId, int pageIndex, int pageSize)
       {
           var hashtable = new Hashtable();
           var userList = new List<User>();
           using (new TracePoint("[sproc] te_File_FileSubscription_GetEmails"))
           {
               using (SqlConnection sqlConnection = this.GetSqlConnection())
               {
                   using (SqlCommand sprocCommand = this.CreateSprocCommand("te_File_FileSubscription_GetEmails", sqlConnection))
                   {
                       sprocCommand.Parameters.Add("@FileId", SqlDbType.Int).Value = (object)fileId;
                       sprocCommand.Parameters.Add("@PageIndex", SqlDbType.Int).Value = (object)pageIndex;
                       sprocCommand.Parameters.Add("@PageSize", SqlDbType.Int).Value = (object)pageSize;
                       sprocCommand.Parameters.Add("@TotalRecords", SqlDbType.Int).Direction = ParameterDirection.Output;
                       sqlConnection.Open();
                       using (SqlDataReader sqlDataReader = sprocCommand.ExecuteReader(CommandBehavior.CloseConnection))
                       {
                           while (sqlDataReader.Read())
                           {
                               User user = CommonDataProvider.cs_PopulateUserFromIDataReader((IDataReader)sqlDataReader);
                               user.Email = DataRecordHelper.GetString((IDataRecord)sqlDataReader, "AlternateEmail", string.Empty);
                               user.SetExtendedAttribute("SubscriptionType", DataRecordHelper.GetInt32((IDataRecord)sqlDataReader, "SubscriptionType").ToString());
                               user.SetExtendedAttribute("SubscriptionID", DataRecordHelper.GetGuid((IDataRecord)sqlDataReader, "FileSubscriptionId").ToString());
                               user.SetExtendedAttribute("SectionID", DataRecordHelper.GetInt32((IDataRecord)sqlDataReader, "FileId").ToString());
                               user.IsAnonymous = true;
                               user.EnableThreadTracking = true;
                               if (!string.IsNullOrEmpty(user.Email) && !hashtable.ContainsKey((object)user.Email))
                               {
                                   hashtable.Add((object)user.Email, (object)true);
                                   userList.Add(user);
                               }
                           }
                           sqlDataReader.Close();
                       }
                       sqlConnection.Close();
                       int totalItems = Convert.ToInt32(sprocCommand.Parameters["@TotalRecords"].Value);
                       return new PagedSet<User>(pageIndex, pageSize, totalItems, (IList)userList);
                   }
               }
           }
       }
    }
}
