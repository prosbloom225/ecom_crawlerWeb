using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;
using crawlerWebService;
using System.Data;
using System.Net;

namespace aspBootstrap {
    public partial class tables : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if (!Page.IsPostBack) {
                DataSet ds = Crawler.getCrawlLog(10, true);
                List<string> dl = new List<string>();
                if (ds.Tables[0].Rows.Count > 0)
                    foreach (DataRow dr in ds.Tables[0].Rows) {
                        dl.Add(dr.ItemArray[0].ToString());
                    }
                ddlCrawlID.DataSource = dl;
                ddlCrawlID.DataBind();
            }

        }

        public void updateTable(object sender, EventArgs e) {
            if (Convert.ToInt32(ddlCrawlID.SelectedValue.ToString()) >= 0)
                ScriptManager.RegisterStartupScript(Page, Page.GetType(), "table", String.Format("table({0});", ddlCrawlID.SelectedValue.ToString()), true);
            }
    }
}