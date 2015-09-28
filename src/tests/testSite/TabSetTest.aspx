<%@ Page Language="C#" AutoEventWireup="true" CodeFile="TabSetTest.aspx.cs" Inherits="TabSetTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <table width="100%">
            <tr>
                <td>
                    <h4>Basic TabSet</h4>
                    <FRControl:TabSet ID="TabSet1" runat="server">
                        <FRControl:Tab runat="server" Text="Tab 1">
                            <ContentTemplate>
                                This is the content for tab 1
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab runat="server" Text="Tab 2">
                            <ContentTemplate>
                                This is the content for tab 2
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab runat="server" Text="Tab 3">
                            <ContentTemplate>
                                This is the content for tab 3
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab runat="server" Text="Tab 4">
                            <ContentTemplate>
                                This is the content for tab 4
                            </ContentTemplate>
                        </FRControl:Tab>
                    </FRControl:TabSet> 
                </td>
                <td>
                    <h4>AutoRotating TabSet</h4>
                    <FRControl:TabSet ID="TabSet2" runat="server" RotateInterval="3000" TabLocation="bottom"  >
                        <FRControl:Tab ID="Tab1" runat="server" Text="Tab 1">
                            <ContentTemplate>
                                This is the content for tab 1
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab2" runat="server" Text="Tab 2">
                            <ContentTemplate>
                                This is the content for tab 2
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab3" runat="server" Text="Tab 3">
                            <ContentTemplate>
                                This is the content for tab 3
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab4" runat="server" Text="Tab 4">
                            <ContentTemplate>
                                This is the content for tab 4
                            </ContentTemplate>
                        </FRControl:Tab>
                    </FRControl:TabSet> 
                </td>
                
            </tr>
            <tr>
                <td>
                    <h4>Tabs on Left TabSet</h4>
                    <FRControl:TabSet ID="TabSet3" runat="server" TabLocation="Left" >
                        <FRControl:Tab ID="Tab5" runat="server" Text="Tab 1">
                            <ContentTemplate>
                                This is the content for tab 1
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab6" runat="server" Text="Tab 2">
                            <ContentTemplate>
                                This is the content for tab 2
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab7" runat="server" Text="Tab 3">
                            <ContentTemplate>
                                This is the content for tab 3
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab8" runat="server" Text="Tab 4">
                            <ContentTemplate>
                                This is the content for tab 4
                            </ContentTemplate>
                        </FRControl:Tab>
                    </FRControl:TabSet> 
                </td>
                <td>
                    <h4>Tabs on Right TabSet</h4>
                    <FRControl:TabSet ID="TabSet4" runat="server" TabLocation="Right"  >
                        <FRControl:Tab ID="Tab9" runat="server" Text="Tab 1">
                            <ContentTemplate>
                                This is the content for tab 1
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab10" runat="server" Text="Tab 2">
                            <ContentTemplate>
                                This is the content for tab 2
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab11" runat="server" Text="Tab 3">
                            <ContentTemplate>
                                This is the content for tab 3
                            </ContentTemplate>
                        </FRControl:Tab>
                        <FRControl:Tab ID="Tab12" runat="server" Text="Tab 4">
                            <ContentTemplate>
                                This is the content for tab 4
                            </ContentTemplate>
                        </FRControl:Tab>
                    </FRControl:TabSet> 
                </td>
                
            </tr>
        </table> 
        
    </div>
    </form>
</body>
</html>
