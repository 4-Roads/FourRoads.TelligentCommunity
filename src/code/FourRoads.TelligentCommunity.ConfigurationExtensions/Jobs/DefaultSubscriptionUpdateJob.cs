using System.Linq;
using FourRoads.Common.TelligentCommunity.Components.Extensions;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.Calendar.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.Calendar.Extensibility.Api.Version1;
using Telligent.Evolution.VelocityExtensions;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Jobs
{
    public class SubscriptionUpdateJob : IEvolutionJob 
    {
        public void Execute(JobData jobData)
        {
            int? groupId = jobData.Data.ContainsKey("GroupId") ? int.Parse(jobData.Data["GroupId"]) : default(int?);

            UserEnumerate userEnumerator = new UserEnumerate(jobData.Data.ContainsKey("UserName") ? jobData.Data["UserName"] : null, groupId);

            GroupEnumerate groupEnumerate = new GroupEnumerate(groupId); 

            int? forumId  = jobData.Data.ContainsKey("ForumId") ?int.Parse(jobData.Data["ForumId"]) : default(int?);
            int? blogId = jobData.Data.ContainsKey("BlogId") ? int.Parse(jobData.Data["BlogId"]) : default(int?);
            int? calendarId = jobData.Data.ContainsKey("CalendarId") ? int.Parse(jobData.Data["CalendarId"]) : default(int?);

            foreach (User user in userEnumerator)
            {
                foreach (Group group in groupEnumerate)
                {
                    ProcessForums(jobData.Data.ContainsKey("processForums"), @group, forumId, user);

                    ProcessBlogs(jobData.Data.ContainsKey("processBlogs"), @group, blogId, user);

                    ProcessCalendars(jobData.Data.ContainsKey("processCalendars"), @group, calendarId, user);
                }
            }
        }

        private void ProcessCalendars(bool processCalendars, Group @group, int? calendarId, User user)
        {
            if (processCalendars)
            {
                CalendarEnumerate calendarEnumerate = new CalendarEnumerate(@group.Id.Value, calendarId);

                foreach (Calendar calendar in calendarEnumerate)
                {
                    //Because calendars don't support extended attributes we need to store them on the group
                    var lookups = calendar.Group.ExtendedAttributes.ToLookup(attribute => attribute.Key, val => (IExtendedAttribute)val);

                    string setting = lookups.GetString("DefaultSubscriptionSetting" + calendar.NodeId, "unset");

                    if (setting.ToLower() == "subscribed")
                    {
                        PublicApi.Users.RunAsUser(user.Id.Value, () => { Telligent.Evolution.Extensions.Calendar.Api.PublicApi.CalendarSubscriptions.Subscribe(user.Id.Value, calendar.Id.Value); });
                    }
                    else if (setting.ToLower() == "unsubscribed")
                    {
                        PublicApi.Users.RunAsUser(user.Id.Value, () => { Telligent.Evolution.Extensions.Calendar.Api.PublicApi.CalendarSubscriptions.Unsubscribe(user.Id.Value, calendar.Id.Value); });
                    }
                }
            }
        }

        private void ProcessBlogs(bool processBlogs, Group @group, int? blogId, User user)
        {
            //NOT API Safe but no choice
            var blogsFrag = ((BlogsScriptedContentFragment)PluginManager.Get<BlogsScriptedContentFragmentExtension>().First().Extension);

            if (processBlogs)
            {
                BlogEnumerate blogEnumerate = new BlogEnumerate(@group.Id.Value, blogId);

                foreach (Blog blog in blogEnumerate)
                {
                    var lookups = blog.ExtendedAttributes.ToLookup(attribute => attribute.Key, val => (IExtendedAttribute) val);

                    string setting = lookups.GetString("DefaultSubscriptionSetting", "unset");

                    if (setting.ToLower() == "subscribed")
                    {
                        PublicApi.Users.RunAsUser(user.Id.Value, () => { blogsFrag.SetSubscribed(blog.Id.Value, true); });
                    }
                    else if (setting.ToLower() == "unsubscribed")
                    {
                        PublicApi.Users.RunAsUser(user.Id.Value, () => { blogsFrag.SetSubscribed(blog.Id.Value, false); });
                    }
                }
            }
        }

        private void ProcessForums(bool processForums, Group @group, int? forumId, User user)
        {
            var forumsFrag = ((ForumsScriptedContentFragment)PluginManager.Get<ForumsScriptedContentFragmentExtension>().First().Extension);

            if (processForums)
            {
                ForumEnumerate forumEnumerate = new ForumEnumerate(@group.Id.Value, forumId);

                foreach (Forum forum in forumEnumerate)
                {
                    var lookups = forum.ExtendedAttributes.ToLookup(attribute => attribute.Key, val => (IExtendedAttribute) val);

                    string setting = lookups.GetString("DefaultSubscriptionSetting", "unset");

                    if (setting.ToLower() == "subscribed")
                    {
                        PublicApi.Users.RunAsUser(user.Id.Value, () => { forumsFrag.SetSubscribed(forum.Id.Value, true); });
                    }
                    else if (setting.ToLower() == "unsubscribed")
                    {
                        PublicApi.Users.RunAsUser(user.Id.Value, () => { forumsFrag.SetSubscribed(forum.Id.Value, false); });
                    }
                }
            }
        }
    }
}