using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using crawlerWebService;
using System.Diagnostics;

namespace aspBootstrap {
    public partial class dashboard : System.Web.UI.Page {
        // Page methods
        protected void Page_Load(object sender, EventArgs e) {


            // Update Notifications Panel
            try {
                DataSet ds = Crawler.getCrawlLog(10, false);
                if (ds != null && ds.Tables.Count > 0) {
                    dgNotifications.DataSource = ds;
                    dgNotifications.DataBind();
                }
            } catch {
                // TODO - Add logging to asp pages
            }
            // Default to missing image graph
            if (!Page.IsPostBack)
                graphMissingImages(this, null);
        }
        protected void dashTimerTick(object sender, EventArgs e) {
            counterPanel.Update();
        }
        protected void counterPanelUpdate(object sender, EventArgs e) {
            TimeSpan ts = Crawler.getElapsed();
            lblElapsed.Text = ts.ToString();

            DateTime cacheExpiry = DateTime.Now.AddSeconds(10);

            string cacheMissingImageCount = Cache["missingImageCount"] as string;
            string cacheProductCount = Cache["productCount"] as string;
            string cachePagesView = Cache["pagesView"] as string;
            if (cacheMissingImageCount == null) {
                // Refresh cache
                cacheMissingImageCount = Crawler.getMissingImageCount().ToString();
                Cache.Insert("missingImageCount", cacheMissingImageCount, null, cacheExpiry, System.Web.Caching.Cache.NoSlidingExpiration);
            }
            if (cacheProductCount == null) {
                // Refresh cache
                cacheProductCount = Crawler.getProductsVisitedCount().ToString();
                Cache.Insert("productCount", cacheProductCount, null, cacheExpiry, System.Web.Caching.Cache.NoSlidingExpiration);
            }
            if (cachePagesView == null) {
                // Refresh cache
                cachePagesView = Crawler.getPagesVisitedCount().ToString();
                Cache.Insert("pagesView",  cachePagesView, null, cacheExpiry, System.Web.Caching.Cache.NoSlidingExpiration);
            }


            lblMissingImageCount.Text = cacheMissingImageCount;
            lblPagesViewed.Text = cachePagesView;
            lblProductCount.Text = cacheProductCount;
        }


        // Graph methods
        protected void graphPages(object sender, EventArgs e) {
            DateTime cacheExpiry = DateTime.Now.AddSeconds(15);
            lblGraphName.Text = "Pages Visited by CrawlID";

            DataTable dt = Cache["dtPagesGraph"] as DataTable;
            if (dt == null) {
                Cache.Insert("dtPagesGraph", Crawler.getGraphData("pages"), null, cacheExpiry, System.Web.Caching.Cache.NoSlidingExpiration);
                dt = Cache["dtPagesGraph"] as DataTable;
            }
            foreach (DataRow dr in dt.Rows) {
                ClientScript.RegisterArrayDeclaration("graphData", String.Format("{{y: '{0}', a: {1}}}", dr[0].ToString(), dr[1].ToString()));
            }

            Page.ClientScript.RegisterStartupScript(GetType(),
                "myKey",
                "Graph();",
                true);
            graphPanel.Update();
        }
        protected void graphProducts(object sender, EventArgs e) {
            DateTime cacheExpiry = DateTime.Now.AddSeconds(15);
            lblGraphName.Text = "Products Visited by CrawlID";

            DataTable dt = Cache["dtProductsGraph"] as DataTable;
            if (dt == null) {
                Cache.Insert("dtProductsGraph", Crawler.getGraphData("products"), null, cacheExpiry, System.Web.Caching.Cache.NoSlidingExpiration);
                dt = Cache["dtProductsGraph"] as DataTable;
            }
            foreach (DataRow dr in dt.Rows) {
                ClientScript.RegisterArrayDeclaration("graphData", String.Format("{{y: '{0}', a: {1}}}", dr[0].ToString(), dr[1].ToString()));
            }

            Page.ClientScript.RegisterStartupScript(GetType(),
                "myKey",
                "Graph();",
                true);
            graphPanel.Update();
        }
        protected void graphMissingImages(object sender, EventArgs e) {
            DateTime cacheExpiry = DateTime.Now.AddSeconds(15);
            lblGraphName.Text = "Missing Images by CrawlID";

            DataTable dt = Cache["dtMissingImageGraph"] as DataTable;
            if (dt == null) {
                Cache.Insert("dtMissingImageGraph", Crawler.getGraphData("missingImages"), null, cacheExpiry, System.Web.Caching.Cache.NoSlidingExpiration);
                dt = Cache["dtMissingImageGraph"] as DataTable;
            }
            foreach (DataRow dr in dt.Rows) {
                ClientScript.RegisterArrayDeclaration("graphData", String.Format("{{y: '{0}', a: {1}}}", dr[0].ToString(), dr[1].ToString()));
            }

            Page.ClientScript.RegisterStartupScript(GetType(),
                "myKey",
                "Graph();",
                true);
            graphPanel.Update();
        }

    }
}