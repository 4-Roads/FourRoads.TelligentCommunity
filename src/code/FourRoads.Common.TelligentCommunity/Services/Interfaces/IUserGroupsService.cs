using System.Collections.Specialized;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.Common.TelligentCommunity.Services.Interfaces
{
    public interface IUserGroupsService : IService
    {
        /// <summary>
        /// Gets a group
        /// </summary>
        /// <param name="groupId">A group ID</param>
        /// <returns>The requested group if it exists, or the root group if it doesn't</returns>
        int GetGroupOrRoot(int? groupId = null);

        /// <summary>
        /// Gets a group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="parentId">The parent group ID</param>
        /// <returns>The group if it exists, or <c>null</c> if it doesn't</returns>
        Group GetGroup(string groupName, int? parentId = null);

        /// <summary>
        /// Creates a group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="groupDescription">The group description</param>
        /// <param name="groupType">The group type</param>
        /// <param name="configuration">A set of configuration values</param>
        /// <param name="createApps">Whether applications should be created within the group or not</param>
        /// <param name="parentId">The parent group ID</param>
        /// <returns>The newly-created group</returns>
        Group CreateGroup(string groupName, string groupDescription, string groupType, NameValueCollection configuration, bool createApps, int? parentId = null);

        /// <summary>
        /// Removes a user from a group
        /// </summary>
        /// <param name="user">The user to be removed</param>
        /// <param name="group">The group to be removed from</param>
        void RemoveUser(User user, Group group);

        /// <summary>
        /// Adds a user to a group
        /// </summary>
        /// <param name="user">The user to be added</param>
        /// <param name="group">The group to be added to</param>
        void AddUser(User user, Group group);

        /// <summary>
        /// Lists the groups a user is a member of
        /// </summary>
        /// <param name="user">The user to be queried</param>
        /// <returns>A <c>PagedList</c> of groups</returns>
        PagedList<Group> ListMemberGroups(User user);

        /// <summary>
        /// Tests if a user is a member of a group
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="group">The group</param>
        /// <returns><c>true</c> if <paramref name="user"/> is a member of <paramref name="group"/>, otherwise, <c>false</c></returns>
        bool IsMember(User user, Group group);
    }
}
