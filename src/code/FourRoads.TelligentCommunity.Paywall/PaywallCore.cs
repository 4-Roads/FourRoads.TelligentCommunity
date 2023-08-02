using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Api.Version2;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Security.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Version2;
using IPermissions = Telligent.Evolution.Extensibility.Api.Version2.IPermissions;

namespace FourRoads.TelligentCommunity.Paywall
{
    public class PaywallCore : IPlugin, IPermissionRegistrar, ITranslatablePlugin, IConfigurablePlugin, IPluginGroup
    {
        private ITranslatablePluginController _translationController;

        public void Initialize()
        {
            Apis.Get<IHtml>().Events.Render += EventsOnRender;
        }

        private void EventsOnRender(HtmlRenderEventArgs e)
        {
            if (string.Compare(e.RenderedProperty, "Body", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var currentContext = Apis.Get<IUrl>().CurrentContext;
                if (currentContext != null)
                {
                    if (currentContext.ApplicationTypeId != null)
                    {
                        var items = currentContext.ContextItems.GetAllContextItems();
                        if (items != null && items.Count > 0)
                        {
                            if (EnableDebugging)
                            {
                                Apis.Get<IEventLog>().Write($"Paywall debug : {currentContext.PageName} {currentContext.UrlName} {currentContext.ApplicationTypeId} {JsonConvert.SerializeObject(items, Formatting.None)}", new EventLogEntryWriteOptions() { Category = "Paywall", EventType = "Information" });
                            }

                            // are we viewing a piece of content ?
                            var contextItem = items.FirstOrDefault(v => v.ApplicationTypeId == currentContext.ApplicationTypeId && v.ApplicationTypeId != v.ContentTypeId);
                            if (contextItem != null && contextItem.ApplicationId.HasValue)
                            {
                                if (EnableDebugging)
                                {
                                    Apis.Get<IEventLog>().Write($"Paywall checking : {currentContext.PageName} {currentContext.UrlName} {currentContext.ApplicationTypeId}", new EventLogEntryWriteOptions() { Category = "Paywall", EventType = "Information" });
                                }

                                if (!Apis.Get<IPermissions>().CheckPermission(currentContext.ApplicationTypeId.Value,
                                       Apis.Get<IUsers>().AccessingUser.Id.GetValueOrDefault(0), new PermissionCheckOptions()
                                       {
                                           ApplicationTypeId = currentContext.ApplicationTypeId,
                                           ApplicationId = contextItem.ApplicationId
                                       }).IsAllowed)
                                {
                                    //Not allowed for this user so truncate the content according to the configuration
                                    if (TruncateLength > 0)
                                    {
                                        if (EnableDebugging)
                                        {
                                            Apis.Get<IEventLog>().Write($"Paywall truncating : {currentContext.PageName} {currentContext.UrlName} {currentContext.ApplicationTypeId}", new EventLogEntryWriteOptions() { Category = "Paywall", EventType = "Information" });
                                        }

                                        e.RenderedHtml = "<div class=\"paywall-restricted\">" +
                                                         Apis.Get<ILanguage>().Truncate(e.RenderedHtml, TruncateLength, "",
                                                             true) +
                                                         "</div>";
                                    }

                                    //optionally also display paywall message
                                    if (ShowPopup)
                                    {
                                        if (EnableDebugging)
                                        {
                                            Apis.Get<IEventLog>().Write($"Paywall trigger popup : {currentContext.PageName} {currentContext.UrlName} {currentContext.ApplicationTypeId}", new EventLogEntryWriteOptions() { Category = "Paywall", EventType = "Information" });
                                        }

                                        //render a small amount of javascript to post a message to make the paywall widget display
                                        e.RenderedHtml +=
                                           @"<script type=""text/javascript""> 
                                    jQuery(function(){          
                                            jQuery.telligent.evolution.messaging.subscribe('paywall.ready', function (data) {

                                                   jQuery.telligent.evolution.messaging.publish('paywall.displayPopup', {  });  
                                            }); 
                                    });
                                    </script>";
                                    }
                                }
                                else if (EnableDebugging)
                                {
                                    Apis.Get<IEventLog>().Write($"Paywall bypassed : {currentContext.PageName} {currentContext.UrlName} {currentContext.ApplicationTypeId}", new EventLogEntryWriteOptions() { Category = "Paywall", EventType = "Information" });
                                }
                            }
                        }
                    }
                }
            }
        }

        public string Name => "4 Roads Paywall";
        public string Description => "This plugin adds the ability to add paywalls to any content on the site";
        public void RegisterPermissions(IPermissionRegistrarController permissionController)
        {
            var membershopPermis = new MembershipGroupPermissionConfiguration()
            { Administrators = true, Managers = true, Moderators = true, Members = true, Owners = true };

            //Register a application level permission for each application in the site
            foreach (var applicationType in Apis.Get<IApplicationTypes>().List())
            {
                permissionController.Register(new Permission(applicationType.Id.Value, "ByPassPaywall", "ByPassPaywallDescription", _translationController, applicationType.Id.Value, new PermissionConfiguration()
                {
                    Joinless = new JoinlessGroupPermissionConfiguration() { Administrators = true, Owners = true, RegisteredUsers = true },
                    PrivateListed = membershopPermis,
                    PrivateUnlisted = membershopPermis,
                    PublicClosed = membershopPermis,
                    PublicOpen = membershopPermis
                }));
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translationController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                var translation = new Translation("en-us");

                translation.Set("Options", "Options");
                translation.Set("ByPassPaywall", "By Pass Paywall");
                translation.Set("ByPassPaywallDescription", "Set this to true to enable users to bypass seeing the paywall");
                translation.Set("TruncateLength", "Truncate Text Length");
                translation.Set("ShowPopup", "Show Paywall Popup");
                translation.Set("EnableDebugging", "Enable Debugging (see event log)");

                return new Translation[]
                {
                    translation
                };
            }
        }

        private int TruncateLength { get; set; }
        private bool ShowPopup { get; set; }
        private bool EnableDebugging { get; set; }

        public void Update(IPluginConfiguration configuration)
        {
            TruncateLength = configuration.GetInt("truncatelength").GetValueOrDefault(0);
            ShowPopup = configuration.GetBool("showpopup").GetValueOrDefault(true);
            EnableDebugging = configuration.GetBool("enableDebugging").GetValueOrDefault(false);
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var grp = new PropertyGroup() { LabelResourceName = "Options" };

                var truncate = new Property()
                {
                    LabelResourceName = "TruncateLength",
                    DataType = "Int",
                    Id = "truncatelength",
                    DefaultValue = "500"
                };
                grp.Properties.Add(truncate);

                var showPopup = new Property()
                {
                    LabelResourceName = "ShowPopup",
                    DataType = "Bool",
                    Id = "showpopup",
                    DefaultValue = "true"
                };
                grp.Properties.Add(showPopup);

                var enableDebugging = new Property()
                {
                    LabelResourceName = "EnableDebugging",
                    DataType = "Bool",
                    Id = "enableDebugging",
                    DefaultValue = "false"
                };
                grp.Properties.Add(enableDebugging);

                return new[] { grp };
            }

        }

        public IEnumerable<Type> Plugins => new[] { typeof(DefaultWidgetInstaller) };
    }
}
