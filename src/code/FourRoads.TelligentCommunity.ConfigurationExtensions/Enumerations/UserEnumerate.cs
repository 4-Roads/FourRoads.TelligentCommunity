using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations
{
    public class UserEnumerate : GenericEnumerate<User>
    {
        public string _userName = string.Empty;
        public int? _groupId = null;

        public UserEnumerate(string userName , int? groupId)
        {
            _userName = userName;
            _groupId = groupId;
        }

        protected override PagedList<User> NextPage(int pageIndex)
        {
            if (_groupId.HasValue)
            {
                //if the group is joinless then all site members should be updated otherwise just the members
                if (PublicApi.Groups.Get(new GroupsGetOptions() {Id = _groupId}).GroupType.ToLower() != "joinless")
                {
                    var pageResult = PublicApi.GroupUserMembers.List(_groupId.Value, new GroupUserMembersListOptions() {IncludeRoleMembers = true, PageIndex = pageIndex});

                    return new PagedList<User>(pageResult.Select(gu => gu.User), pageResult.PageSize, pageResult.TotalCount);
                }
            }

            return PublicApi.Users.List(new UsersListOptions() { Usernames = _userName, PageIndex = pageIndex });
        }
    }
}