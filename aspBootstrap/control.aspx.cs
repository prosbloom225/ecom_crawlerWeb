using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using crawlerWebService;
using System.Data;

namespace aspBootstrap {
    public partial class control : System.Web.UI.Page {
        // Crawl Controls
        protected void Page_Load(object sender, EventArgs e) {
        }
        public void startCrawl(object sender, EventArgs e) {
            string[] ret = Crawler.testCrawl();
            string output = "Crawler has been started.";
            output += "\n " + ret[0];
            output += "\n " + ret[1];
            output += "\n " + ret[2];
            lblModalTitle.Text = "Crawler Control";
            lblModalBody.Text = output;
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();
        }
        public void stopCrawl(object sender, EventArgs e) {
            //bool status =  Crawler.stopCrawl();
            Crawler.stopCrawl();
            bool status = Crawler.getStatusStoppedStarted();
            string[] ret = Crawler.getCrawlThreadInfo();
            string output;
            lblModalTitle.Text = "Crawler Control";
            if (status) {
                output = "Crawler has been stopped.";
                output += "\n " + ret[0];
                output += "\n " + ret[1];
                output += "\n " + ret[2];
                lblModalBody.Text = output;
            } else {
                output = "ERROR in stopping crawler.  Please see logs for more information.";
            }
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();
        }
        public void pauseCrawl(object sender, EventArgs e) {
            bool status =  Crawler.pauseCrawl();
            string[] ret = Crawler.getCrawlThreadInfo();
            string output;
            lblModalTitle.Text = "Crawler Control";
            if (status) {
                output = "Crawler has been paused.";
                output += "\n " + ret[0];
                output += "\n " + ret[1];
                output += "\n " + ret[2];
                lblModalBody.Text = output;
            } else {
                output = "ERROR in pausing crawler.  Please see logs for more information.";
            }
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();
        }
        public void resumeCrawl(object sender, EventArgs e) {
            Crawler.resumeCrawl();
            lblModalTitle.Text = "Crawler Control";
            lblModalBody.Text = "Crawler has been resumed.";
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();

        }

        // Manifest Controls
        public void generateManifest(object sender, EventArgs e) {
            // Dialog for completion
            lblModalTitle.Text = "Crawler Control";
            lblModalBody.Text = "Generate manifest for crawlID: ";
            ddlModal.Visible = true;
            btnModalAccept.Visible = true;
            btnModalAccept.Text = "Generate";
            DataSet ds = Crawler.getCrawlLog(10, true);
            List<string> dl = new List<string>();
            if (ds.Tables[0].Rows.Count >0)
                foreach (DataRow dr in ds.Tables[0].Rows) {
                    dl.Add(dr.ItemArray[0].ToString());
                }
            ddlModal.DataSource = dl;
            ddlModal.DataBind();

            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();


        }
        public void addDeptNums(object sender, EventArgs e) {
            // Dialog for completion
            lblModalTitle.Text = "Crawler Control";
            lblModalBody.Text = "Add dept numbers for crawlID: ";
            ddlModal.Visible = true;
            btnModalAccept.Visible = true;
            btnModalAccept.Text = "Add";
            DataSet ds = Crawler.getCrawlLog(10, true);
            List<string> dl = new List<string>();
            if (ds.Tables[0].Rows.Count >0)
                foreach (DataRow dr in ds.Tables[0].Rows) {
                    dl.Add(dr.ItemArray[0].ToString());
                }
            ddlModal.DataSource = dl;
            ddlModal.DataBind();

            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();

    }
        public void clickCallback(object sender, EventArgs e) {
            // Dialog for waiting 
            btnModalAccept.Visible = false;
            ddlModal.Visible = false;
            lblModalTitle.Text = "Crawler Control";
            lblModalBody.Text = "Running...";
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();

            if (((System.Web.UI.WebControls.Button)sender).Text == "Generate")
                generateManifestCallback();
            else if (((System.Web.UI.WebControls.Button)sender).Text == "Add")
                addDeptNumsCallback();
                // ERROR
        }

        public void generateManifestCallback() {
            int crawlID = Convert.ToInt32(ddlModal.SelectedValue.ToString());
            crawlerWebService.ManifestFactory.createManifest(crawlID);

            // Dialog for completion
            lblModalTitle.Text = "Crawler Control";
            lblModalBody.Text = "Manifest generated for crawlID: " + crawlID;
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();

        }
        public void addDeptNumsCallback() {
            int crawlID = Convert.ToInt32(ddlModal.SelectedValue.ToString());
            crawlerWebService.ManifestFactory.addDepartmentNumbers(crawlID);
            
            // Dialog for completion
            lblModalTitle.Text = "Crawler Control";
            lblModalBody.Text = "Dept Numbers added for crawlID: " + crawlID;
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "myModal", "$('#myModal').modal();", true);
            upModal.Update();
        }

    }
}