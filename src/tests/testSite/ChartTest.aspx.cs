using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using FourRoads.Charting.Components;
using FourRoads.Charting.Schema;

public partial class _Default : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
		settings lsettings = new settings();
		
		chart lchart = new chart();

		chartGraphsGraph[] graph = new chartGraphsGraph[1];
		graph[0] = new chartGraphsGraph();
		graph[0].gid = "1";
		graph[0].title = HttpUtility.UrlEncode("Test Graph %");
		graph[0].value = new chartGraphsGraphValue[2];
		graph[0].value[0] = new chartGraphsGraphValue ( );
		graph[0].value[0].start = 0;
		graph[0].value[0].Value = 92.2;
		graph[0].value[0].xid = "Question1";

		graph[0].value[1] = new chartGraphsGraphValue();
		graph[0].value[1].start = 0;
		graph[0].value[1].Value = -22.2;
		graph[0].value[1].xid = "Question2";

		lchart.graphs = graph;

		
		lchart.series = new chartValue[2];
		lchart.series[0] = new chartValue();
		lchart.series[1] = new chartValue();

		lchart.series[0].Value = "Question1";
		lchart.series[0].xid = "Question1";

		lchart.series[1].Value = "Question2";
		lchart.series[1].xid = "Question2";

		AmChart.DataSource = new ChartDataSource( lchart, lsettings );
		AmChart.DataBind();

		AmChartFileSettings.DataSource = new ChartDataSource(lchart, "/testSite/path/chartTestSettings.Xml");
		AmChart.DataBind();

	}
}
