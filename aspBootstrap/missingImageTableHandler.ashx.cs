using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using crawlerWebService;

namespace aspBootstrap {
    public class missingImageTableHandler : IHttpHandler {

        public void ProcessRequest(HttpContext context) {
            string searchKey = "";
            for (int i = 0; i < context.Request.QueryString.Keys.Count-1; i++) {
                searchKey = searchKey + context.Request.QueryString.GetValues(i)[0];
            }
            context.Response.ContentType = "application/json";
            JavaScriptSerializer _serializer = new JavaScriptSerializer();
            if (searchKey != "") {
                var missingImages = Crawler.getMissingImageManifestEnumerable(Convert.ToInt32(searchKey));
                var result = new {
                    iTotalRecords = missingImages.Count(),
                    iTotalDisplayRecords = missingImages.Count(),
                    aData = missingImages.Select(p => new object[] {p.productId, p.dept, p.imageName, p.type, p.color, p.url, p.nav_from })
                };
            context.Response.Write(_serializer.Serialize(result));
            }
        }

        public bool IsReusable {
            get {
                return false;
            }
        }
    }
}