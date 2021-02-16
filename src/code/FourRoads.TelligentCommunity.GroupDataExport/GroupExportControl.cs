using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.GroupDataExport
{
    public class GroupExportControl : Control, IPropertyControl, INamingContainer
    {
        Button _deleteCurrentReport;
        Button _createReport;
        HyperLink _downloadReport;
        Literal _processing;
        Literal _helper;
        Literal _spacer;
        Literal _spacer2;
        DropDownList _groupDropDownList;
        CheckBox _summaryReport;


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

                _helper = new Literal() { Text = "Choose a group to extract or 'All Groups' for all groups<br>" };
                Controls.Add(_helper);

                _groupDropDownList = new DropDownList() { ID = "groupDropDownList", Text = "Groups" };
                _groupDropDownList.Items.Add(new ListItem() { Text = "All Groups", Value = "", Selected = true });

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
                                _groupDropDownList.Items.Add(new ListItem()
                                {
                                    Text = group.Name,
                                    Value = group.Id.ToString()
                                });
                            }
                        }
                    }
                }
                Controls.Add(_groupDropDownList);

                _spacer = new Literal() { Text = "<br><br>" };
                Controls.Add(_spacer);

                _summaryReport = new CheckBox() { ID = "summaryReport", Text = "Summary Report" };
                Controls.Add(_summaryReport);

                _spacer2 = new Literal() { Text = "<br><br>" };
                Controls.Add(_spacer2);

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
                    using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        response.Write(reader.ReadToEnd());
                    }

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
                Apis.Get<IJobService>().Schedule(typeof(GroupExportJob), DateTime.Now.ToUniversalTime(), new Dictionary<string, string>() { { "groupId", _groupDropDownList.SelectedValue }, { "summary", _summaryReport.Checked.ToString() } });
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
            return Telligent.Evolution.Extensibility.Storage.Version1.CentralizedFileStorage.GetFileStore(GroupExportPlugin.FILESTORE_KEY);
        }

        private bool IsJobRunningOrScheduled()
        {
            return Filestore().GetFile("", "processing.txt") != null;
        }

        private void ShowChoices()
        {
            _deleteCurrentReport.Visible = false;
            _groupDropDownList.Visible = true;
            _summaryReport.Visible = true;
            _helper.Visible = true;
            _spacer.Visible = true;
            _spacer2.Visible = true;
            _createReport.Visible = true;
            _downloadReport.Visible = false;
            _processing.Visible = false;
        }

        private void ShowResults()
        {
            _deleteCurrentReport.Visible = true;
            _groupDropDownList.Visible = false;
            _summaryReport.Visible = false;
            _helper.Visible = false;
            _spacer.Visible = false;
            _spacer2.Visible = false;
            _createReport.Visible = false;
            _downloadReport.Visible = true;
            _processing.Visible = false;

            _downloadReport.NavigateUrl = Filestore().GetFile("", "results.csv").GetDownloadUrl();

        }

        private void ShowRunning()
        {
            _deleteCurrentReport.Visible = true;
            _groupDropDownList.Visible = false;
            _summaryReport.Visible = false;
            _helper.Visible = false;
            _spacer.Visible = false;
            _spacer2.Visible = false;
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
