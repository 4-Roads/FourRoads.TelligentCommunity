using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.GroupDataExport
{
    public class GroupExportPropertyTemplate : IPropertyTemplate, INamingContainer, IHttpCallback
    {

        public string[] DataTypes => new string[] { "custom", "string" };

        public string TemplateName => "groupExport_template";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options
        {
            get
            {
                return null;
            }
        }

        public string Name => "Group Export Template";

        public string Description => "Provides an interface for group details to be exported from Telligent";

        private IHttpCallbackController _callbackController;

        public void Initialize()
        {

        }

        public void ProcessRequest(HttpContextBase httpContext)
        {
            if (httpContext.Request.Form["action"] != null)
            {
                var action = httpContext.Request.Form["action"].ToString();
                var group = httpContext.Request.Form["group"]?.ToString();
                var summary = httpContext.Request.Form["summary"]?.ToString();

                if (action == "create" 
                    && !string.IsNullOrWhiteSpace(group) 
                    && !string.IsNullOrWhiteSpace(summary)
                    )
                {
                    CreateReport(group, summary);
                }
                else if (action == "delete")
                {
                    Delete();   
                }
            }

            return;
        }

        public void SetController(IHttpCallbackController controller)
        {
            _callbackController = controller;
        }

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            if (options.Property.Editable)
            {
                if (IsJobRunningOrScheduled())
                {
                    ShowRunning(writer, options);
                }
                else if (ResultsExist())
                {
                    ShowResults(writer, options);
                }
                else
                {
                    ShowChoices(writer, options);
                }       
            }
        }
        
        private void CleanUpFiles()
        {
            foreach (var file in Filestore().GetFiles(PathSearchOption.AllPaths))
            {
                Filestore().Delete(file.Path, file.FileName);
            }
        }

        void CreateReport(string group, string isSummary)
        {
            if (Filestore().GetFile("", "processing.txt") == null)
            {
                Filestore().AddUpdateFile("", "processing.txt", new MemoryStream() { });
                Apis.Get<IJobService>().Schedule(typeof(GroupExportJob), DateTime.Now.ToUniversalTime(), new Dictionary<string, string>() { { "groupId", group }, { "summary", isSummary } });
            }
        }

        void Delete()
        {
            CleanUpFiles();
        }

        private bool ResultsExist()
        {
            return (Filestore().GetFile("", "results.csv") != null);
        }

        private static ICentralizedFileStorageProvider Filestore()
        {
            return CentralizedFileStorage.GetFileStore(GroupExportPlugin.FILESTORE_KEY);
        }

        private bool IsJobRunningOrScheduled()
        {
            return Filestore().GetFile("", "processing.txt") != null;
        }

        private void ShowChoices(TextWriter writer, IPropertyTemplateOptions options)
        {
            var createLabel = options.Property.Options["createLabel"] ?? "Create Report";
            var resturl = (_callbackController != null) ? _callbackController.GetUrl() : "";

            var groupOptions = new StringBuilder();
            var list = new GroupsListOptions()
            {
                PageIndex = 0,
                PageSize = 100
            };

            bool moreRecords = true;
            while (moreRecords)
            {
                var groups = Apis.Get<Groups>().List(list);
                moreRecords = groups.TotalCount > (++list.PageIndex * list.PageSize);

                if (!groups.HasErrors())
                {
                    foreach (var group in groups)
                    {
                        if ((group.TotalMembers ?? 0) > 0)
                        {
                            groupOptions.AppendLine($"<option value='{group.Id.ToString()}'>{group.Name}</option>");
                        }
                    }
                }
            }

            // html
            writer.Write(
                $@"<div id='{options.FormId}' style='padding-top: 1rem;'>
                    <select name='{options.FormId}_group' id='{options.FormId}_group'>
                        {groupOptions.ToString()}
                    </select>
                    <br /><br />
                    <input type='checkbox' id='{options.FormId}_summary'>Summary Report</input>
                    <br /><br />
                    <button type='button' class='button' id='{options.FormId}_create'>{createLabel}</button>
                </div>
            ");

            var action = string.Empty;
            if (!string.IsNullOrWhiteSpace(resturl))
            {
                action = GetAction(resturl);
            }

            writer.Write($@"
                <script type='text/javascript'>
                    $(document).ready(function() {{
                        var api = {(object)options.JsonApi};
                        var i = $('#{options.UniqueId}');
                ");

            if (!string.IsNullOrWhiteSpace(action))
            {
                writer.Write($@"
                    {action} 
                    var createBtn = $('#{options.FormId}_create');
                    
                    createBtn.on('click', function(e) {{ 
                        e.preventDefault(); 
                        var selectedGroup = $('#{options.FormId}_group').val();
                        var isSummary = $('#{options.FormId}_summary').is(':checked');

                        triggerAction({{ 'action': 'create', group: selectedGroup, summary: isSummary }});
                    }});
                ");
            }

            writer.Write("});\r\n</script>");
        }

        private void ShowResults(TextWriter writer, IPropertyTemplateOptions options)
        {
            var deleteLabel = options.Property.Options["deleteLabel"] ?? "Delete/Cancel Report";
            var downloadLabel = options.Property.Options["downloadLabel"] ?? "Download Report";
            var resturl = (_callbackController != null) ? _callbackController.GetUrl() : "";

            // html
            writer.Write(
                $@"<div id='{options.FormId}' style='padding-top: 1rem;'>
                    <a href='{Filestore().GetFile("", "results.csv").GetDownloadUrl()}' target='_blank' id='{options.FormId}_download'>{downloadLabel}</a>
                    <br /><br />
                    <button type='button' class='button' id='{options.FormId}_delete'>{deleteLabel}</button>
                </div>
            ");

            var action = string.Empty;
            if (!string.IsNullOrWhiteSpace(resturl))
            {
                action = GetAction(resturl);
            }

            writer.Write($@"
                <script type='text/javascript'>
                    $(document).ready(function() {{
                        var api = {(object)options.JsonApi};
                        var i = $('#{options.UniqueId}');
                ");

            if (!string.IsNullOrWhiteSpace(action))
            {
                writer.Write($@"
                    {action} 
                    var deleteBtn = $('#{options.FormId}_delete');

                    deleteBtn.on('click', function(e) {{ 
                        e.preventDefault(); 
                        triggerAction({{ 'action': 'delete' }});
                    }});
                ");
            }

            writer.Write("});\r\n</script>");
        }

        private void ShowRunning(TextWriter writer, IPropertyTemplateOptions options)
        {
            var deleteLabel = options.Property.Options["deleteLabel"] ?? "Delete/Cancel Report";
            var refreshLabel = options.Property.Options["refreshLabel"] ?? "Refresh";
            var resturl = (_callbackController != null) ? _callbackController.GetUrl() : "";

            // html
            writer.Write(
                $@"<div id='{options.FormId}' style='padding-top: 1rem;'>
                    <b>Processing......this may take a few minutes depending on system load and volumes</b>
                    <br /><br />
                    <button type='button' class='button' style='margin-right:5px' id='{options.FormId}_reload'>{refreshLabel}</button>
                    <button type='button' class='button' id='{options.FormId}_delete'>{deleteLabel}</button>
                </div>
            ");

            var action = string.Empty;
            if (!string.IsNullOrWhiteSpace(resturl))
            {
                action = GetAction(resturl);
            }

            writer.Write($@"
                <script type='text/javascript'>
                    $(document).ready(function() {{
                        var api = {(object)options.JsonApi};
                        var i = $('#{options.UniqueId}');
                ");

            if (!string.IsNullOrWhiteSpace(action))
            {
                writer.Write($@"
                    {action} 
                    var deleteBtn = $('#{options.FormId}_delete');
                    var reloadBtn = $('#{options.FormId}_reload');

                    deleteBtn.on('click', function(e) {{ 
                        e.preventDefault(); 
                        triggerAction({{ 'action': 'delete' }});
                    }});

                    reloadBtn.on('click', function(e) {{ 
                        e.preventDefault(); 
                        location.reload();
                    }});
                ");
            }

            writer.Write("});\r\n</script>");
        }

        private static string GetAction(string resturl)
        {
            return $@"function triggerAction(data){{
                    $.telligent.evolution.post({{
                	    url : '{resturl}',
                	    data : data,
                        success: function(){{
                            location.reload();
                        }},
                        error : function(xhr, desc, ex) {{
                            $.telligent.evolution.notifications.show('Action request failed ' + desc, {{ type: 'error' }});
                        }}
                    }});
                }}";
        }
    }
}
