using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;
using Newtonsoft.Json;
using Telligent.Common;
using Telligent.Evolution.Components.Jobs;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Administration.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Rest.Infrastructure;
using Telligent.Jobs;

namespace FourRoads.TelligentCommunity.MigratorFramework
{
    public class MigratorFrameworkCore : IPlugin, IInstallablePlugin, IAdministrationPanel, IPluginGroup, IScriptablePlugin, IHttpCallback
    {
        private Guid _panelId = new Guid("{405CFC9D-3522-456D-994B-6DC4100319F7}");
        private Guid _userInterfacePanel = new Guid("{1C60E86A-1850-411B-AF62-E6222E16273F}");
        private IMigrationRepository _repository;
        private IScriptedContentFragmentController _sfController;
        private IHttpCallbackController _cbController;

        public void Initialize()
        {
            _repository = new MigrationRepository();

#if DEBUG
            Install(new Version());
#endif
        }

        public string Name => "4 Roads Migration Framework";
        public string Description => "Provides the boilerplate architecture to support migration plugins (IMigratorProvider)";
 
        public void Install(Version lastInstalledVersion)
        {
            //Create the SQL data 
            _repository.Install(lastInstalledVersion);

            //Install the admin widget
            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(
                    this,
                    _userInterfacePanel.ToString("N").ToLower() + ".xml",
                    GetType().Assembly.GetManifestResourceStream("FourRoads.TelligentCommunity.MigratorFramework.Resources.widget.xml")
                );

            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                this,
                _userInterfacePanel,
                "migration.js",
                GetType().Assembly.GetManifestResourceStream("FourRoads.TelligentCommunity.MigratorFramework.Resources.migration.js"));

        }

        public void Uninstall()
        {

        }

        public Version Version => GetType().Assembly.GetName().Version;
        public Guid PanelId => _panelId;
        public string CssClass => "";
        public int? DisplayOrder => 400;
        public bool IsCacheable => false;
        public bool VaryCacheByUser => false;
        public bool HasAccess(int userId)
        {;
            return Apis.Get<IRoleUsers>().IsUserInRoles(Apis.Get<IUsers>().Get(new UsersGetOptions() {Id = userId}).Username, new[] {"Administrators"});
        }

        public string GetViewHtml()
        {
            return _sfController.RenderContent(_userInterfacePanel, new NameValueCollection()
            {
                {"CallbackUrl" , _cbController.GetUrl()}
            });
        }

        public string PanelName => "Migration";
        public string PanelDescription => "Allows Administrators to view and manage migrations";
        public Guid AdministrationPanelCategoryId => MigrationPanelCategory.CategoryId;
        public IEnumerable<Type> Plugins => new[] {typeof(MigrationPanelCategory)};
        public Guid ScriptedContentFragmentFactoryDefaultIdentifier => _panelId;
        public void Register(IScriptedContentFragmentController controller)
        {
            _sfController = controller;
            controller.Register(new ScriptedContentFragmentOptions(_userInterfacePanel)
            {
                CanHaveHeader = false,
                CanBeThemeVersioned = false,
                CanHaveWrapperCss = false,
                CanReadPluginConfiguration = false,
                CanWritePluginConfiguration = false,
                IsEditable = false,
                HasAccess = (i, context) => this.HasAccess(context.UserId),
                Extensions = {new ContextExtension() }
            });
        }

        public void ProcessRequest(System.Web.HttpContextBase httpContext)
        {
            if (!string.IsNullOrWhiteSpace(httpContext.Request.QueryString["cancel"]))
            {
                var context = Task.Run(()=> _repository.GetMigrationContext()).Result;

                if (context.State != MigrationState.Finished)
                    _repository.SetState(MigrationState.Cancelling);
            }
            else if (!string.IsNullOrWhiteSpace(httpContext.Request.QueryString["reset"]))
            {
                var context = Task.Run(() => _repository.GetMigrationContext()).Result;
                //Cant reset until the 
                if (context.State < MigrationState.Cancelling)
                {
                    _repository.SetState(MigrationState.Cancelling);
                }
                else if (context.State == MigrationState.Finished || context.LastUpdated.AddMinutes(10) < DateTime.Now ) //FInsihed or borked job 
                {
                    _repository.ResetJob();
                }
            }
            else if (!string.IsNullOrWhiteSpace(httpContext.Request.QueryString["start"]))
            {
                Apis.Get<IJobService>().Schedule<Migrator>(DateTime.UtcNow.AddSeconds(20));

                _repository.CreateNewContext();
            }
            else
            {
                //Return the status
                MigrationContext context = Task.Run(() => _repository.GetMigrationContext()).Result ?? new MigrationContext();

                httpContext.Response.ContentType = "application/json";
                using (StreamWriter wr = new StreamWriter(httpContext.Response.OutputStream))
                { 
                    JsonSerializer.Create().Serialize(wr, context);
                }

            }
        }

        public void SetController(IHttpCallbackController controller)
        {
            _cbController = controller;
        }

        private class ContextExtension : IContextualScriptedContentFragmentExtension
        {
            public object GetExtension(NameValueCollection context)
            {
                return new ContextObject(context["CallbackUrl"]);
            }

            public string ExtensionName => "context";
        }

        private class ContextObject
        {
            private IMigrationRepository _repository;
            private string _callbackUrl;

            public ContextObject(string callBackUrl)
            {
                _callbackUrl = callBackUrl;
                _repository = new MigrationRepository();
            }

            public string State => Task.Run(()=> _repository.GetMigrationContext()).Result.State.ToString();

            public string StatusUrl() => _callbackUrl + "&status=1";

            public string ResetUrl() => _callbackUrl + "&reset=1";

            public string CancelCurrentJobUrl() => _callbackUrl + "&cancel=1";

            public string StartJobUrl() => _callbackUrl + "&start=1";

            public string EnabledMigratorName
            {
                get
                {
                    var migrator = PluginManager.GetSingleton<IMigratorProvider>();

                    if (migrator != null && PluginManager.IsEnabled(migrator))
                    {
                        return migrator.Name;
                    }

                    return null;
                }
            }
        }

    
    }
}
