using System;
using System.Diagnostics;
using System.Linq;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.MigratorFramework
{
    /// <summary>
    /// Updates the former member so that content can be assigned to it
    /// </summary>
    public class FormerMember : IDisposable
    {
        private readonly string _formerMemberName;
        private readonly IMigrationRepository _repository;

        public FormerMember(IMigrationRepository repository)
        {
            _formerMemberName = Apis.Get<IUsers>().FormerMemberName;

            Apis.Get<IUsers>().Update(new UsersUpdateOptions() {Username = _formerMemberName, AccountStatus = "Approved", ModerationLevel = "Unmoderated" });

            Apis.Get<IRoleUsers>().AddUserToRole(new RoleUserCreateOptions() {RoleName = "Registered Users", UserName = _formerMemberName});

            _repository = repository;

            _repository.CreateLogEntry($"User Name:{_formerMemberName} has been added to the Registered Users Role and Unmoderated" , EventLogEntryType.Information);
        }

        public void Dispose()
        {
            Apis.Get<IUsers>().Update(new UsersUpdateOptions() { Username = Apis.Get<IUsers>().FormerMemberName, AccountStatus = "Disapproved", ModerationLevel = "Moderated" });

            var id = Apis.Get<IRoles>().Find("Registered Users").FirstOrDefault().Id;

            Apis.Get<IRoleUsers>().RemoveUserFromRole(id.Value, new RoleUserDeleteOptions(){UserName = _formerMemberName});

            _repository.CreateLogEntry($"User Name:{_formerMemberName} has been removed the Registered Users Role and set moderated", EventLogEntryType.Information);
        }
    }
}