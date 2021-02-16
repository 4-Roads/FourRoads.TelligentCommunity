using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.PwaFeatures.DataProvider;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rest.Infrastructure.Version1;
using Telligent.Evolution.Extensibility.Rest.Version2;
using Telligent.Evolution.Platform.Logging;
using Telligent.Evolution.Rest.Extensions;
using Telligent.Evolution.Rest.Infrastructure.Version2;
using Telligent.Registration.Products;
using HttpMethod = Telligent.Evolution.Extensibility.Rest.Version2.HttpMethod;
using IRestRequest = Telligent.Evolution.Extensibility.Rest.Version2.IRestRequest;
using IRestResponse = Telligent.Evolution.Extensibility.Rest.Version2.IRestResponse;

namespace FourRoads.TelligentCommunity.PwaFeatures.Plugins
{
    public class RestEndpoint : IRestEndpoints
    {
        private PwaDataProvider _data;

        public string Name => "PWA Token REST Endpoints";

        public string Description => "Exposes Token specific calls to the REST Api";

        public void Initialize()
        {
            _data = new PwaDataProvider();
        }

        public void Register(IRestEndpointController restRoutes)
        {
            var tokenRevokeEndpoint = new RestEndpointDocumentation
            {
                EndpointDocumentation = new RestEndpointDocumentationAttribute
                {
                    Resource = "PWAToken",
                    Action = "Delete",
                    Description = "Remove a PWA token to a user"
                }
            };

            tokenRevokeEndpoint.ResponseDocumentation = new RestResponseDocumentationAttribute
            {
                Name = "userId",
                Type = typeof(int),
                Description = "UserID of the token to be removed"
            };

            tokenRevokeEndpoint.ResponseDocumentation = new RestResponseDocumentationAttribute
            {
                Name = "token",
                Type = typeof(string),
                Description = "Token to be removed"
            };

            restRoutes.Add(2, "user/firebase_token", null, null, HttpMethod.Delete, HandleSuggestion, tokenRevokeEndpoint);

            var tokenEndpoint = new RestEndpointDocumentation
            {
                EndpointDocumentation = new RestEndpointDocumentationAttribute
                {
                    Resource = "PWAToken",
                    Action = "Add",
                    Description = "Assign a PWA token to a user"
                }
            };

            tokenEndpoint.ResponseDocumentation = new RestResponseDocumentationAttribute
            {
                Name = "userId",
                Type = typeof(int),
                Description = "UserID of the token to be stored"
            };

            tokenEndpoint.ResponseDocumentation = new RestResponseDocumentationAttribute
            {
                Name = "token",
                Type = typeof(string),
                Description = "Token to be stored"
            };

            restRoutes.Add(2, "user/firebase_token", null, null, HttpMethod.Post, HandleSuggestion, tokenEndpoint);
        }


        private IRestResponse HandleSuggestion(IRestRequest req)
        {
            var resp = new DefaultRestResponse()
            {
                Name = "PWAToken"
            };

            try
            {
                if (int.TryParse(req.Form.Get("userId"), out int userId))
                {
                    string token = req.Form.Get("token");

                    if (!string.IsNullOrWhiteSpace(token) && Apis.Get<IUsers>().AccessingUser.Id == userId)
                    {
                        if (req.Request["HTTP_REST_METHOD"] == null)
                        {
                            _data.StoreToken(userId, token);
                        }
                        else
                        {
                            _data.RevokeToken(userId, token);
                        }
                    }
                    else
                    {
                        resp.Errors = new[] {"Failed to store token"};
                    }

                }
            }
            catch (Exception ex)
            {
                resp.Errors = new[] { ex.UserRenderableMessage() };
            }
            return resp;

        }
    }
}
