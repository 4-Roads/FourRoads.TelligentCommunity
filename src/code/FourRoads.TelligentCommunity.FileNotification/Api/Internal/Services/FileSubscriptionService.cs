using System;
using FourRoads.TelligentCommunity.FileNotification.Api.Internal.Entities;
using FourRoads.TelligentCommunity.FileNotification.Interfaces;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Data;

namespace FourRoads.TelligentCommunity.FileNotification.Api.Internal.Services
{
    public class FileSubscriptionDataProvider : IFileSubscriptionService
    {
        private readonly IFileSubscriptionDataService _iFileSubscriptionDataService;
        
        public FileSubscriptionDataProvider(IFileSubscriptionDataService iFileSubscriptionDataService)
        {
            _iFileSubscriptionDataService = iFileSubscriptionDataService;
        }

        public static readonly string FileSubscriptionDataProviderName = "FileSubscriptionSqlDataProvider";
        private static FileSubscriptionDataProvider _fileSubscriptionSqlDataProviderInstance;


        public  void UnsubscribeFromFile(Guid fileSubscriptionId, int fileId)
        {
            _iFileSubscriptionDataService.UnsubscribeFromFile(fileSubscriptionId, fileId);
        }

        public Guid SubscribeToFile(FileSubscription fileSubscription)
        {
            var response = Guid.Empty;
            response =   _iFileSubscriptionDataService.SubscribeToFile(fileSubscription);
           return response;

        }

        public  bool IsSubscribed(Guid fileSubscriptionId, int userId)
        {
            bool response = false;
            response =  _iFileSubscriptionDataService.IsSubscribed(fileSubscriptionId, userId);
            return response;
        }

        public Guid GetSubscriptionId(int fileId, int userId)
        {
            Guid response = Guid.Empty;
            response = _iFileSubscriptionDataService.GetSubscriptionId(fileId, userId);
            return response;
        }

    }
}
