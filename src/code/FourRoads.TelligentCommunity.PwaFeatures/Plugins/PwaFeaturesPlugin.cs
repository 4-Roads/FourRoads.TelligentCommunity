using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.PwaFeatures.DataProvider;
using FourRoads.TelligentCommunity.PwaFeatures.Extensions;
using FourRoads.TelligentCommunity.PwaFeatures.Resources;
using Google.Apis.Auth.OAuth2;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IConfigurablePlugin = Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using IRequiredConfigurationPlugin = Telligent.Evolution.Extensibility.Version2.IRequiredConfigurationPlugin;
using Notification = Telligent.Evolution.Extensibility.Api.Entities.Version1.Notification;
using Property = Telligent.Evolution.Extensibility.Configuration.Version1.Property;
using PropertyGroup = Telligent.Evolution.Extensibility.Configuration.Version1.PropertyGroup;


namespace FourRoads.TelligentCommunity.PwaFeatures.Plugins
{
    public class PwaFeaturesPlugin : IConfigurablePlugin, INotificationDistributionType, ITranslatablePlugin,  IPluginGroup, INavigable, IHtmlHeaderExtension , IScriptedContentFragmentExtension, IScriptablePlugin, IRequiredConfigurationPlugin
    {
        public string Name => "4 Roads - PWA Features";
        public string Description => "Extends Telligent to Support PWA features";
        public void RegisterUrls(IUrlController controller)
        {
            controller.AddRaw("offline-page","offline", null , null,
                (context, pageContext) =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.Write( _controller.RenderContent(new Guid("b42ccd131e544565a6916706deac683c"), new NameValueCollection()
                    {
                        { "UserId" , Apis.Get<IUsers>().AccessingUser.Id.ToString() },
                        { "Page" , "offline"},
                    }));

                }, new RawDefinitionOptions());


            controller.AddRaw("serviceworker", "serviceworker.js", null, null,
                (context, pageContext) =>
                {
                    context.Response.ContentType = "application/javascript";
                    context.Response.Write(_controller.RenderContent(new Guid("b42ccd131e544565a6916706deac683c"), new NameValueCollection()
                    {
                        { "UserId" , Apis.Get<IUsers>().AccessingUser.Id.ToString() },
                        { "Page" , "serviceworker"},
                        { "FirebaseSenderId" , _configuration.GetString("FirebaseSenderId")},
                        { "FirebaseConfig", _configuration.GetString("FirebaseConfig")}
                    }));

                }, new RawDefinitionOptions());

