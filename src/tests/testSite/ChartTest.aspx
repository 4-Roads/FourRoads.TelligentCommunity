<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="ChartTest.aspx.cs" Inherits="_Default" %>

<%@ Register Assembly="FourRoads.Charting" Namespace="FourRoads.Charting.Controls"
	TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body id="AmChartTest">
    <form id="form1" runat="server">
		<cc1:AmChart ID="AmChart" runat="server" UseSWFObject="true"></cc1:AmChart>
		
		<cc1:AmChart ID="AmChartFileSettings" runat="server" UseSWFObject="true">
		</cc1:AmChart>
    <div>
    
    </div>
    </form>
</body>
</html>
