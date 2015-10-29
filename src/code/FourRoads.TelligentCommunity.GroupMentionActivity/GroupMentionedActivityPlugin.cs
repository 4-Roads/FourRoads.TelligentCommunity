using System;
using System.Collections.Generic;
using System.Linq;
using FourRoads.Common.TelligentCommunity.Components.Extensions;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.GroupMentionActivity
{
    public class GroupMentionedActivityPlugin : IActivityStoryType, ITranslatablePlugin 
    {
        public static Guid StoryTypeIdGuid = new Guid("{1B11621B-3EFE-4014-9B32-8A9AFDD70CD6}");
        private IActivityStoryController _controller;
        private ITranslatablePluginController _translatablePluginController;
        private Translation[] _translations;

        public void Initialize()
        {
            PublicApi.Mentions.Events.AfterCreate += EventsOnAfterCreate;
        }

        private void EventsOnAfterCreate(MentionAfterCreateEventArgs maca)
        {
            if (maca.MentionedContentTypeId == ContentTypeIds[0])
            {
                // Special case if this is an activity feed story mention then change it to the group.
                Guid mentioningId = maca.MentioningContentId;
                Guid mentioningTypeId = maca.MentioningContentTypeId;

                if (mentioningTypeId == Telligent.Evolution.Components.ContentTypes.StatusMessage)
                {
                    var story = PublicApi.ActivityStories.Get(mentioningId);

                    if (story != null && story.Errors.Count == 0)
                    {
                        mentioningId = story.Content.Application.Container.ContainerId;
                        mentioningTypeId = story.Content.Application.Container.ContainerTypeId;
                    }
                }

                _controller.Create(new ActivityStoryCreateOptions
                {
                    ContentId = maca.MentionedContentId,
                    ContentTypeId = maca.MentionedContentTypeId,
                    LastUpdate = DateTime.UtcNow,
                    Actors = new List<Telligent.Evolution.Extensibility.Content.Version1.ActivityStoryActor>()
                    {
                        new Telligent.Evolution.Extensibility.Content.Version1.ActivityStoryActor()
                        {
                            UserId = maca.MentioningUserId,
                            Verb = "Mention",
                            Date = DateTime.UtcNow
                        }
                    },
                    ExtendedAttributes = new IExtendedAttribute[]
                    {
                        new ExtendedAttribute(){Key = "MentioningContentId" , Value =mentioningId.ToString() },
                        new ExtendedAttribute(){Key = "MentioningContentTypeId" , Value =mentioningTypeId.ToString() }
                    }
                });
            }

        }

        public string Name {
            get { return "4 Roads - Group Mentioned Activity Story"; }
        }
        public string Description {
            get { return "Activity story that displays a message in the group when it is mentioned elsewhere"; }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translatablePluginController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                if (_translations == null)
                {
                    Translation translation = new Translation("en-us");
                    translation.Set("story_name", "Group Mentioned Story");
                    translation.Set("story_description", "Stories associated with thisn group being mentioned");
                    translation.Set("GroupMentioned", "<span class=\"user-name\"><a class=\"internal-link view-user-profile activity-summary-user\" href=\"{0}\">{1}</a></span> mentioned this group in <a href=\"{2}\">{3}</a>.");
                    _translations = new Translation[]
                        {
                            translation
                        };
                }
                return _translations;
            }
        }


        public void SetController(IActivityStoryController controller)
        {
            _controller = controller;
        }


        public bool CanDeleteStory(Guid storyId, int userId)
        {
            var story = PublicApi.ActivityStories.Get(storyId);

            if (story != null)
            {
                var group = PublicApi.Groups.Get(story.ContentId.Value);

                if (group != null && group.Errors.Count() == 0)
                {
                    if (PublicApi.GroupUserMembers.List(group.Id.Value, new GroupUserMembersListOptions() {MembershipType = "Owner,Manager", IncludeRoleMembers = true, PageSize = 1, UserId = userId}).TotalCount > 0)
                    {
                        return true;
                    }

                }
            }

            return false;
        }

        public int? GetPrimaryUser(IActivityStory story)
        {
            var user = story.Actors.FirstOrDefault();

            if (user != null)
            {
                return user.UserId;
            }

            return null;
        }

        public string GetPreviewHtml(IActivityStory story, Target target)
        {
            return RenderActivity(story);
        }

        private string RenderActivity(IActivityStory story)
        {
            var group = PublicApi.Groups.Get(story.ContentId.Value);

            if (group != null && group.Errors.Count() == 0)
            {
                User user = PublicApi.Users.Get(new UsersGetOptions() { Id = GetPrimaryUser(story) });

                if (user != null)
                {
                    var attributes = story.ExtendedAttributes.ToLookup(att => att.Key);

                    Guid contentId = attributes.GetGuid("MentioningContentId" , Guid.Empty);
                    Guid contentTypeId = attributes.GetGuid("MentioningContentTypeId", Guid.Empty);

                    var content  = PublicApi.Content.Get(contentId, contentTypeId);

                    if (content != null && content.Errors.Count == 0)
                    {

                        return string.Format(_translatablePluginController.GetLanguageResourceValue("GroupMentioned"),  PublicApi.Html.EncodeAttribute(user.Url) , user.DisplayName,
                            PublicApi.Html.EncodeAttribute(content.Url), content.HtmlName("web"));
                    }
                }

            }
            return string.Empty;
        }

        public string GetViewHtml(IActivityStory story, Target target)
        {
            return RenderActivity(story);
        }

        public string StoryTypeName
        {
            get { return _translatablePluginController.GetLanguageResourceValue("story_name"); }
        }
        public string StoryTypeDescription
        {
            get { return _translatablePluginController.GetLanguageResourceValue("story_description"); }
        }
        public Guid StoryTypeId
        {
            get { return StoryTypeIdGuid; }
        }
        public Guid[] ContentTypeIds
        {
            get
            {
                //Not API safe
                return new Guid[]{
                    Telligent.Evolution.Components.ContentTypes.Group
                };
            }
        }
        public bool IsCacheable
        {
            get { return true; }
        }
        public bool VaryCacheByUser
        {
            get { return false; }
        }
    }
}
