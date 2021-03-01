using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using FourRoads.TelligentCommunity.Sentrus.Entities;
using FourRoads.TelligentCommunity.Sentrus.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using User = Telligent.Evolution.Extensibility.Api.Entities.Version1.User;

namespace FourRoads.TelligentCommunity.Sentrus.Controls
{
    public class InactiveUserManagementPropertyTemplate : IPropertyTemplate, INamingContainer, IHttpCallback
    {
        public string[] DataTypes => new string[] { "custom", "string" };

        public string TemplateName => "sentrus_inactiveUserManagement";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options
        {
            get
            {
                return null;
            }
        }

        public string Name => "4 Roads - Sentrus - Inactive User Management Template";

        public string Description => "Provides and interface for managing inactive users.";

        private IUserEncouragementAndMaintenance _userEncouragementAndMaintenancePlugin;
        private IHttpCallbackController _callbackController;
        private IUsers _users;
        private IUsers Users
        {
            get
            {
                if(_users == null)
                {
                    _users = Apis.Get<IUsers>();
                }

                return _users;
            }
        }
        private IEventLog _eventLog;
        private IEventLog EventLog
        {
            get
            {
                if (_eventLog == null)
                {
                    _eventLog = Apis.Get<IEventLog>();
                }

                return _eventLog;
            }
        }

        public void Initialize()
        {
            _userEncouragementAndMaintenancePlugin = PluginManager.GetSingleton<IUserEncouragementAndMaintenance>();
        }

        public void ProcessRequest(HttpContextBase httpContext)
        {
            httpContext.Response.ContentType = "text/javascript";

            if (httpContext.Request.Form["action"] != null 
                && httpContext.Request.Form["data"] != null
                && httpContext.Request.Form["id"] != null)
            {
                var action = httpContext.Request.Form["action"].ToString();
                var data = httpContext.Request.Form["data"].ToString();
                var uniqueId = httpContext.Request.Form["id"].ToString();

                if(action == "hide")
                {
                    Hide(httpContext, data);
                    UpdateRows(uniqueId, httpContext);
                }
                else if(action == "delete")
                {
                    Delete(httpContext, data);
                    UpdateRows(uniqueId, httpContext);
                }
                else
                {
                    httpContext.Response.Write($"$.telligent.evolution.notifications.show('Unsupported action {action}.', {{ type: 'error' }});");
                    httpContext.Response.StatusCode = 404;
                }
            }
            else
            {
                httpContext.Response.Write($"$.telligent.evolution.notifications.show('Unsupported action.', {{ type: 'error' }});");
                httpContext.Response.StatusCode = 404;
                return;
            }
        }

        public void SetController(IHttpCallbackController controller)
        {
            _callbackController = controller;
        }

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            var resturl = (_callbackController != null) ? _callbackController.GetUrl() : "";   
            var hideLabel = options.Property.Options["hidelabel"] ?? "Hide Selected Users";
            var deleteLabel = options.Property.Options["deletelabel"] ?? "Delete Selected Users";

            if (options.Property.Editable)
            {
                var rows = GetRows(options.UniqueId);

                var borderStyle = "border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc";
                writer.Write(
                    $@"<div id='{options.UniqueId}_container' style='height:250px;overflow-x:scroll;overflow-y:auto;min-width:500px'>
                        <table style='width:100%' cellspacing='0'>
                           <thead>
                              <tr style='font-weight: bold;'>
                                 <th style='width:5%;{borderStyle};border-left:solid 1px #cccccc;text-align:center'>
                                    <input id='{options.UniqueId}_selectAll' type='checkbox' name='{options.UniqueId}_selectAll'></th>
                                 <th style='width:25%;{borderStyle};'>User Name</th>
                                 <th style='width:40%;{borderStyle};'>Email</th>
                                 <th style='width:10%;{borderStyle};'>Posts</th>
                                 <th style='width:20%;{borderStyle};'>Last Activity</th>
                              </tr>
                            </thead>
                            <tbody>
                              {rows}
                           </tbody>
                        </table>
                        </div>
                        <div id='{options.UniqueId}_actions' style='padding-top: 1rem'>
                            <button type='button' class='button' id='{options.UniqueId}_hide'>{hideLabel}</a>
                            <button type='button' class='button' id='{options.UniqueId}_delete'>{deleteLabel}</a>
                        </div>
                        ");

                var action = string.Empty;
                if (!string.IsNullOrWhiteSpace(resturl))
                {
                    action = $@"function triggerAction(data){{
                                    $.telligent.evolution.post({{
                	                    url : '{resturl}',
                	                    data : data,
                                        success: function(){{
                                            selectAll.prop('checked', false);
                                            $('.{options.UniqueId}_rowSelector :checkbox').on('change', function(){{
                                                selectAll.prop('checked', $('.{options.UniqueId}_rowSelector :checkbox:checked').length === $('.{options.UniqueId}_rowSelector :checkbox').length);
                                            }});
                                        }},
                                        error : function(xhr, desc, ex) {{
                                            $.telligent.evolution.notifications.show('Action request failed ' + desc, {{ type: 'error' }});
                                        }}
                                    }});
                                }}";
                }

