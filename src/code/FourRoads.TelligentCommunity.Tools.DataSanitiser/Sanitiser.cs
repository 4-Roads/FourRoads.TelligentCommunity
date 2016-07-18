using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using FourRoads.Common.TelligentCommunity.Components;
using log4net;
using Telligent.Common;
using Telligent.Evolution;
using Telligent.Evolution.Api.Services;
using Telligent.Evolution.Components;
using IUserService = Telligent.Evolution.Components.IUserService;

namespace FourRoads.TelligentCommunity.Tools.DataSanitiser
{
    public class Sainitizer
    {
        private const int PageSize = 100;
        private static Sainitizer _instance;

        public static Sainitizer Instance
        {
            get { return _instance ?? (_instance = new Sainitizer()); }
        }

        public ILog Log { get; private set; }

        public void Execute(ILog log)
        {
            Log = log;
            ServicesHelper.EnsureInitialized();
            SiteSettings settings = SetupContext();
            SiteSettingsManager.Save(settings);
            ProcessUsers("user_{0}");
            UpdateAspNetMembershipTable();
        }

        private static void UpdateAspNetMembershipTable()
        {
            string connectionString = DataProvider.GetConnectionString();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("update m set " +
                                                "m.LoweredEmail = CONCAT(u.UserName,'@localhost.local'), " +
                                                "m.Email = CONCAT(u.UserName,'@localhost.local') " +
                                                "from aspnet_Users u " +
                                                "inner join aspnet_Membership as m on u.UserId = m.UserId", conn);
                cmd.ExecuteNonQuery();
            }
        }

        private void ProcessUsers(string userNameFormat)
        {
            int pageIndex = 0;
            int totalCount;
            do
            {
                IUserService userService = Services.Get<IUserService>();

                UserSet coreUsers = userService.GetUsers(new UserQuery
                {
                    IncludeHiddenUsers = true,
                    PageSize = PageSize,
                    PageIndex = pageIndex,
                    SortBy = SortUsersBy.JoinedDate
                });

                if (coreUsers == null)
                {
                    Log.Error("Aborting !");
                    break;
                }

                totalCount = coreUsers.TotalRecords;
                Log.WarnFormat("Processing Batch {0} out of {1}", pageIndex + 1, totalCount / ((pageIndex + 1) * PageSize));

                foreach (Telligent.Evolution.Components.User user in coreUsers.Users)
                {
                    if (user.UserID < 2103)
                    {
                        continue;
                    }
                    var msg = string.Format("UserId: {0}, PrivateEmail: {1}, Username: {2}",
                        user.UserID,
                        user.Email,
                        user.Username);

                    try
                    {
                        Services.Get<IUserAvatarService>().DeleteUserAvatar(user.UserID);
                    }
                    catch (Exception exception)
                    {
                        Log.Error(string.Format("Could not delete avatar for user Id {0}", user.UserID), exception);
                    }

                    try
                    {
                        var writable = userService.GetUserWithWriteableProfile(user.UserID, user.Username);
                        writable.Email = string.Format("{0}@localhost.local", string.Format(userNameFormat, user.UserID));
                        writable.Password = "123456";
                        writable.ProfileFields = GetSanitizedProfileFieldsDictionary(user.ProfileFields);
                        writable.CommonName = string.Format("User {0}", user.UserID);

                        userService.UpdateUser(writable);
                        RenameUserStatus rus = userService.RenameUser(writable, string.Format(userNameFormat, user.UserID), true, true);
                        if (rus != RenameUserStatus.Success)
                        {
                            throw new TCException(CSExceptionType.UnknownError, "Could not update username: " + rus);
                        }
                        Log.Info(msg);
                    }
                    catch (Exception exception)
                    {
                        Log.Error(string.Format("Could not sanitize user Id {0}", user.UserID), exception);
                    }
                }
            } while (totalCount > (++pageIndex) * PageSize);
        }

        private SiteSettings SetupContext()
        {
            IExecutionContext context = Services.Get<IContextService>().GetExecutionContext();

            context.User = Services.Get<IUserService>().GetUser(2100);

            //disable email and wipe out some configuration details
            context.SiteSettings.EnableEmail = false;
            context.SiteSettings.SmtpPortNumber = "";
            context.SiteSettings.SmtpServer = "";
            context.SiteSettings.SmtpServerPassword = "";
            context.SiteSettings.SmtpServerUserName = "";
            context.SiteSettings.SmtpServerRequiredLogin = false;
            context.SiteSettings.UsernameMinLength = 2;
            context.SiteSettings.PasswordRegex = ".+";
            context.SiteSettings.NotificationFromEmailAddress = "local@localhost.local";
            context.SiteSettings.DomainName = "localhost.local";
            context.SiteSettings.EnableMailGateway = false;

            return context.SiteSettings;
        }

        private Dictionary<string, string> GetSanitizedProfileFieldsDictionary(Dictionary<string, string> profileFields)
        {
            var result = new Dictionary<string, string>();
            foreach (var key in profileFields.Keys)
            {
                string val;
                switch (key)
                {
                    case "core_Birthday":
                        val = "0001-01-01 00:00:00";
                        break;
                    case "core_Language":
                        val = "en-US";
                        break;
                    case "core_Gender":
                        val = "0";
                        break;
                    //case "core_Website":
                    //case "core_Location":
                    //case "core_Public Email":
                    default:
                        val = string.Empty;
                        break;
                }
                result.Add(key, val);
            }
            return result;
        }
    }
}