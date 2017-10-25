using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data.SqlClient;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using System.Data;
using FourRoads.TelligentCommunity.Sentrus.Entities;

namespace FourRoads.TelligentCommunity.Sentrus.Logic
{

    public class UserHealth : IUserHealth
    {
        private Role _administratorRole;

        public Role AdministratorRole
        {
            get
            {
                if (_administratorRole == null)
                {
                    _administratorRole = Apis.Get<IRoles>().Find("Administrators").FirstOrDefault();
                }
                return _administratorRole;
            }
        }



        public IEnumerable<User> GetInactiveUsers(int accountAge , bool includeIgnored = false)
        {
            if (accountAge <= 0)
            {
                //avoid returning everyone who logged in before NOW or some future date 
                yield break;
            }
            User anon = Apis.Get<IUsers>().Get(new UsersGetOptions { Username = "anonymous" });

            if (anon != null && AdministratorRole != null)
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand("dbo.fr_LastLogin_List", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.Add("@LastLogonDate", SqlDbType.DateTime).Value = DateTime.Now.AddMonths(accountAge * -1);
                        command.Parameters.Add("@excludeIgnored", SqlDbType.Bit).Value = includeIgnored ? 0 : 1;

                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Guid userId = new Guid(Convert.ToString(reader["MembershipId"]));

                                User usr = Apis.Get<IUsers>().Get(new UsersGetOptions { ContentId = userId });

                                if (usr != null)
                                {
                                    if (usr.Id != anon.Id &&
                                        !Apis.Get<IRoleUsers>().IsUserInRoles(usr.Username, new[] {AdministratorRole.Name}))
                                    {
                                        yield return usr;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public Entities.LastLoginDetails GetLastLoginDetails(Guid guid)
                                    {
            LastLoginDetails loginDetails = null;

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand("dbo.fr_LastLogin_Get", connection) { CommandType = CommandType.StoredProcedure })
                {
                    command.Parameters.Add("@MembershipId", SqlDbType.UniqueIdentifier).Value = guid;

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            loginDetails = new LastLoginDetails();

                            loginDetails.EmailCountSent = Convert.ToInt32(reader["EmailCountSent"]);
                            loginDetails.MembershipId = new Guid(Convert.ToString(reader["MembershipId"]));
                            loginDetails.LastLogonDate = Convert.ToDateTime(reader["LastLogonDate"]);
                            loginDetails.IgnoredUser = Convert.ToBoolean(reader["IgnoredUser"]);

                            if (!Convert.IsDBNull(reader["FirstEmailSentAt"]))
                                loginDetails.FirstEmailSentAt = Convert.ToDateTime(reader["FirstEmailSentAt"]);
                                    }
                                }
                            }
                        }

           return loginDetails;
        }

        public void CreateUpdateLastLoginDetails(Entities.LastLoginDetails lastLoginData)
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand("dbo.fr_LastLogin_CreateUpdate", connection) { CommandType = CommandType.StoredProcedure })
                {
                    command.Parameters.Add("@MembershipId", SqlDbType.UniqueIdentifier).Value = lastLoginData.MembershipId;
                    command.Parameters.Add("@EmailCountSent", SqlDbType.Int).Value = lastLoginData.EmailCountSent;
                    command.Parameters.Add("@LastLogonDate", SqlDbType.DateTime).Value = lastLoginData.LastLogonDate;
                    command.Parameters.Add("@IgnoredUser", SqlDbType.Bit).Value = lastLoginData.IgnoredUser ? 1 : 0;

                    if (lastLoginData.FirstEmailSentAt.HasValue)
                        command.Parameters.Add("@FirstEmailSentAt", SqlDbType.DateTime).Value = lastLoginData.FirstEmailSentAt.Value;
                    else
                        command.Parameters.Add("@FirstEmailSentAt", SqlDbType.DateTime).Value = DBNull.Value;

                    connection.Open();

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
