using System.Collections;
using FourRoads.TelligentCommunity.RenderingHelper;
using FourRoads.TelligentCommunity.Splash.Logic;
using Telligent.Evolution.Extensibility.Urls.Version1;

namespace FourRoads.TelligentCommunity.Splash.Interfaces
{
    public interface ISplashLogic : ICQProcessor
    {
        void UpdateConfiguration(SplashConfigurationDetails configuration);
        void RegisterUrls(IUrlController controller);
        string ValidateAndHashAccessCode(string password);
        bool SaveDetails(string email, IDictionary additionalFields);
        string GetUserListDownloadUrl();
    }
}
