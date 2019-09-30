// //------------------------------------------------------------------------------
// // <copyright company="4 Roads LTD">
// //     Copyright (c) 4 Roads LTD 2019.  All rights reserved.
// // </copyright>
// //------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.Evolution.Controls;

[assembly: WebResource("FourRoads.Common.TelligentCommunity.Controls.MultiSelectRepeater.js", "text/javascript", PerformSubstitution = true)]
namespace FourRoads.Common.TelligentCommunity.Controls
{
    public class MultiSelectRepeater : WrappedRepeater, INamingContainer
    {
        protected HiddenField _selectedItemIndexes;

        #region Events

        #endregion

        #region Public Properties

        public enum SELECTMODE
        {
            Single,
            Multi
        }

        [Bindable(true), Category("Behavior"), Description("Specifies that the form is posted when an item is selected.")]
        public bool AutoPostBack
        {
            get
            {
                object o = ViewState["AutoPostBack"];
                if (o == null)
                {
                    return false;
                }
                else
                {
                    return (bool) o;
                }
            }
            set { ViewState["AutoPostBack"] = value; }
        }

        [Bindable(true), Category("Appearance"), TypeConverter(typeof (EnumConverter)), Description("Controls if this control is a multi select control or single select.")]
        public SELECTMODE SelectMode
        {
            get
            {
                object o = ViewState["SelectMode"];
                if (o == null)
                {
                    return SELECTMODE.Single;
                }
                else
                {
                    return (SELECTMODE) o;
                }
            }
            set { ViewState["SelectMode"] = value; }
        }

        [Bindable(true), Category("Misc"), Description("Sets the server ID of the control that is reposible for row selection")]
        public string RowSelector
        {
            get
            {
                object o = ViewState["RowSelector"];
                if (o == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (string) o;
                }
            }
            set { ViewState["RowSelector"] = value; }
        }

        [Bindable(true), Category("Appearance"), Description("Specifies the css style when a row is highlighted when it is selected.")]
        public string RowSelectedCssClass
        {
            get
            {
                object o = ViewState["RowSelectedCssClass"];
                if (o == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (string) o;
                }
            }
            set { ViewState["RowSelectedCssClass"] = value; }
        }

        public string[] SelectedItems
        {
            get
            {
                EnsureChildControls();

                List<string> items = new List<string>();

                if (Page.Request.Form[_selectedItemIndexes.ID] != null)
                {
                    string[] selectedValues = Page.Request.Form[_selectedItemIndexes.ID].Split(new char[] {'^'});

                    foreach (string value in selectedValues)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            items.Add(value);
                        }
                    }

                    return items.ToArray();
                }

