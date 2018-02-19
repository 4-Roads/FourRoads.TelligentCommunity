using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations
{
    public class GalleryEnumerate : GenericEnumerate<Gallery>
    {
        public int _groupId;
        public int? _galleryId;

        public GalleryEnumerate(int groupId, int? galleryId)
        {
            _groupId = groupId;
            _galleryId = galleryId;
        }

        protected override PagedList<Gallery> NextPage(int pageIndex)
        {
            if (_galleryId == null)
            {
                return Apis.Get<IGalleries>().List(new GalleriesListOptions() { PageIndex = pageIndex, GroupId = _groupId });
            }

            return new PagedList<Gallery>(new[] { Apis.Get<IGalleries>().Get(new GalleriesGetOptions() { Id = _galleryId.Value }) }, 1, 0, 1);
        }
    }
}