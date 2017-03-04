using FourRoads.TelligentCommunity.MetaData.Interfaces;
using FourRoads.TelligentCommunity.MetaData.Rest.Entities;
using System;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using Telligent.Evolution.Extensibility.Rest.Version2;

namespace FourRoads.TelligentCommunity.MetaData.Rest
{
    public class MetaDataRestEndpoints : IRestEndpoints, IApplicationPlugin
    {
        private IMetaDataLogic _logic;

        public MetaDataRestEndpoints()
        {
            _logic = Injector.Get<IMetaDataLogic>();
        }

        public string Description
        {
            get
            {
                return "Enables REST API support for Meta Data Information";
            }
        }

        public string Name
        {
            get
            {
                return "Meta Data Rest API Endpoints";
            }
        }

        public void Initialize()
        {
        }

        public void Register(IRestEndpointController restRoutes)
        {
            restRoutes.Add(2, "content/meta-data/image/{contentTypeId}/{contentId}", new { resource = "meta-data", action = "show" }, null, HttpMethod.Get, ShowMetaDataImage);
        }
        
        private IRestResponse ShowMetaDataImage(IRestRequest request)
        {
            var response = new ShowMetaDataImageResponse();

            Guid contentTypeId = Guid.Empty;
            Guid contentId = Guid.Empty;

            if (!Guid.TryParse(request.PathParameters["contentTypeId"].ToString(), out contentTypeId))
            {
                response.Errors = new[] { "ContentTypeId must be valid" };
                return response;
            }

            if (!Guid.TryParse(request.PathParameters["contentId"].ToString(), out contentId))
            {
                response.Errors = new[] { "ContentId must be valid" };
                return response;
            }

            if (contentTypeId == Guid.Empty)
            {
                response.Errors = new[] { "ContentTypeId must be specified" };
                return response;
            }

            if (contentId == Guid.Empty)
            {
                response.Errors = new[] { "ContentId must be specified" };
                return response;
            }

            var imageUrl = _logic.GetBestImageUrlForContent(contentId, contentTypeId);

            response.Data = string.IsNullOrWhiteSpace(imageUrl) ? string.Empty : imageUrl;

            return response;
        }
    }
}
