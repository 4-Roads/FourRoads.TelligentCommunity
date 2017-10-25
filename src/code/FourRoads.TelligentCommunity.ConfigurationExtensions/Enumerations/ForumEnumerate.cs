using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations
{
    public class ForumEnumerate : GenericEnumerate<Forum>
    {
        public int _groupId;
        public int? _forumId;

        public ForumEnumerate(int groupId , int? forumId)
        {
            _groupId = groupId;
            _forumId = forumId;
        }

        protected override PagedList<Forum> NextPage(int pageIndex)
        {
            if (_forumId == null)
            {
                return Apis.Get<IForums>().List(new ForumsListOptions() { PageIndex = pageIndex, GroupId = _groupId });
            }

            return new PagedList<Forum>(new[] { Apis.Get<IForums>().Get(_forumId.Value) }, 1, 0 , 1);
        }
    }
}