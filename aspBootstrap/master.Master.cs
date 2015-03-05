using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using crawlerWebService;

namespace aspBootstrap {
    public partial class master : System.Web.UI.MasterPage {
        protected void Page_Load(object sender, EventArgs e) {

        }

        // Header
        protected void dropdown_show(object sender, EventArgs e) {
            // get data
            float maxSeeds = 0;
            float seedsVisited = 0;
            try {
                maxSeeds = Crawler.getMaxSeeds(); 
                seedsVisited = maxSeeds - Crawler.getSeedsToVisit();
            } catch {
            }
            // Use the data 
            lblCrawlCompletionPercentage.Text = seedsVisited + "/" + maxSeeds + "seeds";
            // Trigger the dropdown via javascript
            Page.ClientScript.RegisterStartupScript(GetType(), "Show", "<script> $('#dropdownTriggerButton').click();</script>");
            // Set the progress bar width
            Page.ClientScript.RegisterStartupScript(GetType(), "Progress", "<script> $('#crawl1progress').attr('aria-valuenow','" + (seedsVisited/maxSeeds) * 100 + "%');</script>");
            if (maxSeeds <= 0)
                Page.ClientScript.RegisterStartupScript(GetType(), "changeBar", "<script> $('#crawl1progress').css('width', '" + 0 + "%');</script>");
            else
                Page.ClientScript.RegisterStartupScript(GetType(), "changeBar", "<script> $('#crawl1progress').css('width', '" + (seedsVisited/maxSeeds) * 100 + "%');</script>");
        }
    }
}