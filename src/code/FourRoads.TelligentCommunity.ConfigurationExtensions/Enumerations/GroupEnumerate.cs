using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations
{
    public class GroupEnumerate : GenericEnumerate<Group>
    {
        public int? _groupId = null;

        public GroupEnumerate()
        {

        }

        public GroupEnumerate(int? groupId)
        {
            _groupId = groupId;
        }

        protected override PagedList<Group> NextPage(int pageIndex)
        {
            if (_groupId == null)
            {
                return Apis.Get<IGroups>().List(new GroupsListOptions() {PageIndex = pageIndex , GroupTypes = "Joinless"});
            }

            return new PagedList<Group>(new []{Apis.Get<IGroups>().Get(new GroupsGetOptions(){Id = _groupId})}, 1 ,0 , 1);
        }
    }
}