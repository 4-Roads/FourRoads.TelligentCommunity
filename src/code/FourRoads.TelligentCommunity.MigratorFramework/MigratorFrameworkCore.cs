using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;
using Newtonsoft.Json;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using IPluginConfiguration = Telligent.Evolution.Extensibility.Version2.IPluginConfiguration;

namespace FourRoads.TelligentCommunity.MigratorFramework
{
    public class MigratorFrameworkCore : IMigratorFramework
    {
        private Guid _panelId = new Guid("{405CFC9D-3522-456D-994B-6DC4100319F7}");
        private Guid _userInterfacePanel = new Guid("{1C60E86A-1850-411B-AF62-E6222E16273F}");
        private IMigrationRepository _repository;
        private IScriptedContentFragmentController _sfController;
        private IHttpCallbackController _cbController;
        private IPluginConfiguration _configuration;

        public void Initialize()
        {
            _repository = new MigrationRepository();

#if DEBUG
            Install(new Version());
#endif
        }

        public string Name => "4 Roads Migration Framework";
        public string Description => "Provides the boilerplate architecture to support migration plugins (IMigratorProvider)";

        public bool UpdateExisting => _configuration.GetBool("updateIfExistsInDestination") ?? false;


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

            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                this,
                _userInterfacePanel,
                "list.vm",
                GetType().Assembly.GetManifestResourceStream("FourRoads.TelligentCommunity.MigratorFramework.Resources.list.vm"));

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
                Apis.Get<IJobService>().Schedule<Migrator>(DateTime.UtcNow.AddSeconds(20) , new Dictionary<string, string>()
                {
                    { "updateIfExistsInDestination", _configuration.GetBool("updateIfExistsInDestination").ToString() }
                   ,{ "checkForDeletions", _configuration.GetBool("checkForDeletions").ToString()}
                   ,{ "maxThreads", _configuration.GetInt("maxThreads").ToString()}
                   ,{"objectHandlers" , httpContext.Request.Form["objectHandlers[]"]}
                });

                _repository.CreateNewContext();
            }
            else if (!string.IsNullOrWhiteSpace(httpContext.Request.QueryString["rewrite"]))
            {
                httpContext.Response.ContentType = "text/xml";

                ProcessRewriteMap(httpContext.Response.OutputStream);

            }
            else
            {
                //Return the status
                MigrationContext context =  _repository.GetMigrationContext() ?? new MigrationContext();

                httpContext.Response.ContentType = "application/json";
                using (StreamWriter wr = new StreamWriter(httpContext.Response.OutputStream))
                { 
                    JsonSerializer.Create().Serialize(wr, context);
                }

                if (context.State == MigrationState.Cancelling && context.LastUpdated.AddMinutes(30) < DateTime.Now ) //FInsihed or borked job 
                {
                    _repository.SetState(MigrationState.Finished);
                }
            }
        }

        private void ProcessRewriteMap(Stream stream)
        {
            using (XmlTextWriter wr = new XmlTextWriter(stream, Encoding.UTF8))
            {
                wr.WriteStartElement("rewriteMaps");
                wr.WriteStartElement("rewriteMap");
                wr.WriteAttributeString("name", "StaticRewrites");
                foreach (Tuple<string,string> redirect in _repository.ListUrlRedirects())
                {
                    wr.WriteStartElement("add");
                    wr.WriteAttributeString("key", redirect.Item1);
                    wr.WriteAttributeString("value", redirect.Item2);
                    wr.WriteEndElement();
                }
                wr.WriteEndElement();
                wr.WriteEndElement();
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

            public string State => _repository.GetMigrationContext().State.ToString();

            public string StatusUrl() => _callbackUrl + "&status=1";

            public string DownloadRewriteUrl() => _callbackUrl + "&rewrite=1";

            public string ResetUrl() => _callbackUrl + "&reset=1";

            public string CancelCurrentJobUrl() => _callbackUrl + "&cancel=1";

            public string StartJobUrl() => _callbackUrl + "&start=1";

            public IPagedList<MigrationLog> LogEntries(IDictionary options)
            {
                return _repository.ListLog((int)options["PageSize"], (int)options["PageIndex"]);
            }

            public IEnumerable<string> ObjectHandlers
            {
                get
                {
                    var migrator = PluginManager.GetSingleton<IMigratorProvider>();

                    if (migrator != null && PluginManager.IsEnabled(migrator))
                    {
                        return migrator.GetFactory().GetOrderObjectHandlers();
                    }

                    return null;
                }
            }

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


        public void Update(IPluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup group = new PropertyGroup() { LabelText = "Options" };
                Property property = new Property() { DataType = "bool", LabelText = "Update Existing Migrated Content", Id = "updateIfExistsInDestination" , DefaultValue = "True"};
                int maxThreads = Environment.ProcessorCount * 2;

                group.Properties.Add(property);
                property = new Property() { DataType = "bool", LabelText = "Check for Deletions", Id = "checkForDeletions", DefaultValue = "True" };
                group.Properties.Add(property);
                property = new Property() { DataType = "int", DefaultValue = maxThreads.ToString(), Id = "maxThreads", LabelText = "Max threads" };

                for (int counter = 1; counter <= maxThreads; counter++)
                {
                    property.SelectableValues.Add(new PropertyValue { LabelText = $"{counter}", Value = $"{counter}", OrderNumber = counter });
                }

                group.Properties.Add(property);

                return new[] { group };


            }
        }
    }
}