                return new string[] {};
            }
        }

        [Bindable(true), Category("Appearance"), Description("Specifies the css style when a row is highlighted when the mouse is over it.")]
        public string RowHighlightCssClass
        {
            get
            {
                object o = ViewState["RowHighlightCssClass"];
                if (o == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (string) o;
                }
            }
            set { ViewState["RowHighlightCssClass"] = value; }
        }


        [DefaultValue(""), Description("Specifies the DataItemID that is used for id binding.")]
        public string DataItemID
        {
            get
            {
                object o = ViewState["DataItemID"];
                if (o == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (string) o;
                }
            }
            set { ViewState["DataItemID"] = value; }
        }

        [DefaultValue(""), Description("Specifies the CommandName used in the server-side DataGrid event when the row is clicked.")]
        public string RowClickEventCommandName
        {
            get
            {
                object o = ViewState["RowClickEventCommandName"];
                if (o == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (string) o;
                }
            }
            set { ViewState["RowClickEventCommandName"] = value; }
        }

        [DefaultValue(""), Description("Specifies client script to be called when a row is selected.")]
        public string SelectedItemCallback
        {
            get
            {
                object o = ViewState["SelectedItemCallback"];
                if (o == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (string) o;
                }
            }
            set { ViewState["SelectedItemCallback"] = value; }
        }

        [DefaultValue(true), Description("Indicates whether or not rows are highlighted/clickable.")]
        public bool RowSelectionEnabled
        {
            get
            {
                object o = ViewState["RowSelectionEnabled"];
                if (o == null)
                {
                    return true;
                }
                else
                {
                    return (bool) o;
                }
            }
            set { ViewState["RowSelectionEnabled"] = value; }
        }

        #endregion

        #region Overridden DataGrid Methods

        protected override void RenderChildren(HtmlTextWriter writer)
        {
            base.RenderChildren(writer);

            _selectedItemIndexes.RenderControl(writer);
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(MultiSelectRepeater), "FourRoads.Common.TelligentCommunity.Controls.MultiSelectRepeater.js");

            if (_selectedItemIndexes == null)
            {
                //Create a hidden field that manages the list of selected items
                _selectedItemIndexes = Page.LoadControl(typeof (HiddenField), new object[] {}) as HiddenField;
                _selectedItemIndexes.ID = "HiddenSelectedItemList";

                Controls.Add(_selectedItemIndexes);
            }
        }

        protected override RepeaterItem CreateItem(int itemIndex, ListItemType itemType)
        {
            return new MultiSelectRepeaterItem(itemIndex, itemType);
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            if (!RowSelectionEnabled)
            {
                return; // exit if not RowSelectionEnabled == true
            }

            // add the click client-side event handler, if needed
            if (ChildControlsCreated && Controls.Count > 0)
            {
                List<string> rowObjects = CreateClickEvent();

                //Now build row object initialization script
                const string MultiselectDGManagerString = @"
					var {0} = new MultiselectDG_RowManager({1} , '{2}' , '{3}' , '{4}' , {5});
				";

                const string MultiselectDGRow = @"
					{0}.AddRow({1});
				";

                StringBuilder strBuilder = new StringBuilder(4000);


                strBuilder.AppendFormat(MultiselectDGManagerString, ClientID, (SelectMode == SELECTMODE.Multi) ? "true" : "false", _selectedItemIndexes.ClientID, RowHighlightCssClass, RowSelectedCssClass, !string.IsNullOrEmpty(SelectedItemCallback) ? SelectedItemCallback : "null");

                strBuilder.AppendLine();

                foreach (string row in rowObjects)
                {
                    strBuilder.AppendFormat(MultiselectDGRow, ClientID, row);
                    strBuilder.AppendLine();
                }


                CSControlUtility.Instance().RegisterStartupScript(this, typeof (MultiSelectRepeater), ClientID, strBuilder.ToString(), true);

            }
        }

        protected virtual List<string> CreateClickEvent()
        {
            List<string> rowObjects = new List<string>();

            foreach (MultiSelectRepeaterItem dgi in Items)
            {
                if (dgi.ItemType != ListItemType.Header && dgi.ItemType != ListItemType.Footer && dgi.ItemType != ListItemType.Pager)
                {
                    if (!string.IsNullOrEmpty(RowSelector))
                    {
                        IAttributeAccessor rowSelector = dgi.FindControl(RowSelector) as IAttributeAccessor;

                        if (rowSelector != null)
                        {
                            if (RowClickEventCommandName != string.Empty && AutoPostBack)
                            {
                                rowSelector.SetAttribute("onclick", Page.ClientScript.GetPostBackClientHyperlink(dgi, RowClickEventCommandName));
                            }
                            else if (RowClickEventCommandName != string.Empty)
                            {
                                rowSelector.SetAttribute("ondblclick", Page.ClientScript.GetPostBackClientHyperlink(dgi, RowClickEventCommandName));
                                rowSelector.SetAttribute("onclick", string.Format("javascript:{0}.SelectRow(event , {1});", ClientID, dgi.ItemIndex.ToString()));
                            }
                            else
                            {
                                rowSelector.SetAttribute("onclick", string.Format("javascript:{0}.SelectRow(event , {1});", ClientID, dgi.ItemIndex.ToString()));
                            }
                            rowSelector.SetAttribute("onmouseover", string.Format("javascript:{0}.MouseRowIn({1});", ClientID, dgi.ItemIndex.ToString()));
                            rowSelector.SetAttribute("onmouseout", string.Format("javascript:{0}.MouseRowOut({1});", ClientID, dgi.ItemIndex.ToString()));

                            if (DataSource != null && dgi.DataItemID == null)
                            {
                                dgi.DataItemID = DataBinder.Eval(((IList) DataSource)[dgi.ItemIndex], DataItemID);
                            }

                            rowObjects.Add(string.Format("new MultiselectDG_Row( '{0}' , '{1}' , '{2}' )", rowSelector.GetAttribute("class"), dgi.DataItemID, ((Control) rowSelector).ClientID));
                        }
                    }
                }
            }

            return rowObjects;
        }

        #endregion
    }


    public class MultiSelectRepeaterItem : RepeaterItem, IPostBackEventHandler
    {
        public MultiSelectRepeaterItem(int itemIndex, ListItemType itemType) : base(itemIndex, itemType)
        {
        }

        public object DataItemID
        {
            get { return ViewState["DataItemID"]; }
            set { ViewState["DataItemID"] = value; }
        }

        #region IPostBackEventHandler Members

        public void RaisePostBackEvent(string eventArgument)
        {
            CommandEventArgs commandArgs = new CommandEventArgs(eventArgument, DataItemID);
            RepeaterCommandEventArgs args = new RepeaterCommandEventArgs(this, this, commandArgs);
            base.RaiseBubbleEvent(this, args);
        }

        #endregion
    }
}