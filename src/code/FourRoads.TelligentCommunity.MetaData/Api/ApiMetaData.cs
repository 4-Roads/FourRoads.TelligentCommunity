using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.MetaData.Api
{
    public class ApiMetaData : ApiEntity
    {
        public ApiMetaData(Logic.MetaData metaData)
        {
            Title = metaData.Title;
            Description = metaData.Description;
            Keywords = metaData.Keywords;
            ExtendedMetaTags = metaData.ExtendedMetaTags.ToArray();
        }

        public ApiMetaData(AdditionalInfo additionalInfo):base(additionalInfo) { }
        public ApiMetaData(IList<Warning> warnings, IList<Error> errors) : base(warnings, errors) { }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public KeyValuePair<string,string>[] ExtendedMetaTags { get; set; }
    }
}