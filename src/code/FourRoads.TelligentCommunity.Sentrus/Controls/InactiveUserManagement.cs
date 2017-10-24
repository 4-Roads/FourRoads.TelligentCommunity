using System.Configuration;
using System.Data.SqlClient;
using FourRoads.Common.TelligentCommunity.Plugins.Base;

namespace FourRoads.TelligentCommunity.Sentrus.Controls
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web.UI;
    using System.Web.UI.HtmlControls;
    using System.Web.UI.WebControls;
    using System.Xml;
    using System.Xml.XPath;
    using Interfaces;
    using Telligent.DynamicConfiguration.Components;
    using Telligent.Evolution.Controls.Extensions;
    using Telligent.Evolution.Extensibility.Api.Entities.Version1;
    using Telligent.Evolution.Extensibility.Api.Version1;
    using Telligent.Evolution.Extensibility.Version1;
    using Common.TelligentCommunity.Controls;
    using FourRoads.TelligentCommunity.Sentrus.Entities;


    public class InactiveUserItemTemplate : ITemplate
    {
        private ListItemType _type;

        public InactiveUserItemTemplate(ListItemType type)
        {
            _type = type;
        }

        public void InstantiateIn(Control container)
        {
            switch (_type)
            {
                case ListItemType.Header:
                    container.Controls.Add(new LiteralControl("<div style=\"height:250px;overflow-x:scroll;overflow-y:auto;min-width:500px\"><table style=\"width:100%\" cellspacing=\"0\">"));

                    var li = new HtmlGenericControl("tr");
                    container.Controls.Add(li);
                    li.Attributes.Add("style", "font-weight: bold;");

                    var selected = new HtmlGenericControl("td") { InnerText = "" };
                    selected.Attributes.Add("style", "border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;border-left:solid 1px #cccccc;;border-right:solid 1px #cccccc;");
                    li.Controls.Add(selected);
                    CheckBox selectAll = new CheckBox() {ID = "selectAll"};
                    selected.Controls.Add(selectAll);

                    var userName = new HtmlGenericControl("td") { InnerText = "User Name" };
                    userName.Attributes.Add("style", "width:10em;border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(userName);

                    var email = new HtmlGenericControl("td") { InnerText = "Email" };
                    email.Attributes.Add("style", "width:20em;border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(email);

                    var totalPosts = new HtmlGenericControl("td") { InnerText = "Posts" };
                    totalPosts.Attributes.Add("style", "width:4em;border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(totalPosts);

                    var lastActivity = new HtmlGenericControl("td") { InnerText = "Last Activity" };
                    lastActivity.Attributes.Add("style", "width:20em;border-bottom:solid 1px #cccccc;border-right:solid 1px #cccccc;");
                    li.Controls.Add(lastActivity);
                    break;

                case ListItemType.Footer:
                    container.Controls.Add(new LiteralControl("</table></div>"));
                    break;

                case ListItemType.Item:
                case ListItemType.AlternatingItem:
                    li = new HtmlGenericControl("tr") { ID = "row" };
                    container.Controls.Add(li);

                    if (ListItemType.AlternatingItem == _type)
                    {
                        li.Attributes.Add("style", "background-color:#eeeeee;");
                    }

                    selected = new HtmlGenericControl("td") { ID = "selected" };
                    selected.Attributes.Add("style", "padding:3px;border-right:solid 1px #cccccc;border-left:solid 1px #cccccc;;border-right:solid 1px #cccccc;");
                    li.Controls.Add(selected);

                    CheckBox checkBox = new CheckBox() { ID = "rowSelector", CssClass = "rowSelector" };
                    selected.Controls.Add(checkBox);

                    userName = new HtmlGenericControl("td") { ID = "username" };
                    userName.Attributes.Add("style", "padding:3px;width:10em;border-right:solid 1px #cccccc;");
                    li.Controls.Add(userName);

                    email = new HtmlGenericControl("td") { ID = "email" };
                    email.Attributes.Add("style", "padding:3px;border-right:solid 1px #cccccc;");
                    li.Controls.Add(email);

                    totalPosts = new HtmlGenericControl("td") { ID = "totalPosts" };
                    totalPosts.Attributes.Add("style", "padding:3px;border-right:solid 1px #cccccc;");
                    li.Controls.Add(totalPosts);

                    lastActivity = new HtmlGenericControl("td") { ID = "lastActivity" };
                    lastActivity.Attributes.Add("style", "padding:3px;border-right:solid 1px #cccccc;");
                    li.Controls.Add(lastActivity);
                    break;
            }

            container.DataBinding += ContainerOnDataBinding;
        }

        private void ContainerOnDataBinding(object sender, EventArgs eventArgs)
        {
            var item = sender as RepeaterItem;
            if (item != null)
            {
                if (item.ItemType == ListItemType.Header)
                {
                    var selectAll = item.FindControl("selectAll") as CheckBox;

                    if (selectAll != null)
                    {
                        ((Control) sender).Page.ClientScript.RegisterClientScriptBlock(GetType(), "SelectAll", string.Format(@"
                                                                            $(function(){{
                                                                                 $('#{0}').change(function(){{
                                                                                      $('.rowSelector :checkbox').prop('checked' ,$('#{0}').prop('checked'));
                                                                                 }});
                                                                             }});", selectAll.ClientID), true);
                    }
                }

                if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
                {
                    IXPathNavigable nav = item.DataItem as IXPathNavigable;

                    if (nav != null)
                    {
                        var navigator = nav.CreateNavigator();

                        if (navigator != null)
                        {
                            var row = item.FindControl("row") as HtmlGenericControl;
                            var username = item.FindControl("username") as HtmlGenericControl;
                            var email = item.FindControl("email") as HtmlGenericControl;
                            var lastActivity = item.FindControl("lastActivity") as HtmlGenericControl;
                            var totalposts = item.FindControl("totalposts") as HtmlGenericControl;

                            row.Attributes.Add("data-userid", navigator.GetAttribute("id", string.Empty));
                            username.InnerHtml = string.Format("<a href='{0}' target='_blank'>{1}</a>", navigator.GetAttribute("profile", string.Empty), navigator.GetAttribute("username", string.Empty));
                            email.InnerText = navigator.GetAttribute("email", string.Empty);
                            totalposts.InnerText = navigator.GetAttribute("totalPosts", string.Empty);
                            lastActivity.InnerText = navigator.GetAttribute("lastActivity", string.Empty);
                        }
                    }
                }
            }
        }
    }

    public class InactiveUserManagement : Control, INamingContainer, IPropertyControl
    {
        private Repeater _repeater;
        private XmlDataSource _configurationDataSource;
        private ApiSafeUserLookup _userToAssignTo;
        private Button _delete;
        private Button _hideUsers;
        private CheckBox _showHiddenUsers;

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            if (_repeater == null)
            {
                _repeater = new Repeater();
                _repeater.ID = "repeater";
                _repeater.ViewStateMode = ViewStateMode.Disabled;

                Controls.Add(_repeater);

                _userToAssignTo = new ApiSafeUserLookup() { ID = "UserToAssignTo" };
                _userToAssignTo.IncludeSystemAccounts = true;

                _delete = new Button() {ID = "Delete" , Text = "Delete Selected Users"};

                HtmlTable table = new HtmlTable();
                HtmlTableRow row = new HtmlTableRow();
                HtmlTableCell assignUserCell = new HtmlTableCell();
                HtmlTableCell deleteCell = new HtmlTableCell();

                Controls.Add(table);
                row.Style.Add("vertical-align" , "bottom");
                table.Rows.Add(row);
                row.Cells.Add(assignUserCell);
                row.Cells.Add(deleteCell);
                assignUserCell.Controls.Add(new Literal() { Text = "<strong>Assign Deleted User To:</strong>" });
                assignUserCell.Controls.Add(_userToAssignTo);
                deleteCell.Controls.Add(_delete);

                _delete.Click += DeleteOnClick;
                _delete.OnClientClick = "return confirm('Are you sure you want to delete the selected users, this can not be undone');";

                _hideUsers = new Button() { ID = "HideUser", Text = "Hide Selected Users" };
                _hideUsers.Click += HideUsersOnClick;
                     
                row = new HtmlTableRow();
                HtmlTableCell hideUser = new HtmlTableCell();
                hideUser.Controls.Add(_hideUsers);
                table.Rows.Add(row);
                row.Cells.Add(hideUser);

                _showHiddenUsers = new CheckBox(){ID = "ShowHidden" , Text = "Show Hidden Users"};
                _showHiddenUsers.AutoPostBack = true;
                _showHiddenUsers.CheckedChanged += ShowHiddenUsersOnCheckedChanged;
                HtmlTableCell showCheck = new HtmlTableCell();
                showCheck.Controls.Add(_showHiddenUsers);
                row.Cells.Add(showCheck);
            }
        }

        private void ShowHiddenUsersOnCheckedChanged(object sender, EventArgs eventArgs)
        {
            //Set up the grid 
            SetConfigurationPropertyValue(null);
            _repeater.DataSource = _configurationDataSource;
            _repeater.DataBind();
        }


        private void HideUsersOnClick(object sender, EventArgs eventArgs)
        {
            IUserHealth userHealth = Injector.Get<IUserHealth>();
            EnumerateUsers(user =>
            {
                if (user.ContentId != null)
                {
                    var lastLoginData = userHealth.GetLastLoginDetails(user.ContentId);

                    lastLoginData.IgnoredUser = true;

                    userHealth.CreateUpdateLastLoginDetails(lastLoginData);

                    Telligent.Evolution.Extensibility.Caching.Version1.CacheService.Remove(LastLoginDetails.CacheKey(user.ContentId), Telligent.Evolution.Extensibility.Caching.Version1.CacheScope.All);
                }
            });

            //Set up the grid 
            SetConfigurationPropertyValue(null);
            _repeater.DataSource = _configurationDataSource;
            _repeater.DataBind();
        }

        private void EnumerateUsers(Action<User> userAction)
        {
            foreach (RepeaterItem item in _repeater.Items)
            {
                CheckBox rowSel = item.FindControl("rowSelector") as CheckBox;
                var row = item.FindControl("row") as HtmlGenericControl;
                if (row != null && rowSel != null && rowSel.Checked)
                {
                    int userId = Convert.ToInt32(row.Attributes["data-userid"]);

                    User user = PublicApi.Users.Get(new UsersGetOptions() { Id = userId });

                    if (user != null)
                    {
                        userAction(user);
                    }
                }
            }

        }

        private void DeleteOnClick(object sender, EventArgs eventArgs)
        {
            EnumerateUsers(user =>
            {
                IEnumerable<User> selectedUsers = _userToAssignTo.SelectedUsers;

                User assgnedUser = null;
                if (selectedUsers.Count() > 0)
                {
                    assgnedUser = PublicApi.Users.Get(new UsersGetOptions() { Id = _userToAssignTo.SelectedUsers.First().Id });
                }

                if (assgnedUser == null)
                {
                    assgnedUser = PublicApi.Users.Get(new UsersGetOptions() { Username = "anonymous" });
                }

                PublicApi.Users.Delete(new UsersDeleteOptions() { Id = user.Id, Username = assgnedUser.Username });
            });

            //Set up the grid 
            SetConfigurationPropertyValue(null);
            _repeater.DataSource = _configurationDataSource;
            _repeater.DataBind();
        }


        protected override void OnInit(EventArgs e)
        {
            EnsureChildControls();

            base.OnInit(e);

            _repeater.HeaderTemplate = new InactiveUserItemTemplate(ListItemType.Header);
            _repeater.ItemTemplate = new InactiveUserItemTemplate(ListItemType.Item);
            _repeater.AlternatingItemTemplate = new InactiveUserItemTemplate(ListItemType.AlternatingItem);
            _repeater.FooterTemplate = new InactiveUserItemTemplate(ListItemType.Footer);


            Page.ClientScript.RegisterClientScriptBlock(GetType(), "Insert",string.Format( @"
                                                                            $(function(){{
                                                                                $('.CommonFormFieldName' , $('#{0}').closest('table')).remove();
                                                                             }});", this.ClientID + "_container"), true);
        }

        protected override void Render(HtmlTextWriter writer)
        {
            writer.WriteBeginTag("div");
            writer.WriteAttribute("id", ClientID + "_container");

            base.Render(writer);

            writer.WriteEndTag("div");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            SetConfigurationPropertyValue(null);

            //Set up the grid 
            _repeater.DataSource = _configurationDataSource;
            _repeater.DataBind();
        }


        private void CreateDataSource(string data)
        {
            _configurationDataSource = new XmlDataSource();
            _configurationDataSource.EnableCaching = false;
            _configurationDataSource.Data = data;
            ViewState["SourceData"] = data;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            //Do the query here for out of date users
            StringBuilder sb = new StringBuilder(20000);

            using (StringWriter sw = new StringWriter(sb)){
                using (XmlTextWriter wr = new XmlTextWriter(sw))
                {
                    wr.WriteStartDocument();
                    wr.WriteStartElement("users");
                    int maxCount = 100;
                    int inactivityPeriod = PluginManager.GetSingleton<IUserEncouragementAndMaintenance>() != null
                        ? PluginManager.GetSingleton<IUserEncouragementAndMaintenance>().InactivityPeriod
                        : -1;
                    foreach (User user in Injector.Get<IUserHealth>().GetInactiveUsers(inactivityPeriod, _showHiddenUsers.Checked))
                    {
                        maxCount--;
                        if (maxCount <= 0)
                            break;

                        wr.WriteStartElement("user");

                        wr.WriteStartAttribute("id");
                        wr.WriteValue(user.Id.GetValueOrDefault(0));
                        wr.WriteEndAttribute();

                        wr.WriteStartAttribute("username");
                        wr.WriteValue(user.Username);
                        wr.WriteEndAttribute();

                        wr.WriteStartAttribute("email");
                        wr.WriteValue(user.PrivateEmail);
                        wr.WriteEndAttribute();

                        wr.WriteStartAttribute("profile");
                        wr.WriteValue(user.ProfileUrl);
                        wr.WriteEndAttribute();

                        wr.WriteStartAttribute("totalPosts");
                        wr.WriteValue(GetTotalPostsForUser(user.Id.GetValueOrDefault()));
                        wr.WriteEndAttribute();

                        wr.WriteStartAttribute("lastActivity");
                        wr.WriteValue(user.LastLoginDate.GetValueOrDefault(DateTime.MinValue).ToShortDateString());
                        wr.WriteEndAttribute();

                        wr.WriteEndElement();
                    }
                    wr.WriteEndElement();
                }
            }

            CreateDataSource(sb.ToString());
        }

        private long GetTotalPostsForUser(int userId)
        {
            try
            {
                string totalPostsCmd =
                    string.Format(@"SELECT Count(*) FROM te_Content_ContentDetails WHERE AuthorId = {0} ", userId);

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

        public object GetConfigurationPropertyValue()
        {
            return string.Empty;
        }

        public Property ConfigurationProperty { get; set; }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public Control Control
        {
            get { return this; }
        }
  
        public event ConfigurationPropertyChanged ConfigurationValueChanged;
    }

    public class TestSettingButton : Button, IPropertyControl
    {
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            //do some stuff
            var plg = PluginManager.GetSingleton<IUserEncouragementAndMaintenance>();
            
            if (plg != null)
                plg.TestSettings();

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Text = "Test Currently Saved Settings";
        }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public Property ConfigurationProperty
        { get; set; }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public new Control Control
        {
            get { return this; }
        }

        public object GetConfigurationPropertyValue()
        {
            return null;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            
        }
    }
}
