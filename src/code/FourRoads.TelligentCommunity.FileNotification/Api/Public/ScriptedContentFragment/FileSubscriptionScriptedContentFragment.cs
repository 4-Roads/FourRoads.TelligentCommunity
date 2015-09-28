using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Entities;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Services;
using FourRoads.TelligentCommunity.FileNotification.Data;
using FourRoads.TelligentCommunity.FileNotification.Interfaces;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Plugins;
using Telligent.Evolution.Api.Entities.Mappers;
using Telligent.Evolution.Api.Services;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.MediaGalleries.Components;
using Telligent.Evolution.VelocityExtensions;

namespace FourRoads.TelligentCommunity.FileNotification.Api.Public.ScriptedContentFragment
{
    public class FileSubscriptionScriptedContentFragment : IFileSubscriptionScriptedContentFragment
    {
        private readonly IFileSubscriptionService _iFileSubscriptionService;

        public FileSubscriptionScriptedContentFragment(IFileSubscriptionService iFileSubscriptionService)
        {
            _iFileSubscriptionService = iFileSubscriptionService;
        }

        public Guid SubscribeToFile(int fileId, string email, int userId)
        {
            var response = Guid.Empty;
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);
            var csContext = CSContext.Current;
            var accessingUser = csContext.User;
            
            if (!string.IsNullOrWhiteSpace(email) && match.Success && fileId != 0 && userId == accessingUser.UserID)
            {
                var fileSubscription = new FileSubscription()
                {
                    UserId = userId,
                    Email = email,
                    FileId = fileId,
                    IsConfirmed = true,
                    FileSubscriptionId = Guid.Empty,
                    SubscriptionType = EmailSubscriptionType.Post
                };

                try
                {
                    response =  _iFileSubscriptionService.SubscribeToFile(fileSubscription);
                }
                catch (Exception ex)
                {
                    var csException = new CSException(CSExceptionType.DataProvider, ex.Message);
                    csException.Log();
                    fileSubscription.Errors.Add(new Error("error", ex.Message));
                }
                
            }
            return response;
        }

        public Guid SubscribeAnonymously(string email, int fileId)
        {
            Guid response = Guid.Empty;
            bool isSuccess = false;
            var regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            if (!string.IsNullOrWhiteSpace(email))
            {
                Match match = regex.Match(email);
                isSuccess = match.Success;
            }

            var csContext = CSContext.Current;
            var accessingUser = csContext.User;

            var fileSubscription = new FileSubscription()
                                           {
                                               UserId = accessingUser.UserID,
                                               Email = email,
                                               FileId = fileId,
                                               IsConfirmed = true, // for anonymous user : should this be true or false?
                                               FileSubscriptionId = Guid.Empty,
                                               SubscriptionType = EmailSubscriptionType.Post
                                           };
            

            if (isSuccess && accessingUser.IsAnonymous)
            {
                try
                {
                    response = _iFileSubscriptionService.SubscribeToFile(fileSubscription);
                }
                catch (Exception ex)
                {
                    var csException = new CSException(CSExceptionType.DataProvider, ex.Message);
                    csException.Log();
                    fileSubscription.Errors.Add(new Error("error", ex.Message));
                }
            }

            return response;
        }

        public bool IsSubscribed(Guid fileSubscriptionId, int userId)
        {
            bool response = false;
            var csContext = CSContext.Current;
            var accessingUser = csContext.User;
            int accessingUserId = accessingUser.UserID;
            try
            {
                if (!accessingUser.IsAnonymous && fileSubscriptionId != Guid.Empty && userId == accessingUserId)
                {
                    response = _iFileSubscriptionService.IsSubscribed(fileSubscriptionId, userId);
                }
            }
            catch (Exception ex)
            {
                var csException = new CSException(CSExceptionType.DataProvider, ex.Message);
                csException.Log();
            }
            return response;
        }

        public void UnsubscribeToFile(Guid fileSubscriptionId, int fileId)
        {
            var csContext = CSContext.Current;
            var accessingUser = csContext.User;
            int userId = accessingUser.UserID;

            try
            {
                if (fileSubscriptionId != Guid.Empty && fileId != 0 && !accessingUser.IsAnonymous)
                {
                    bool isSubscribedBefore = _iFileSubscriptionService.IsSubscribed(fileSubscriptionId, userId);
                    if (isSubscribedBefore)
                    {
                      _iFileSubscriptionService.UnsubscribeFromFile(fileSubscriptionId, fileId);
                    }
                }
            }
            catch(Exception ex)
            {
                var csException = new CSException(CSExceptionType.DataProvider, ex.Message);
                csException.Log();
            }
        }

        public Guid GetSubscriptionId(int fileId, int userId)
        {
            var csContext = CSContext.Current;
            var accessingUser = csContext.User;

            Guid response = Guid.Empty;
            try
            {
                if (!accessingUser.IsAnonymous && accessingUser.UserID == userId && fileId != 0)
                {
                    response = _iFileSubscriptionService.GetSubscriptionId(fileId, userId);
                }
            } 
            catch(Exception ex)
            {
                var csException = new CSException(CSExceptionType.DataProvider, ex.Message);
                csException.Log();
            }
            return response;
        }
    }
}
