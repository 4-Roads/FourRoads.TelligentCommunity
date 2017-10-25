using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations
{
    public class BlogEnumerate : GenericEnumerate<Blog>
    {
        public int _groupId;
        public int? _blogId;

        public BlogEnumerate(int groupId, int? blogId)
        {
            _groupId = groupId;
            _blogId = blogId;
        }

        protected override PagedList<Blog> NextPage(int pageIndex)
        {
            if (_blogId == null)
            {
                return Apis.Get<IBlogs>().List(new BlogsListOptions() { PageIndex = pageIndex, GroupId = _groupId });
            }

            return new PagedList<Blog>(new[] { Apis.Get<IBlogs>().Get(new BlogsGetOptions() { Id = _blogId.Value }) }, 1, 0, 1);
        }
    }
}