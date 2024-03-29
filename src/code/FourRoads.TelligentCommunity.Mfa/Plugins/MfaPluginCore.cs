﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using DryIoc;
using FourRoads.Common.Extensions;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using FourRoads.TelligentCommunity.Mfa.DataProvider;
using FourRoads.TelligentCommunity.Mfa.Interfaces;
using FourRoads.TelligentCommunity.Mfa.Logic;
using FourRoads.TelligentCommunity.Mfa.Resources;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;
using Property = Telligent.Evolution.Extensibility.Configuration.Version1.Property;
using PropertyGroup = Telligent.Evolution.Extensibility.Configuration.Version1.PropertyGroup;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class MfaPluginCore : IPluginGroup, IBindingsLoader, INavigable,
        Telligent.Evolution.Extensibility.Version2.IConfigurablePlugin, ITranslatablePlugin, IHttpRequestFilter
    {
        private IPluginConfiguration _configuration;
        private ITranslatablePluginController _translations;

        public void Initialize()
        {
            var jwtSecret = GetJwtSecret();
            Injector.Get<IMfaLogic>().Initialize(_configuration.GetBool("emailVerification").Value,
                PluginManager.Get<VerifyEmailPlugin>().FirstOrDefault(),
                PluginManager.Get<EmailVerifiedSocketMessage>().FirstOrDefault(),
                _configuration.GetDateTime("emailCutoffDate").GetValueOrDefault(DateTime.MinValue),
                jwtSecret,
                _configuration.GetBool("isPersistent").GetValueOrDefault(true)
            );
        }

        public string Name => "4 Roads - MFA Plugin";

        public string Description => "Plugin for adding MFA using the google authenticator";
        public void FilterRequest(IHttpRequest request)
        {
            if (request?.HttpContext == null) 
                return;
            
            try
            {
                if (!(request.HttpContext.Request.Url is null))
                {
                   Injector.Get<IMfaLogic>().FilterRequest(request);
                }
            }
            catch
            {
                // ignored
            }
        }

        public void RegisterUrls(IUrlController controller)
        {
            Injector.Get<IMfaLogic>().RegisterUrls(controller);
        }

        public IEnumerable<Type> Plugins => new[]
        {
            typeof(DependencyInjectionPlugin),
            typeof(MfaSqlScriptsInstaller),
            typeof(FactoryDefaultWidgetProviderInstaller),
            typeof(MfaAuthenticatorExtension),
            typeof(VerifyEmailPlugin),
            typeof(VerifyEmailTokens),
            typeof(EmailVerifiedSocketMessage),
            typeof(DatePropertyTemplate)
        };

        public void LoadBindings(IContainer container)
        {
            container.Register<IMfaLogic, MfaLogic>(Reuse.Singleton);
            container.Register<IMfaDataProvider, MfaDataProvider>(Reuse.Singleton);
        }

        private string GetJwtSecret()
        {
            try
            {
                var config = (MachineKeySection) WebConfigurationManager.GetSection("system.web/machineKey");
                
                if (config != null && !config.DecryptionKey.Contains("AutoGenerate"))
                {
                    return config.DecryptionKey;
                }
                
                byte[] autoGenKeyV4 = (byte[]) Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\ASP.NET\\4.0.30319.0\\", "AutoGenKeyV4", new byte[]{});

                if (autoGenKeyV4 != null)
                {
                    return Convert.ToBase64String(autoGenKeyV4);
                }
            }
            catch (Exception ex)
            {
                Apis.Get<IExceptions>().Log(ex);
            }
#if VERBOSE_MACHINE_KEY_FALLBACK_WARNING
            Apis.Get<IEventLog>().Write("MFA Plugin requires machineKey with decryption key specified. Using fallback method until fixed.",
                new EventLogEntryWriteOptions
                {
                    Category = Name,
                    EventType = nameof(EventType.Warning)
                });
#endif
            
            //if no machineKey defined in web.config, fallback to hash value of a string
            //consisting of serviceUser membership Id and site home page url
            var serviceUser = Apis.Get<IUsers>().Get(new UsersGetOptions {Username = Apis.Get<IUsers>().ServiceUserName});
            var siteUrl =  Apis.Get<IUrl>().Absolute(Apis.Get<ICoreUrls>().Home(false));

            return $"{siteUrl}{serviceUser.ContentId:N}".MD5Hash();
        }

        public int LoadOrder => 0;

        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup group = new PropertyGroup() {LabelResourceName = "GroupOptions", Id = "options"};

                group.Properties.Add(new Property()
                {
                    Id = "emailVerification",
                    LabelResourceName = "EmailVerification", 
                    DataType = nameof(PropertyType.Bool),
                    DefaultValue = "true"
                });
                
                group.Properties.Add(new Property()
                {
                    Id = "emailCutoffDate",
                    LabelResourceName = "EmailCutoffDate",
                    DescriptionResourceName = "EmailCutoffDateDescription",
                    DataType = nameof(PropertyType.Date),
                    Template = "mfadate",
                    DefaultValue = ""
                });
                
                group.Properties.Add( new Property()
                {
                    Id = "isPersistent",
                    LabelResourceName = "IsPersistent",
                    DescriptionResourceName = "IsPersistentDesc",
                    DataType = nameof(PropertyType.Bool), 
                    DefaultValue = "true"
                });
                return new[] {group};
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translations = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation translation = new Translation("en-us");

                translation.Set("EmailVerification", "Enable Email Verification");
                translation.Set("EmailCutoffDate", "Email Cutoff Date");
                translation.Set("EmailCutoffDateDescription", "When enabled on an existing community this date prevents users that have joined the site before this date being asked to authenticate email address");
                translation.Set("GroupOptions", "Options");
                translation.Set("IsPersistent", "Is Persistent");
                translation.Set("IsPersistentDesc", "When enabled, sets MFA cookie expiration date to match Community, otherwise MFA cookie is set for the duration of browser session");

                return new[] {translation};
            }
        }
    }

    public class PasswordPropertyTemplate : IPlugin, IPropertyTemplate
    {
        public string Name => "Password Text Property Template";

        public string Description => "Enables rendering of password text input";

        public void Initialize()
        {
        }

        public string[] DataTypes => new string[] {"String"};

        public string TemplateName => "frPasswordProperty";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options => (PropertyTemplateOption[]) null;

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            var passwordValue = options.Value == null ? string.Empty : options.Value.ToString();
            if (options.Property.Editable)
            {
                writer.Write(
                    "<input type=\"password\" autocomplete=\"off\" minlength=\"32\" maxlength=\"255\" size=\"32\"  id=\"");
                writer.Write(options.UniqueId);
                writer.Write("\"");
                if (!string.IsNullOrWhiteSpace(passwordValue))
                {
                    writer.Write(" value=\"");
                    writer.Write(passwordValue);
                    writer.Write("\"");
                }

                writer.Write("/>");
                writer.Write($@"<script type=""text/javascript"">                
                $(function() {{
                    var api = {options.JsonApi};
                    var i = $('#{options.UniqueId}');
                      api.register({{
                        val: function(val) {{ return i.val(); }},
                        hasValue: function() {{ return i.val() !== ''; }}
                    }});
                    i.change(function() {{ 
                        api.changed(i.val()); 
                    }});
                }});
                </script>");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(passwordValue)) return;

                writer.Write(HttpUtility.HtmlEncode(new string('•', passwordValue.Length)));
            }
        }
    }

    public class DatePropertyTemplate : IPlugin, IPropertyTemplate
    {
        public string Name => "MFA Date Property Template";

        public string Description => "Enables rendering of date configuration properties";

        public void Initialize()
        {
        }

        public string[] DataTypes => new string[] {"Date"};

        public string TemplateName => "mfadate";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options => (PropertyTemplateOption[]) null;

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            DateTime? nullable = options.Value == null ? new DateTime?() : (DateTime?) options.Value;
            if (options.Property.Editable)
            {
                writer.Write("<input type=\"text\" size=\"9\" id=\"");
                writer.Write(options.UniqueId);
                writer.Write("\"");
                if (nullable.HasValue)
                {
                    writer.Write(" value=\"");
                    writer.Write(DateTimeUtil.ToUtc(nullable.Value).ToString("o"));
                    writer.Write("\"");
                }

                writer.Write("/>");
                writer.Write($"<script type=\"text/javascript\">" +
                             $"$(function() {{\r\n    var api = {options.JsonApi};\r\n    var i = $('#{options.UniqueId}');\r\n    " +
                             $"i.glowDateTimeSelector($.extend({{}}, $.fn.glowDateTimeSelector.dateDefaults, " +
                             $"{{ showPopup: true, allowBlankValue: true }}));\r\n    api.register({{\r\n   " +
                             $"     val: function(val) {{ return i.val(); }},\r\n     " +
                             $"   hasValue: function() {{ return i.glowDateTimeSelector('val') != null; }}\r\n    }});\r\n  " +
                             $"  i.on('glowDateTimeSelectorChange', function() {{ \r\n" +
                             $"    api.changed(i.val()); \r\n " +
                             $"   }});\r\n}});\r\n</script>\r\n");
            }
            else
            {
                if (!nullable.HasValue)
                    return;
                writer.Write(HttpUtility.HtmlEncode(nullable.Value.ToString(options.DateFormatString)));
            }
        }
    }
}