            controller.AddRaw("manifest", "manifest.json", null, null,
                (context, pageContext) =>
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    context.Response.Write(_controller.RenderContent(new Guid("b42ccd131e544565a6916706deac683c"), new NameValueCollection()
                    {
                        { "UserId" , Apis.Get<IUsers>().AccessingUser.Id.ToString() },
                        { "Page" , "manifest"},
                        { "FirebaseSenderId" , _configuration.GetString("FirebaseSenderId")},
                        { "FirebaseConfig", _configuration.GetString("FirebaseConfig")}
                    }));

                }, new RawDefinitionOptions());
        }

        public string ExtensionName => "fr_pwa_features";
        public object Extension => new UtilityExtension(_configuration.GetString("FirebaseConfig"));
        private ITranslatablePluginController _translation;
        private IPluginConfiguration _configuration;
        private IScriptedContentFragmentController _controller;
        private PwaDataProvider _data;

        public void Initialize()
        {
            if (PluginManager.IsEnabled(this))
            {
                _data = new PwaDataProvider();

                FirebaseApp.Create(
                    new AppOptions()
                    {
                        Credential = GoogleCredential.FromJson(_configuration.GetString("FirebaseAdminJson"))
                    });
            }
        }

        public bool Distribute(Notification notification, NotificationUserChanges userChanges)
        {
            try
            {
                if (PluginManager.IsEnabled(this))
                {
                    if (userChanges != NotificationUserChanges.UsersAdded || (notification == null || notification.UserId == 0))
                        return false;

                    NotificationListOptions notificationListOptions = new NotificationListOptions();
                    notificationListOptions.UserId = notification.UserId;
                    notificationListOptions.IsRead = false;
                    notificationListOptions.PageIndex = 0;
                    notificationListOptions.PageSize = 1;

                    string notificationId = notification.NotificationId.ToString("N");

                    string shortText = Apis.Get<IUrl>().Decode(notification.Message("ShortText"));

                    var user = Apis.Get<Users>().Get(new UsersGetOptions() {Id = notification.UserId});

                    var pageContext = Apis.Get<IUrl>().ParsePageContext(notification.TargetUrl);

                    var item = pageContext?.ContextItems.Find(f => f.ContentTypeId == notification.ContentTypeId);

                    var title = "Activity on ";

                    if (item != null)
                    {
                        var content = Apis.Get<IContents>().Get(item.ContentId.Value, item.ContentTypeId.Value);

                        if (content != null)
                        {
                            title = content.HtmlName("text");
                        }
                    }

                    PushGoogleNotification(user, notificationId, title, shortText, notification.TargetUrl);

                    return true;
                }
            }
            catch (Exception ex)
            {
                new TCException("Push Notification General Error", ex).Log();
            }

            return true;
        }

        private void PushGoogleNotification(User user, string notificationId, string title, string body, string url)
        {
            // Subscribe the devices corresponding to the registration tokens to the
                // topic
                var response = Task.Run(
                    () =>
                    {
                        try
                        {
                            var registrationTokens = _data.ListUserTokens(user.Id.Value);

                            if (registrationTokens.Count > 0)
                            {

                                // See documentation on defining a message payload.
                                var message = new MulticastMessage()
                                {
                                    Data = new Dictionary<string, string>()
                                    {
                                        {"title", title},
                                        {"body", body},
                                        {"url", url},
                                        {"targetUserId", user.Id.GetValueOrDefault(-1).ToString()},
                                    },
                                    Tokens = registrationTokens
                                };

                                // Send a message to the devices subscribed to the provided topic.
                                return FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            new TCException("Google Push Notification Failed" , ex).Log();
                        }

                        return null;
                    }).Result;
        }

        public string NotificationDistributionDescription => _translation.GetLanguageResourceValue("distriubtion_type_description");

        public string NotificationDistributionName => _translation.GetLanguageResourceValue("distribution_type_name");

        public Guid NotificationDistributionTypeId { get; } = new Guid("{6753662C-02D0-4088-BF2E-9019E104FF15}");

        public bool IsEnabledByDefault => true;

        public void SetController(ITranslatablePluginController controller)
        {
            _translation = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation translation = new Translation("en-US");
                translation.Set("distribution_type_name", "Push Notifications");
                translation.Set("distriubtion_type_description", "Push notifications to Fireabsae");
                return new[]
                {
                    translation
                };
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup firebaseConfiguration = new PropertyGroup() { Id = "firebaseConfiguration", LabelText = "Firebase Push Configuration", OrderNumber = 1 };

                Property senderId = new Property() { Id = "FirebaseSenderId", LabelText = "SenderId", DataType = "string"};
                senderId.DescriptionText = "This is the Firebase Sender Id.";
                firebaseConfiguration.Properties.Add(senderId);

                Property config = new Property() { Id = "FirebaseConfig", LabelText = "Config", DataType = "string" };
                config.DescriptionText = "The Firebase config obtained from https://console.firebase.google.com/ Settings -> Your apps .";
                config.Template = "string";
                config.Options["rows"] = "10";
                firebaseConfiguration.Properties.Add(config);

                Property token = new Property() { Id = "FirebaseAdminJson", LabelText = "Server Key [PRIVATE]", DataType = "string" };
                token.DescriptionText = "The Firebase Admin SDK JSON generated from https://console.firebase.google.com/ Settings -> Service Accounts.";
                token.Template = "string";
                token.Options["rows"] = "10";
                firebaseConfiguration.Properties.Add(token);

                return new[] {  firebaseConfiguration };
            }
        }

        public IEnumerable<Type> Plugins  => new[]
        {
            typeof (WidgetInstaller),
            typeof(CustomUrlsPanelInstaller),
            typeof(PwaSqlScriptsInstaller),
            typeof(RestEndpoint)
        };

        public string GetHeader(RenderTarget target)
        {
            return Convert.ToString(HttpContext.Current.Items["pwa_manifest_path"]) ?? string.Empty;
        }

        public bool IsCacheable => false;
        public bool VaryCacheByUser => false;
        public Guid ScriptedContentFragmentFactoryDefaultIdentifier => new Guid("1fe74a21eab446279f261d167bd86d0a");
        public void Register(IScriptedContentFragmentController controller)
        {
            _controller = controller;

            controller.Register(
                new ScriptedContentFragmentOptions(new Guid("b42ccd131e544565a6916706deac683c"))
                {
                    CanBeThemeVersioned = true,
                    CanReadPluginConfiguration = false,
                    CanWritePluginConfiguration = false,
                    CanHaveHeader = false,
                    IsEditable = true,
                    Extensions = { Injector.Get<CustomUrlsPanelContext>() },

                });
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration.GetString("FirebaseSenderId")) &&
                                    !string.IsNullOrWhiteSpace(_configuration.GetString("FirebaseConfig")) &&
                                    !string.IsNullOrWhiteSpace(_configuration.GetString("FirebaseAdminJson"));
    }

}
