using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Api.Version2;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Security.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensibility.Version2;
using IPermissions = Telligent.Evolution.Extensibility.Api.Version2.IPermissions;


namespace FourRoads.TelligentCommunity.Paywall
{
    public class PaywallCore : IPlugin, IPermissionRegistrar , ITranslatablePlugin, IConfigurablePlugin, IPluginGroup
    {
        private ITranslatablePluginController _translationController;
        private IScriptedContentFragmentController _fragmentConfiguration;

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

                   var items = currentContext.ContextItems.GetAllContextItems();

                   if (currentContext.ApplicationTypeId != null)
                   {
                       var contextItem =
                           items.FirstOrDefault(v => v.ApplicationTypeId == (currentContext.ApplicationTypeId));

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
                               e.RenderedHtml = "<div class=\"paywall-restricted\">" +
                                                Apis.Get<ILanguage>().Truncate(e.RenderedHtml, TruncateLength, "",
                                                    true) +
                                                "</div>";
                           }

                           //optionally also display paywall message
                           if (ShowPopup)
                           {
                                //render a small amount of javascrtipt to post a message to make the paywall widget display
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
                permissionController.Register(new Permission(applicationType.Id.Value, "ByPassPaywall" , "ByPassPaywallDescription", _translationController, applicationType.Id.Value , new PermissionConfiguration()
                {
                    Joinless = new JoinlessGroupPermissionConfiguration(){Administrators = true, Owners = true,RegisteredUsers = true},
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

                translation.Set("ByPassPaywall", "By Pass Paywall");
                translation.Set("ByPassPaywallDescription", "Set this to true to enable useds to bypass seeing the paywall");
                translation.Set("TruncateLength", "Truncate Text Length");
                translation.Set("ShowPopup", "Show Paywall Popup");


                return new Translation[]
                {
                    translation
                };
            }
        }

        private int TruncateLength { get; set; }
        private bool ShowPopup { get; set; }

        public void Update(IPluginConfiguration configuration)
        {
            TruncateLength = configuration.GetInt("truncatelength").GetValueOrDefault(0);
            ShowPopup = configuration.GetBool("showpopup").GetValueOrDefault(true);
        }

        public PropertyGroup[] ConfigurationOptions {
            get
            {

                var grp = new PropertyGroup() { LabelResourceName = "Optionb" };

                var truncate = new Property()
                {
                    LabelResourceName = "TruncateLength",
                    DataType = "Int", 
                    Id = "truncatelength", 
                    DefaultValue = "500"
                };

                grp.Properties.Add(truncate);

                var showPopup= new Property()
                {
                    LabelResourceName = "ShowPopup",
                    DataType = "Bool",
                    Id = "showpopup",
                    DefaultValue = "true"
                };

                grp.Properties.Add(showPopup);

                return new[] { grp };
            }

        }

        public IEnumerable<Type> Plugins => new[] { typeof(DefaultWidgetInstaller) };
    }
}
