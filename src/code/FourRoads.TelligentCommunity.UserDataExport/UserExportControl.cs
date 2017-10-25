using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.UserDataExport
{
    public class UserExportControl : Control, IPropertyControl, INamingContainer
    {
        Button _deleteCurrentReport;
        Button _createReport;
        HyperLink _downloadReport;
        Literal _processing;
        Literal _helper;
        Literal _spacer;
        DropDownList _userGroupDropDownList;

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            if (_deleteCurrentReport == null)
            {
                _processing = new Literal()
                {
                    Text =
                        "<strong>Processing......this may take a few minutes depending on system load and volumes</strong><br>",
                    ID = "processing"
                };
                Controls.Add(_processing);

                _deleteCurrentReport = new Button() { ID = "deleteCurrentReport", Text = "Delete/Cancel Report" };
                _deleteCurrentReport.Click += _deleteCurrentReport_Click;
                Controls.Add(_deleteCurrentReport);

                _helper = new Literal() { Text = "Choose a group to extract or 'All Users' for all users<br>" };
                Controls.Add(_helper);

                _userGroupDropDownList = new DropDownList() { ID = "userGroupDropDownList", Text = "User Groups" };
                _userGroupDropDownList.Items.Add(new ListItem() { Text = "All Users", Value = "", Selected = true });

                GroupsListOptions list = new GroupsListOptions()
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
                                _userGroupDropDownList.Items.Add(new ListItem()
                                {
                                    Text = group.Name,
                                    Value = group.Id.ToString()
                                });
                            }
                        }
                    }
                }
                Controls.Add(_userGroupDropDownList);

                _spacer = new Literal() { Text = "<br><br>" };
                Controls.Add(_spacer);

                _createReport = new Button() { ID = "createReport", Text = "Create Report" };
                _createReport.Click += _createReport_Click;
                Controls.Add(_createReport);

                _downloadReport = new HyperLink() { ID = "downloadReport", Text = "Download Report" };
                _downloadReport.Target = "_blank";
                Controls.Add(_downloadReport);

                if (IsJobRunningOrScheduled())
                {
                    ShowRunning();
                }
                else if (ResultsExist())
                {
                    ShowResults();
                }
                else
                {
                    ShowChoices();
                }
            }
        }

        void _downloadReport_Click(object sender, EventArgs e)
        {
            var file = Filestore().GetFile("", "results.csv");

            if (file != null)
            {
                var response = HttpContext.Current.Response;

                response.Clear();
                response.AddHeader("Content-Disposition", "attachment; filename=" + file.FileName);
                response.AddHeader("Content-Length", file.ContentLength.ToString());
                response.ContentType = "application/octet-stream";

                using (var fs = file.OpenReadStream())
                {
                    byte[] buffer = new byte[file.ContentLength];
                    fs.Read(buffer, 0, file.ContentLength);

                    response.Write(buffer);

                    response.End();
                }
            }
            else
            {
                //Clean up the files as there must be one left over
                CleanUpFiles();

                ShowChoices();
            }
        }

        private void CleanUpFiles()
        {
            foreach (var file in Filestore().GetFiles(PathSearchOption.AllPaths))
            {
                Filestore().Delete(file.Path, file.FileName);
            }
        }

        void _createReport_Click(object sender, EventArgs e)
        {
            if (Filestore().GetFile("", "processing.txt") == null)
            {
                Filestore().AddUpdateFile("", "processing.txt", new MemoryStream() { });
                Apis.Get<IJobService>().Schedule(typeof(UserExportJob), DateTime.Now.ToUniversalTime(), new Dictionary<string, string>() { { "groupId", _userGroupDropDownList.SelectedValue } });
            }

            ShowRunning();
        }

        void _deleteCurrentReport_Click(object sender, EventArgs e)
        {
            CleanUpFiles();

            ShowChoices();
        }

        private bool ResultsExist()
        {
            return (Filestore().GetFile("", "results.csv") != null);
        }

        private static Telligent.Evolution.Extensibility.Storage.Version1.ICentralizedFileStorageProvider Filestore()
        {
            return Telligent.Evolution.Extensibility.Storage.Version1.CentralizedFileStorage.GetFileStore(UserExportPlugin.FILESTORE_KEY);
        }

        private bool IsJobRunningOrScheduled()
        {
            return Filestore().GetFile("", "processing.txt") != null;
        }

        private void ShowChoices()
        {
            _deleteCurrentReport.Visible = false;
            _userGroupDropDownList.Visible = true;
            _helper.Visible = true;
            _spacer.Visible = true;
            _createReport.Visible = true;
            _downloadReport.Visible = false;
            _processing.Visible = false;
        }

        private void ShowResults()
        {
            _deleteCurrentReport.Visible = true;
            _userGroupDropDownList.Visible = false;
            _helper.Visible = false;
            _spacer.Visible = false;
            _createReport.Visible = false;
            _downloadReport.Visible = true;
            _processing.Visible = false;

            _downloadReport.NavigateUrl = Filestore().GetFile("", "results.csv").GetDownloadUrl();

        }

        private void ShowRunning()
        {
            _deleteCurrentReport.Visible = true;
            _userGroupDropDownList.Visible = false;
            _helper.Visible = false;
            _spacer.Visible = false;
            _createReport.Visible = false;
            _downloadReport.Visible = false;
            _processing.Visible = true;
        }

        public ConfigurationDataBase ConfigurationData
        {
            get
            {
                return null;
            }
            set
            {

            }
        }

        public Property ConfigurationProperty
        {
            get
            {
                return null;
            }
            set
            {

            }
        }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public Control Control
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
