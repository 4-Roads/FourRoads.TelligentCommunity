using System.Collections;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.Common.TelligentCommunity.Services.Interfaces
{
    public interface IUserRolesService
    {
        /// <summary>
        /// Gets a user
        /// </summary>
        /// <param name="userId">A user ID</param>
        /// <returns>The requested user if it exists, or the accessing user if it doesn't</returns>
        int GetUserOrCurrent(int? userId = null);

        /// <summary>
        /// Gets a set roles for a user
        /// </summary>
        /// <param name="userId">A user ID</param>
        /// <returns>A set of roles</returns>
        IEnumerable<Role> GetRoles(int? userId = null);

        /// <summary>
        /// Gets a set of roles
        /// </summary>
        /// <param name="roles">A comma-delimited string of role names</param>
        /// <returns>A set of roles</returns>
        IEnumerable<Role> GetRoles(string roles);

        /// <summary>
        /// Gets the roles based on a search
        /// </summary>
        /// <param name="searchResult">A search result</param>
        /// <returns>A set of roles</returns>
        IEnumerable<Role> GetRoles(SearchResult searchResult);

        /// <summary>
        /// Tests if a user is a member of any of the given roles
        /// </summary>
        /// <param name="roles">A set of roles</param>
        /// <param name="userId">A user ID</param>
        /// <returns>true if the user is a member of any of the given roles, otherwise, false</returns>
        bool IsMember(IEnumerable<int> roles, int? userId = null);

        /// <summary>
        /// Lists the members of a role
        /// </summary>
        /// <param name="roleName">The role name</param>
        /// <param name="options">The filter options</param>
        /// <returns>A paged list of users</returns>
        PagedList<User> ListMembers(string roleName, IDictionary options);

        /// <summary>
        /// Lists the members of a role
        /// </summary>
        /// <param name="roleId">The role ID</param>
        /// <param name="options">The filter options</param>
        /// <returns>A paged list of users</returns>
        PagedList<User> ListMembers(int roleId, IDictionary options);

        /// <summary>
        /// Ensures a role exists
        /// </summary>
        /// <param name="rolename">The role name</param>
        /// <returns>The named role</returns>
        Role EnsureRole(string rolename);

        /// <summary>
        /// Removes a user from a role
        /// </summary>
        /// <param name="user">The user to be removed</param>
        /// <param name="role">The role to be removed from</param>
        void RemoveUser(User user, Role role);

        /// <summary>
        /// Adds a user to a role
        /// </summary>
        /// <param name="user">The user to be added</param>
        /// <param name="role">The role to be added to</param>
        void AddUser(User user, Role role);
    }
}
