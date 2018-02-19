using FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations;
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

namespace FourRoads.TelligentCommunity.GroupDataExport
{
    public class GroupExportJob : IEvolutionJob
    {
        private int _groupid;
        private bool _grouped = false;
        private bool _summary = false;

        public void Execute(JobData jobData)
        {
            if (jobData.Data.ContainsKey("groupId"))
            {
                if (int.TryParse(jobData.Data["groupId"], out _groupid))
                {
                    _grouped = true;
                }
            }

            if (jobData.Data.ContainsKey("summary"))
            {
                bool.TryParse(jobData.Data["summary"], out _summary);
            }

            var fs = CentralizedFileStorage.GetFileStore(GroupExportPlugin.FILESTORE_KEY);
            if (fs.GetFile("", "processing.txt") == null)
                return;

            try
            {
                StringBuilder resultCsv = new StringBuilder(100000);
                resultCsv.AppendLine(BuildHeader());

                if (_grouped)
                {
                    // single group
                    var group = Apis.Get<IGroups>().Get(new GroupsGetOptions() { Id = _groupid });

                    if (!group.HasErrors())
                    {
                        var details = ExtractGroup(group);
                        details.ForEach(line => resultCsv.AppendLine(line));
                    }
                }
                else
                {
                    // all groups with users 
                    GroupsListOptions grouplist = new GroupsListOptions()
                    {
                        PageIndex = 0,
                        PageSize = 100
                    };

                    bool moreGroups = true;

                    while (moreGroups)
                    {
                        var groups = Apis.Get<IGroups>().List(grouplist);
                        moreGroups = groups.TotalCount > (++grouplist.PageIndex * grouplist.PageSize);

                        if (!groups.HasErrors())
                        {
                            foreach (var group in groups)
                            {
                                if ((group.TotalMembers ?? 0) > 0)
                                {
                                    var details = ExtractGroup(group);
                                    details.ForEach(line => resultCsv.AppendLine(line));
                                }
                            }
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
                Apis.Get<IEventLog>().Write("Error exporting groups:" + ex,
                    new EventLogEntryWriteOptions() { Category = "Group Export" });
            }
            finally
            {
                Apis.Get<IEventLog>().Write("Finished exporting groups",
                    new EventLogEntryWriteOptions() { Category = "Group Export" });

                fs.Delete("", "processing.txt");
            }
        }

        private List<string> ExtractGroup(Group group)
        {
            List<string> details = new List<string>();

            var fs = CentralizedFileStorage.GetFileStore(GroupExportPlugin.FILESTORE_KEY);
            bool moreRecords = true;
            bool firstTime = true;

            GroupUserMembersListOptions list = new GroupUserMembersListOptions()
            {
                PageIndex = 0,
                PageSize = 100
            };

            DateTime latestPostDateTime = GetGroupLatestUpdate(group.Id.Value);

            while (moreRecords)
            {
                if (fs.GetFile("", "processing.txt") == null)
                    return details;

                var results = Apis.Get<IGroupUserMembers>().List(group.Id.Value, list);
                moreRecords = results.TotalCount > (++list.PageIndex * list.PageSize);

                foreach (var groupUser in results)
                {
                    if (firstTime)
                    {
                        details.Add(ExtractGroup(groupUser.Group, latestPostDateTime));
                        firstTime = false;
                        if (_summary)
                        {
                            moreRecords = false;
                            break;
                        }
                    }

                    details.Add(ExtractGroupUser(groupUser, latestPostDateTime));
                }
            }

            return details;
        }

        private DateTime GetGroupLatestUpdate(int groupId)
        {
            DateTime latestPostDateTime = DateTime.MinValue;

            BlogEnumerate blogEnumerate = new BlogEnumerate(groupId, null);
            foreach (Blog blog in blogEnumerate)
            {
                if (blog.LatestPostDate != null && blog.LatestPostDate.Value > latestPostDateTime)
                {
                    latestPostDateTime = blog.LatestPostDate.Value;
                }
            }

            ForumEnumerate forumEnumerate = new ForumEnumerate(groupId, null);
            foreach (Forum forum in forumEnumerate)
            {
                if (forum.LatestPostDate != null && forum.LatestPostDate.Value > latestPostDateTime)
                {
                    latestPostDateTime = forum.LatestPostDate.Value;
                }
            }

            GalleryEnumerate galleryEnumerate = new GalleryEnumerate(groupId, null);
            foreach (Gallery gallery in galleryEnumerate)
            {
                if (gallery.LatestPostDate != null && gallery.LatestPostDate.Value > latestPostDateTime)
                {
                    latestPostDateTime = gallery.LatestPostDate.Value;
                }
            }
            
            return latestPostDateTime;

        }

        private string ExtractGroupUser(GroupUser groupUser, DateTime latestPostDateTime)
        {
            
            List<string> elements = new List<string>
            {
                groupUser.Group.Name,
                groupUser.Group.Description,
                Apis.Get<ILanguage>().FormatDateAndTime(groupUser.Group.DateCreated.GetValueOrDefault(DateTime.MinValue)),
                Apis.Get<ILanguage>().FormatDateAndTime(latestPostDateTime),
                groupUser.User.Username,
                groupUser.MembershipType.Contains("Owner") ? "Yes": "",
                groupUser.MembershipType.Contains("Manager") ? "Yes": "",
                groupUser.MembershipType,
                groupUser.User.DisplayName,
                groupUser.User.PrivateEmail,
                groupUser.User.PublicEmail,
                Apis.Get<ILanguage>().FormatDateAndTime(groupUser.User.LastLoginDate.GetValueOrDefault(DateTime.MinValue))
            };

            return string.Join(",", elements.Select(Csv.Escape));
        }

        private string ExtractGroup(Group group, DateTime latestPostDateTime)
        {

            List<string> elements = new List<string>
            {
                group.Name,
                group.Description,
                Apis.Get<ILanguage>().FormatDateAndTime(group.DateCreated.GetValueOrDefault(DateTime.MinValue)),
                Apis.Get<ILanguage>().FormatDateAndTime(latestPostDateTime),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            return string.Join(",", elements.Select(Csv.Escape));
        }


        private string BuildHeader()
        {
            //Build the header
            List<string> elements = new List<string>
            {
                "GroupName",
                "GroupDescription",
                "GroupCreated",
                "GroupLastActivity",
                "UserName",
                "GroupOwner",
                "GroupManager",
                "GroupMembershipType",
                "DisplayName",
                "Private Email",
                "Public Email",
                "LastLoginDate"
            };

            return string.Join(",", elements.Select(Csv.Escape));
        }
    }

}