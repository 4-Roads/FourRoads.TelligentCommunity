using System.Linq;
using FourRoads.Common.TelligentCommunity.Components.Extensions;
using FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.VelocityExtensions;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Jobs
{
    public class SubscriptionUpdateJob : IEvolutionJob 
    {
        public void Execute(JobData jobData)
        {
            //NOT API Safe but no choice
            var forumsFrag = ((ForumsScriptedContentFragment)PluginManager.Get<ForumsScriptedContentFragmentExtension>().First().Extension);
            var blogsFrag = ((BlogsScriptedContentFragment)PluginManager.Get<BlogsScriptedContentFragmentExtension>().First().Extension);

            int? groupId = jobData.Data.ContainsKey("GroupId") ? int.Parse(jobData.Data["GroupId"]) : default(int?);

            UserEnumerate userEnumerator = new UserEnumerate(jobData.Data.ContainsKey("UserName") ? jobData.Data["UserName"] : null, groupId);

            GroupEnumerate groupEnumerate = new GroupEnumerate(groupId); 

            int? forumId  = jobData.Data.ContainsKey("ForumId") ?int.Parse(jobData.Data["ForumId"]) : default(int?);
            int? blogId = jobData.Data.ContainsKey("BlogId") ? int.Parse(jobData.Data["BlogId"]) : default(int?);

            bool processForums = jobData.Data.ContainsKey("processForums");
            bool processBlogs = jobData.Data.ContainsKey("processBlogs");

            foreach (User user in userEnumerator)
            {
                foreach (Group group in groupEnumerate)
                {
                    if (processForums)
                    {
                        ForumEnumerate forumEnumerate = new ForumEnumerate(group.Id.Value, forumId);

                        foreach (Forum forum in forumEnumerate)
                        {
                            var lookups = forum.ExtendedAttributes.ToLookup(attribute => attribute.Key , val => (IExtendedAttribute)val);

                            string setting = lookups.GetString("DefaultSubscriptionSetting", "unset");

                            if (setting.ToLower() == "subscribed")
                            {
                                PublicApi.Users.RunAsUser(user.Id.Value, () => 
                                {
                                    forumsFrag.SetSubscribed(forum.Id.Value, true);
                                });
                            }
                            else if (setting.ToLower() == "unsubscribed")
                            {
                                PublicApi.Users.RunAsUser(user.Id.Value, () =>
                                {
                                    forumsFrag.SetSubscribed(forum.Id.Value, false);
                                });
                            }
                        }
                    }

                    if (processBlogs)
                    {
                        BlogEnumerate blogEnumerate = new BlogEnumerate(group.Id.Value, blogId);

                        foreach (Blog blog in blogEnumerate)
                        {
                            var lookups = blog.ExtendedAttributes.ToLookup(attribute => attribute.Key, val => (IExtendedAttribute)val);

                            string setting = lookups.GetString("DefaultSubscriptionSetting", "unset");

                            if (setting.ToLower() == "subscribed")
                            {
                                PublicApi.Users.RunAsUser(user.Id.Value, () =>
                                {
                                    blogsFrag.SetSubscribed(blog.Id.Value, true);
                                });
                            }
                            else if (setting.ToLower() == "unsubscribed")
                            {
                                PublicApi.Users.RunAsUser(user.Id.Value, () =>
                                {
                                    blogsFrag.SetSubscribed(blog.Id.Value, false);
                                });
                            }
                        }   

                    }
                }
            }
        }
    }
}