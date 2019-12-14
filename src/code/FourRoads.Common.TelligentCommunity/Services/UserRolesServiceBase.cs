using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.Common.TelligentCommunity.Services
{
    public class UserRolesServiceBase : IUserRolesService
    {
        protected readonly IEventLog _eventLog;
        protected readonly IProcess _process;
        protected readonly IRoles _rolesService;
        protected readonly IUsers _users;
        protected readonly IRoleUsers _userRoleService;

        public UserRolesServiceBase(IUsers users,
            IEventLog eventLog,
            IProcess process,
            IRoles rolesService,
            IRoleUsers roleUsersService)
        {
            _users = users;
            _eventLog = eventLog;
            _process = process;
            _rolesService = rolesService;
            _userRoleService = roleUsersService;
        }

        public int GetUserOrCurrent(int? userId = null)
        {
            if (!userId.HasValue)
            {
                userId = _users.AccessingUser.Id;
            }
            else
            {
                User user = _users.Get(new UsersGetOptions() { Id = userId });

                if (user == null || user.HasErrors())
                    throw new TCException($"Invalid User Account Specified {userId}");

            }
            return userId.Value;
        }
        
        public IEnumerable<Role> GetRoles(string roles)
        {
            if (roles == null || string.IsNullOrWhiteSpace(roles))
            {
                return null;
            }

            var roleIds = new HashSet<int>(roles.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(r => Convert.ToInt32(r)));

            return roleIds.Select(s => _rolesService.Get(s));
        }

        public IEnumerable<Role> GetRoles(SearchResult searchResult)
        {
            if (searchResult == null || searchResult.SearchFields.Count == 0)
            {
                return null;
            }

            var roleIds = searchResult.SearchFields.Where(f => f.Name == "roles").Select(s => Convert.ToInt32(s.Value));

            return roleIds.Select(s => _rolesService.Get(s));
        }
        
        public IEnumerable<Role> GetRoles(int? userId = null)
        {
            var user = _users.Get(new UsersGetOptions() { Id = GetUserOrCurrent(userId) });

            return _rolesService.List(new RolesListOptions() { UserId = user.Id, Include = "user", PageIndex = 0, PageSize = 1000 })
                .OrderBy(r => r.Name);
        }

        public bool IsMember(IEnumerable<int> roles, int? userId = null)
        {
            if (roles == null || !roles.Any())
            {
                return false;
            }

            var user = _users.Get(new UsersGetOptions() { Id = GetUserOrCurrent(userId) });

            var userRoles = GetRoles(user.Id).Select(r => r.Id);
            if (userRoles == null || !userRoles.Any())
            {
                return false;
            }

            return roles.Any(id => userRoles.Contains(id));
        }

        public PagedList<User> ListMembers(string roleName, IDictionary options)
        {
            Role role = _rolesService.Find(roleName).FirstOrDefault(r => r.Name == roleName);

            if (role == null || role.HasErrors())
            {
                return null;
            }

            return ListMembers(role.Id.Value, options);
        }
        
        public PagedList<User> ListMembers(int roleId, IDictionary options)
        {
            PagedList<User> users = new PagedList<User>();
            int pageIndex = 0;
            int pageSize = 1000;

            if (options["PageSize"] != null)
            {
                pageSize = Convert.ToInt32(options["PageSize"]);
            }

            if (options["PageIndex"] != null)
            {
                pageIndex = Convert.ToInt32(options["PageIndex"]);
            }

            users = _users.List(new UsersListOptions()
            {
                RoleId = roleId,
                PageSize = pageSize,
                PageIndex = pageIndex,
                IncludeHidden = true
            });

            return users;
        }

        public Role EnsureRole(string roleName)
        {
            Role role = null;

            roleName = roleName.Trim();

            _users.RunAsUser("admin", () =>
            {
                var description = $"Member of the {roleName} AD group";

                // do we have a role if not can we create it
                role = _rolesService.Find(roleName).FirstOrDefault(r => r.Name == roleName);
                if (role == null || role.HasErrors())
                {

                    role = _rolesService.Create(roleName, description);
                    if (role != null && !role.HasErrors())
                    {
                        _eventLog.Write($"Added role {roleName}", new EventLogEntryWriteOptions() { Category = "Connect", EventType = "Information" });
                    }
                }
            });

            return role;
        }

        public void AddUser(User user, Role role)
        {
            _users.RunAsUser("admin", () =>
            {
                if (!_userRoleService.IsUserInRoles(user.Username, new string[] { role.Name }))
                {
                    // Silenty add the user to the role
                    _process.RunProcessWithDisabledActivityStories(() =>
                    {
                        _process.RunProcessWithDisabledNotifications(() =>
                        {
                            var newMembership = _userRoleService.AddUserToRole(new RoleUserCreateOptions()
                            {
                                UserId = user.Id.Value,
                                RoleId = role.Id.Value
                            });
                        });
                    });
                    // removed success check from here as sometimes gave false null results even when the role was assigned......
                    _eventLog.Write($"Added userid {user.Id} to role {role.Name}", new EventLogEntryWriteOptions() { Category = "Connect", EventType = "Information" });
                }
            });
        }

        public void RemoveUser(User user, Role role)
        {
            _users.RunAsUser("admin", () =>
            {
                if (_userRoleService.IsUserInRoles(user.Username, new string[] { role.Name }))
                {
                    // Silenty remove the user from the role
                    _process.RunProcessWithDisabledActivityStories(() =>
                    {
                        _process.RunProcessWithDisabledNotifications(() =>
                        {
                            var oldMembership = _userRoleService.RemoveUserFromRole(role.Id.Value, new RoleUserDeleteOptions()
                            {
                                UserId = user.Id.Value
                            });
                        });
                    });
                    // removed success check from here as sometimes gave false null results even when the role was removed......
                    _eventLog.Write($"Removed userid {user.Id} from role {role.Name}", new EventLogEntryWriteOptions() { Category = "Connect", EventType = "Information" });
                }
            });
        }
    }
}
