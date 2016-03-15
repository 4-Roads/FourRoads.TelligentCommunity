using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.UserDataExport
{
    public class UserExportJob : IEvolutionJob
    {
        public void Execute(JobData jobData)
        {
            var fs = CentralizedFileStorage.GetFileStore(UserExportPlugin.FILESTORE_KEY);

            if (fs.GetFile("", "processing.txt") == null)
                return;

            try
            {
                UsersListOptions list = new UsersListOptions()
                {
                    PageIndex = 0,
                    PageSize = 100,
                    IncludeHidden = true,
                    AccountStatus = "All"
                };

                StringBuilder resultCsv = new StringBuilder(100000);

                bool moreRecords = true;

                //Build the header
                List<string> elements = new List<string>();
                elements.Add("UserName");
                elements.Add("DisplayName");
                elements.Add("Account Email");
                elements.Add("LastLoginDate");
                elements.Add("Language");
                elements.Add("AccountStatus");
                elements.Add("Allow Partners To Contact");
                elements.Add("Allow Site To Contact");
                elements.Add("Avatar URL");
                elements.Add("Birthday");
                elements.Add("Bio");
                elements.Add("Location");
                elements.Add("TotalPosts");

                foreach (var profileFeild in PublicApi.UserProfileFields.List())
                {
                    elements.Add(profileFeild.Title);
                }

                resultCsv.AppendLine(string.Join(",", elements.Select(Csv.Escape)));

                while (moreRecords)
                {
                    var results = PublicApi.Users.List(list);

                    moreRecords = results.TotalCount > (++list.PageIndex*list.PageSize);

                    foreach (var user in results)
                    {
                        elements.Clear();

                        elements.Add(user.Username);
                        elements.Add(user.DisplayName);
                        elements.Add(user.PrivateEmail);
                        elements.Add(PublicApi.Language.FormatDateAndTime(user.LastLoginDate.GetValueOrDefault(DateTime.MinValue)));
                        elements.Add(user.Language);
                        elements.Add(user.AccountStatus);
                        elements.Add(user.AllowSitePartnersToContact.ToString());
                        elements.Add(user.AllowSiteToContact.ToString());
                        elements.Add(user.AvatarUrl);
                        elements.Add(PublicApi.Language.FormatDateAndTime(user.Birthday.GetValueOrDefault(DateTime.MinValue)));
                        elements.Add(user.Bio(""));
                        elements.Add(user.Location);
                        elements.Add(user.TotalPosts.ToString());

                        var profileFeilds = user.ProfileFields.ToLookup(l => l.Label);

                        foreach (var profileFeild in PublicApi.UserProfileFields.List())
                        {
                            if (profileFeilds.Contains(profileFeild.Name))
                            {
                                elements.Add(profileFeilds[profileFeild.Name].First().Value);
                            }
                            else
                            {
                                elements.Add(string.Empty);
                            }
                        }

                        resultCsv.AppendLine(string.Join(",", elements.Select(Csv.Escape)));
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
                PublicApi.Eventlogs.Write("Error exporting users:" + ex, new EventLogEntryWriteOptions() { Category = "User Export" });
            }
            finally
            {
                PublicApi.Eventlogs.Write("Finished exporting users", new EventLogEntryWriteOptions() {Category = "User Export"});

                fs.Delete("", "processing.txt");
            }
        }

    }
}