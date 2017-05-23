using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.UserDataExport
{
    public class UserExportJob : IEvolutionJob
    {
        private int _groupid;
        private bool _grouped = false;

        public void Execute(JobData jobData)
        {
            if (jobData.Data.ContainsKey("groupId"))
            {
                if (int.TryParse(jobData.Data["groupId"], out _groupid))
                {
                    _grouped = true;
                }
            }

            var fs = CentralizedFileStorage.GetFileStore(UserExportPlugin.FILESTORE_KEY);
            if (fs.GetFile("", "processing.txt") == null)
                return;

            try
            {
                StringBuilder resultCsv = new StringBuilder(100000);
                resultCsv.AppendLine(BuildHeader());

                bool moreRecords = true;

                if (_grouped)
                {
                    GroupUserMembersListOptions list = new GroupUserMembersListOptions()
                    {
                        PageIndex = 0,
                        PageSize = 100
                    };

                    while (moreRecords)
                    {
                        if (fs.GetFile("", "processing.txt") == null)
                            return;

                        var results = Apis.Get<GroupUserMembers>().List(_groupid ,list);
                        moreRecords = results.TotalCount > (++list.PageIndex * list.PageSize);

                        foreach (var groupUser in results)
                        {
                            resultCsv.AppendLine(ExtractUser(groupUser.User));
                        }
                    }
                }
                else
                {
                    UsersListOptions list = new UsersListOptions()
                    {
                        PageIndex = 0,
                        PageSize = 100,
                        IncludeHidden = true,
                        AccountStatus = "All"
                    };

                    while (moreRecords)
                    {
                        if (fs.GetFile("", "processing.txt") == null)
                            return;

                        var results = Apis.Get<IUsers>().List(list);
                        moreRecords = results.TotalCount > (++list.PageIndex*list.PageSize);

                        foreach (var user in results)
                        {
                            resultCsv.AppendLine(ExtractUser(user));
                        }
                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter wr = new StreamWriter(ms))
                    {
                        wr.Write(resultCsv);
                        wr.Flush();

                        ms.Seek(0, SeekOrigin.Begin);

                        fs.AddUpdateFile("", "results.csv", ms);
                    }
                }
            }
            catch (Exception ex)
            {
                Apis.Get<IEventLog>().Write("Error exporting users:" + ex,
                    new EventLogEntryWriteOptions() { Category = "User Export" });
            }
            finally
            {
                Apis.Get<IEventLog>().Write("Finished exporting users",
                    new EventLogEntryWriteOptions() { Category = "User Export" });

                fs.Delete("", "processing.txt");
            }
        }

        private string ExtractUser(User user)
        {
            List<string> elements = new List<string>
            {
                user.Username,
                user.DisplayName,
                user.PrivateEmail,
                user.PublicEmail,
                Apis.Get<ILanguage>().FormatDateAndTime(user.LastLoginDate.GetValueOrDefault(DateTime.MinValue)),
                user.Language,
                user.AccountStatus,
                user.AllowSitePartnersToContact.ToString(),
                user.AllowSiteToContact.ToString(),
                user.AvatarUrl,
                Apis.Get<ILanguage>().FormatDateAndTime(user.Birthday.GetValueOrDefault(DateTime.MinValue)),
                user.Bio(""),
                user.Location,
                user.TotalPosts.ToString()
            };
            
            var profileFields = user.ProfileFields.ToLookup(l => l.Label);
            foreach (var profileFeild in Apis.Get<IUserProfileFields>().List())
            {
                if (profileFields.Contains(profileFeild.Name))
                {
                    elements.Add(profileFields[profileFeild.Name].First().Value);
                }
                else
                {
                    elements.Add(string.Empty);
                }
            }

            return string.Join(",", elements.Select(Csv.Escape));
        }

        private string BuildHeader()
        {
            //Build the header
            List<string> elements = new List<string>
            {
                "UserName",
                "DisplayName",
                "Private Email",
                "Public Email",
                "LastLoginDate",
                "Language",
                "AccountStatus",
                "Allow Partners To Contact",
                "Allow Site To Contact",
                "Avatar URL",
                "Birthday",
                "Bio",
                "Location",
                "TotalPosts"
            };

            foreach (var profileField in Apis.Get<IUserProfileFields>().List())
            {
                elements.Add(profileField.Title);
            }

            return string.Join(",", elements.Select(Csv.Escape));
        }
    }

}