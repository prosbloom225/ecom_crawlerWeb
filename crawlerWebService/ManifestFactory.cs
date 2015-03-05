using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using log4net.Appender;
using System.IO;

namespace crawlerWebService{
    public class ManifestFactory {
        private static OracleConnection con;
        private static log4net.ILog logger = log4net.LogManager.GetLogger("Main");


        public static bool createManifest(int CrawlID) {
            using (DataSet ds = Crawler.getMissingImages(CrawlID)){
                List<string[]>storage = new List<string[]>();
                foreach (DataTable table in ds.Tables) {
                    foreach (DataRow dr in table.Rows) {
                        // Lets store these and batch the query to fill data
                        storage.Add(new string[]{dr[0].ToString(), dr[1].ToString()});
                    }
                    // storage is ready, lets populate the data we already have
                    List<img> imageList = createMissingImageList(storage.ToArray(), CrawlID);
                    // DB Stuff
                    // First clear out any manifest created for this crawlID
                    crawlerWebService.Crawler.removeManifest(CrawlID);
                    // Now insert it into the db
                    crawlerWebService.Crawler.insertManifest(imageList, CrawlID);


                }
                // All ok 
                return true;
            }

            // Default return
            return false;

        }

        public static List<img> createMissingImageList(object[] errorText, int crawlID) {

            // If we weren't passed any data, don't waste any time 
            if (errorText == null) {
                return null;
            }

            List<img> icsList = new List<img>();
            //string[] elements = new string[0];
            string temp;
            string imageName;




            foreach (string[] elements in errorText) {
                // constraints
                if (elements[0].Length <= 0)
                    break;
                if (elements[1].Contains("catalog.jsp"))
                    continue;
                try {
                    imageName = elements[0].Trim().Substring(elements[0].Trim().LastIndexOf('/') + 1, elements[0].Trim().IndexOf('?') - elements[0].Trim().LastIndexOf('/') - 1);
                    temp = elements[0].Trim().Substring(elements[0].Trim().LastIndexOf("wid=") + 4, elements[0].Trim().Substring(elements[0].Trim().LastIndexOf("wid=")).IndexOf("&") - 4);

                    // URL
                    string url = elements[0];
                    // Product Id
                    string productId;
                    if (elements[1].Contains("product")) {
                        productId = elements[1].Substring(elements[1].IndexOf("product/prd-") + 12);
                        productId = productId.Substring(0, productId.IndexOf("/"));
                    } else
                        productId = "";

                    // Color
                    string color = imageName.Substring(imageName.IndexOf("_") + 1);
                    // Type 
                    string type;
                    if (temp.Equals("30")) {
                        type = "Swatch";
                    }
                    else if (temp.Equals("1000")) {
                        type = "Zoom";
                    } else if (temp.Equals("350")) {
                        type = "Main";
                    } else if (temp.Equals("50")) {
                        type = "Carousel";
                    } else if (temp.Equals("20")) {
                        type = "Swatch-Col";
                    } else {
                        type = "Other";
                    }
                    // nav_from
                    string nav_from = elements[1];

                // Generate the img instance 
                img curr = new img(url, productId, "NULL", type, imageName, nav_from, color);
                icsList.Add(curr);

                } catch (Exception ex) {
                    logger.Error("PARSING ERROR - " + ex.Message);
                }
            }

            return icsList;
        }

