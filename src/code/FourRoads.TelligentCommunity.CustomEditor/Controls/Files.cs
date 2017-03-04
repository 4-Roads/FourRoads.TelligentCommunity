using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Storage.Version1;

namespace FourRoads.TelligentCommunity.CustomEditor.Controls
{

    public class Files : Control, INamingContainer, IPropertyControl
    {
        private ICentralizedFileStorageProvider fileStore = CentralizedFileStorage.GetFileStore("custom-editor-filestore");

        private ListBox list = new ListBox();
        private FileUpload uploader = new FileUpload();
        private Button delete = new Button();
        private Button upload = new Button();
        private Button download = new Button();

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Panel top = new Panel();
            Panel bottom = new Panel();
            
            list.Rows = 10;
            delete.Text = "Delete";
            delete.Command += Delete;
            upload.Text = "Upload";
            upload.Command += UploadOnClick;
            download.Text = "Download";
            download.Command += DownloadOnClick;

            top.Controls.Add(list);
            top.Controls.Add(delete);
            bottom.Controls.Add(uploader);
            bottom.Controls.Add(upload);
            bottom.Controls.Add(download);
            Controls.Add(top);
            Controls.Add(bottom);
        }

        protected void Delete(Object source, CommandEventArgs args)
        {
            ListItem item = list.SelectedItem;
            if(item != null)
            {
                String fname = item.Value;
                ICentralizedFile file = fileStore.GetFile("", fname);
                if (file != null)
                {
                    fileStore.Delete("", fname);
                    ListDataBind();
                }
            }
        }

        protected void ListDataBind()
        {
            if (fileStore == null)
            {
                //plugin is not enabled, bail out
                return;
            }
            list.Items.Clear();
            PathSearchOption search = new PathSearchOption();
            IEnumerable<ICentralizedFile> mune = fileStore.GetFiles(search);
            foreach (ICentralizedFile file in mune)
            {
                list.Items.Add(new ListItem(file.FileName));
            }
        }


        protected void UploadOnClick(Object source, CommandEventArgs args)
        {
            if(fileStore != null && uploader.HasFile)
            {
                string fname = uploader.FileName;
                fileStore.AddUpdateFile("", fname, uploader.FileContent);
                ListDataBind();
            }
        }

        protected void DownloadOnClick(Object source, CommandEventArgs args)
        {
            if(fileStore != null)
            {
                ListItem item = list.SelectedItem;
                if(item != null)
                {
                    ICentralizedFile file = fileStore.GetFile("", item.Value);
                    if (file != null)
                    {
                        HttpResponse response = HttpContext.Current.Response;
                        response.ClearContent();
                        response.AddHeader("Content-Disposition", "attachment; filename=" + file.FileName);
                        response.AddHeader("Content-Length", file.ContentLength.ToString());
                        response.ContentType = "text/plain";
                        using (Stream readStream = file.OpenReadStream())
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = readStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                response.BinaryWrite(buffer);
                            }
                        }
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                }
            }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EnsureChildControls();
            ListDataBind();
        }

        public void SetConfigurationPropertyValue(object value)
        {

        }

        public object GetConfigurationPropertyValue()
        {
            return null;
        }

        public Property ConfigurationProperty { get; set; }
        public ConfigurationDataBase ConfigurationData { get; set; }
        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public Control Control
        {
            get { return this; }
        }

    }

}