                writer.Write($@"
                    <script type='text/javascript'>
                            $(document).ready(function() {{
                                var api = {(object)options.JsonApi};
                                var selectAll = $('#{(object)options.UniqueId}_selectAll');
                                selectAll.on('change', function(){{
                                    $('.{options.UniqueId}_rowSelector :checkbox').prop('checked', selectAll.prop('checked'));
                                }});
                                
                                $('.{options.UniqueId}_rowSelector :checkbox').on('change', function(){{
                                    selectAll.prop('checked', $('.{options.UniqueId}_rowSelector :checkbox:checked').length === $('.{options.UniqueId}_rowSelector :checkbox').length);
                                }});
                ");

                if (!string.IsNullOrWhiteSpace(action))
                {
                    var assignTo = _userEncouragementAndMaintenancePlugin.AssignUserTo;
                    var confirmDeleteMessage = "Are you sure want to delete users without re-assignment?";

                    if (!string.IsNullOrWhiteSpace(assignTo) && Int32.TryParse(assignTo, out int userId))
                    {
                        var user = Users.Get(new UsersGetOptions() { Id = userId });
                        if (user != null)
                        {
                            confirmDeleteMessage = $"Delete users and assign to {user.Username}?";
                        }
                    }

                    writer.Write($@"
                        {action} 
                        var hideBtn = $('#{(object)options.UniqueId}_hide');
                        var deleteBtn = $('#{(object)options.UniqueId}_delete');
                        deleteBtn.on('click', function(e) {{ 
                            e.preventDefault();
                            var selectedUsers = $('.{options.UniqueId}_rowSelector :checkbox:checked').map(function(){{
                                return $(this).val();
                            }}).toArray();
                            if(selectedUsers.length === 0){{
                                $.telligent.evolution.notifications.show('No user selected.', {{ type: 'error' }});
                            }}
                            else {{
                                var deleteConfirmed = confirm('{confirmDeleteMessage}');
                                if (deleteConfirmed == true)
                                {{
                                    triggerAction({{ 'action': 'delete', data: selectedUsers.join(), id: '{options.UniqueId}' }});    
                                }}
                            }}
                        }});

                        hideBtn.on('click', function(e) {{ 
                            e.preventDefault(); 
                             var selectedUsers = $('.{options.UniqueId}_rowSelector :checkbox:checked').map(function(){{
                                return $(this).val();
                            }}).toArray();
                            if(selectedUsers.length === 0){{
                                $.telligent.evolution.notifications.show('No user selected.', {{ type: 'error' }});
                            }}
                            else {{
                                triggerAction({{ 'action': 'hide', data: selectedUsers.join(), id: '{options.UniqueId}' }});
                            }}
                        }});
                    ");
                }

                writer.Write("});\r\n</script>");
            }
        }

        private string GetRows(string uniqueId)
        {
            var borderStyle = "solid 1px #cccccc";
            var tdStyle = $"padding:3px;border-right:{borderStyle}";
            var maxCount = _userEncouragementAndMaintenancePlugin != null ? _userEncouragementAndMaintenancePlugin.MaxRows : 100;
            var users = GetUsers();
            var rows = new StringBuilder();

            foreach (var user in users)
            {
                maxCount--;
                if (maxCount < 0)
                    break;

                var userId = user.Id.GetValueOrDefault(0);
                var rowId = $"{uniqueId}_{userId}";
                rows.AppendLine(RowTemplate(uniqueId, user, tdStyle, borderStyle));
            }

            return rows.ToString();
        }

        private void UpdateRows(string uniqueId, HttpContextBase httpContext)
        {
            var borderStyle = "solid 1px #cccccc";
            var tdStyle = $"padding:3px;border-right:{borderStyle}";
            var maxCount = _userEncouragementAndMaintenancePlugin != null ? _userEncouragementAndMaintenancePlugin.MaxRows : 100;
            var users = GetUsers();

            httpContext.Response.Write($"$('#{uniqueId}_container table tbody').empty();");

            foreach (var user in users)
            {
                maxCount--;
                if (maxCount <= 0)
                    break;

                var userId = user.Id.GetValueOrDefault(0);
                var rowId = $"{uniqueId}_{userId}";
                var row = RowTemplate(uniqueId, user, tdStyle, borderStyle);
                if (!string.IsNullOrWhiteSpace(row))
                {
                    httpContext.Response.Write($"$('#{uniqueId}_container table tbody').append(\"{row}\");");
                }
            }
        }

        private IEnumerable<User> GetUsers()
        {
            var showHiddenUsers = _userEncouragementAndMaintenancePlugin != null
                ? _userEncouragementAndMaintenancePlugin.ShowHiddenUsers
                : false;
            var inactivityPeriod = _userEncouragementAndMaintenancePlugin != null
                ? _userEncouragementAndMaintenancePlugin.InactivityPeriod
                : -1;

            var rows = new StringBuilder();

            var users = Injector.Get<IUserHealth>().GetInactiveUsers(inactivityPeriod, showHiddenUsers);
            return users;
        }

        private string RowTemplate(string uniqueId, Telligent.Evolution.Extensibility.Api.Entities.Version1.User user, string tdStyle, string borderStyle)
        {
            var userId = user.Id.GetValueOrDefault(0);
            var rowId = $"{uniqueId}_{userId}";

            // needs to be single-line a multi-row breaks on js response
            return
                $@"<tr id='{rowId}_row' data-userid='{userId}'><td id='{rowId}_selected' style='{tdStyle};border-left:{borderStyle};border-right:{borderStyle};text-align:center;' class='{uniqueId}_rowSelector'><input id='{rowId}_rowSelector' type='checkbox' name='{rowId}_rowSelector' value='{userId}'></td><td id='{rowId}_username' style='{tdStyle};'><a href='{user.ProfileUrl}' target='_blank'>{user.Username}</a></td><td id='{rowId}_email' style='{tdStyle};'>{user.PrivateEmail}</td><td id='{rowId}_totalPosts' style='{tdStyle};'>{GetTotalPostsForUser(userId)}</td><td id='{rowId}_lastActivity' style='{tdStyle};'>{user.LastLoginDate.GetValueOrDefault(DateTime.MinValue).ToShortDateString()}</td></tr>";
        }

        private long GetTotalPostsForUser(int userId)
        {
            try
            {
                var totalPostsCmd =
                    string.Format(@"SELECT Count(1) FROM te_Content_ContentDetails WITH(NOLOCK) WHERE AuthorId = {0} ", userId);

                using (
                    SqlConnection connection =
                        new SqlConnection(ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(totalPostsCmd, connection))
                    {
                        connection.Open();

                        return Convert.ToInt64(command.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return -1;
            }
        }

        private void Delete(HttpContextBase httpContext, string userIdString)
        {
            if(string.IsNullOrWhiteSpace(userIdString))
            {
                return;
            }

            var userIds = userIdString.Split(',');
            foreach (var id in userIds)
            {
                if (Int32.TryParse(id, out int userId))
                {
                    try
                    {
                        var user = Users.Get(new UsersGetOptions() { Id = userId });
                        var assignedUser = Users.Get(new UsersGetOptions() { Username = "anonymous" });
                        if (!string.IsNullOrWhiteSpace(_userEncouragementAndMaintenancePlugin.AssignUserTo) &&
                            Int32.TryParse(_userEncouragementAndMaintenancePlugin.AssignUserTo, out int assignedUserId))
                        {
                            assignedUser = Users.Get(new UsersGetOptions() { Id = assignedUserId });
                        }

                        if (user != null)
                        {
                            Users.Delete(new UsersDeleteOptions() { Id = user.Id, Username = assignedUser.Username });
                        }
                        else
                        {
                            WriteToEventLog($"User with id: {userId} not found. No action done.", "Warning");
                        }
                    }
                    catch(Exception ex)
                    {
                        new TCException($"Error deleting user with id: {userId}. {ex.Message}", ex).Log();
                    }
                }
                else
                {
                    WriteToEventLog($"User id: {id} invalid. No action done.", "Warning");
                }
            }

            httpContext.Response.Write("$.telligent.evolution.notifications.show('Selected users deleted.', {type: 'success'});");
        }

        private void Hide(HttpContextBase httpContext, string userIdString)
        {
            if (string.IsNullOrWhiteSpace(userIdString))
            {
                return;
            }

            var userIds = userIdString.Split(',');
            foreach (var id in userIds)
            {
                if (Int32.TryParse(id, out int userId))
                {
                    try
                    {
                        var user = Users.Get(new UsersGetOptions() { Id = userId });
                        if (user.ContentId != null)
                        {
                            var userHealth = Injector.Get<IUserHealth>();
                            var lastLoginData = userHealth.GetLastLoginDetails(user.ContentId);

                            lastLoginData.IgnoredUser = true;

                            userHealth.CreateUpdateLastLoginDetails(lastLoginData);

                            Telligent.Evolution.Extensibility.Caching.Version1.CacheService.Remove(LastLoginDetails.CacheKey(user.ContentId), Telligent.Evolution.Extensibility.Caching.Version1.CacheScope.All);
                        }
                        else
                        {
                            WriteToEventLog($"User with id: {userId} not found. No action done.", "Warning");
                        }
                    }
                    catch(Exception ex)
                    {
                        new TCException($"Error hiding user with id: {userId}. {ex.Message}", ex).Log();
                    }
                }
            }

            httpContext.Response.Write("$.telligent.evolution.notifications.show('Selected users marked as hidden.', {type: 'success'});");
        }

        private void WriteToEventLog(string message, string eventType)
        {
            EventLog.Write(message, new EventLogEntryWriteOptions()
            {
                Category = "4 Roads - Sentrus - Inactive User Management",
                EventType = eventType
            });
        }
    }
}