        public static void addDepartmentNumbers(int crawlID) {
            List<img> icsList = new List<img>();
            // populate the icsList with db data


            using (DataSet ds = Crawler.getMissingImageManifest(crawlID)) {
                List<string[]> storage = new List<string[]>();
                foreach (DataTable table in ds.Tables) {
                    foreach (DataRow dr in table.Rows) {
                        icsList.Add(new img(dr[0].ToString(),
                            dr[1].ToString(),
                            dr[2].ToString(),
                            dr[3].ToString(),
                            dr[4].ToString(),
                            dr[5].ToString(),
                            dr[6].ToString()));
                    }
                }
            }

            // hurr durr pointer
            List<img> icsListBackup = new List<img>(icsList.ToArray());
            int count = 0;
            Dictionary<string, string> map = new Dictionary<string, string>();
            // init database
            con = new OracleConnection();
            con.ConnectionString =
            "Data Source=" +
                "(DESCRIPTION = " +
                    "(ADDRESS = (PROTOCOL = TCP)(HOST = 127.0.0.4)(PORT = 9101))" +
                    "(CONNECT_DATA =" +
                        "(SID = KOHLDBPA1)" +
                    ")" +
                ");Persist Security Info=True;User ID=pkmdes2;Password=pkd2iu4ro";
            // build the query
            string prodIds = "";
            string collIds = "";
            // chunk in 500s
            while (icsList.Count > 0) {
                prodIds = "";
                for (int i = 0; i <= 500; i++) {
                    if (icsList.Count != 0) {
                        img curr = icsList[0];
                        //foreach (img curr in icsList) {
                        // might as well count here
                        count++;
                        if (!curr.productId.Contains("c"))
                            prodIds += "'" + curr.productId + "'" + ", ";
                        else
                            collIds += "'" + curr.productId + "'" + ", ";
                        icsList.Remove(curr);
                    }
                }

                // fix leading comma
                if (prodIds.Length!=0)
                    prodIds = prodIds.Substring(0, prodIds.Length - 2);
                if (collIds.Length!=0)
                    collIds = collIds.Substring(0, collIds.Length - 2);
                // Generate sql
                string sql = "SELECT product_id, max(dept_no) from atgprdcata.kls_sku sku";
                sql += " inner join atgprdcata.dcs_prd_chldsku child on child.sku_id = sku.sku_id";
                sql += " where product_id in (" + prodIds + ") group by product_id";

                string csql = "select cpr.category_id, max(sku.dept_no) ";
                csql += "from atgprdcata.kls_sku sku, atgprdcata.dcs_prd_chldsku chs, atgprdcata.dcs_cat_chldprd cpr ";
                csql += "where sku.sku_id = chs.sku_id";
                csql += " and cpr.child_prd_id = chs.product_id";
                csql += " and cpr.child_prd_id = (select max(cpr2.child_prd_id)";
                csql += " from atgprdcata.dcs_cat_chldprd cpr2";
                csql += " where cpr2.category_id = cpr.category_id";
                csql += " group by cpr2.category_id)";
                csql += " and (cpr.category_id in ( " + collIds + " ))group by cpr.CATEGORY_ID";
                // Run query
                OracleCommand command = new OracleCommand(sql, con);
                OracleCommand scommand = new OracleCommand(csql, con);
                OracleDataReader reader;
                try {
                    // ProdIds
                    con.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read()) {
                        try {
                            map.Add(reader.GetString(0), reader.GetString(1));
                        } catch (Exception e) {
                            // nothing to see here, duplicate map add
                        }
                    }
                    // CollIds
                    reader = scommand.ExecuteReader();
                    while (reader.Read()) {
                        try {
                            map.Add(reader.GetString(0), reader.GetString(1));
                        } catch (Exception e) {
                            // nothing to see here, duplicate map add
                        }
                    }
                } catch (Exception ex) {
                    // Shits broke yo
                    logger.Error("Error executing dept num query");
                    logger.Error(ex.Message);
                } finally {
                    con.Close();
                }
            }

            // refer to backup... really should clean this up
            icsList = icsListBackup;

            // Now map them dept_id's 
            foreach (var s in map)
                for (int i = 0; i < icsList.Count; i++)
                    if (s.Key == icsList[i].productId)
                        icsList[i].dept = s.Value;

            // update db 
            ArrayList sqls = new ArrayList();
            foreach (img ics in icsList) {
                if (ics.productId != "" && ics.dept != "") {
                    string sql = String.Format("UPDATE crawlerDB.dbo.missingImageManifest SET dept = {0} WHERE crawlID = {1} AND url='{2}'", ics.dept, crawlID, ics.url);
                    sqls.Add(sql);
                }
            }
            //string[] a = sqls.ToArray(typeof(string)) as string[];
            crawlerWebService.Crawler.updateDeptNums(sqls.ToArray(typeof(string)) as string[]);

            // Output
            logger.Info(String.Format("{0}/{1} images processed", count, icsList.Count));
        }




    }
}
