using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Oracle.DataAccess.Client;
using log4net.Appender;
using System.IO;

namespace crawlerConsole {
    public class ManifestFactory {
        private static OracleConnection con;
        private static log4net.ILog logger = log4net.LogManager.GetLogger("Main");

        public class img {
            public string productId;
            public string dept;
            public string type;
            public string color;
            public string imageName;
            public string nav_from;
        }
        public static List<img> createMissingImageList(string[] errorText) {
            List<img> icsList = new List<img>();
            string[] elements = new string[0];
            string temp;
            string imageName;


            // If we weren't passed an arg, load the log file
            if (errorText == null) {
                string curr;
                List<string> storage = new List<string>();
                using (StreamReader s = new StreamReader(Environment.CurrentDirectory.ToString() + @"\Logs\ics.log")) {
                    while ((curr = s.ReadLine()) != null) {
                        storage.Add(curr.Substring(4));
                    }
                }
                errorText = storage.ToArray();
            }


            foreach (string s in errorText) {
                img curr = new img();
                elements = s.Split(',');
                // constraints
                if (elements[0].Length <= 0)
                    break;
                if (elements[1].Contains("catalog.jsp"))
                    continue;
                try {
                    imageName = elements[0].Trim().Substring(elements[0].Trim().LastIndexOf('/') + 1, elements[0].Trim().IndexOf('?') - elements[0].Trim().LastIndexOf('/') - 1);
                    temp = elements[0].Trim().Substring(elements[0].Trim().LastIndexOf("wid=") + 4, elements[0].Trim().Substring(elements[0].Trim().LastIndexOf("wid=")).IndexOf("&") - 4);
                        imageName = imageName.Replace("_sw", "");
                    // Product Id
                    string productId;
                    if (elements[1].Contains("product")) {
                        productId = elements[1].Substring(elements[1].IndexOf("product/prd-") + 12);
                        productId = productId.Substring(0, productId.IndexOf("/"));
                    } else
                        productId = "";
                    curr.productId = productId;
                    // Color
                    curr.color = imageName.Substring(imageName.IndexOf("_") + 1);
                    // Type 
                    if (temp.Equals("30")) {
                        curr.type = "Swatch";
                    }
                    else if (temp.Equals("1000")) {
                        curr.type = "Zoom";
                    } else if (temp.Equals("350")) {
                        curr.type = "Main";
                    } else if (temp.Equals("50")) {
                        curr.type = "Carousel";
                    } else if (temp.Equals("20")) {
                        curr.type = "Swatch-Col";
                    } else {
                        curr.type = "Other";
                    }
                    // nav_from
                    curr.nav_from = elements[1];

                    // imageName
                    curr.imageName = imageName;
                } catch (Exception ex) {
                    logger.Error("PARSING ERROR - " + ex.Message);
                }
                icsList.Add(curr);
            }
            // write csv
            if (File.Exists(Environment.CurrentDirectory + @"\out\MissingImages.csv"))
                File.Delete(Environment.CurrentDirectory + @"\out\MissingImages.csv");
            File.Create(Environment.CurrentDirectory + @"\out\MissingImages.csv").Close();

            using (StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + @"\out\MissingImages.csv")) {
                for (int i = 0; i < icsList.Count; i++) {
                    sw.WriteLine(String.Format("NULL,{0},{1},{2},{3},{4}", ((img)icsList[i]).productId, ((img)icsList[i]).type, ((img)icsList[i]).color, ((img)icsList[i]).imageName, ((img)icsList[i]).nav_from));
                }
            }
            return icsList;
        }

        public static void addDepartmentNumbers(List<img> icsList) {
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
                prodIds = prodIds.Substring(0, prodIds.Length - 2);
                // Generate sql
                string sql = "SELECT product_id, max(dept_no) from atgprdcata.kls_sku sku";
                sql += " inner join atgprdcata.dcs_prd_chldsku child on child.sku_id = sku.sku_id";
                sql += " where product_id in (" + prodIds + ") group by product_id";
                // Run query
                OracleCommand command = new OracleCommand(sql, con);
                OracleDataReader reader;
                try {
                    con.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read()) {
                        try {
                            map.Add(reader.GetString(0), reader.GetString(1));
                        } catch (Exception e) {
                            // nothing to see here, duplicate map add
                        }
                    }
                } catch (Exception ex) {
                    // Shits broke yo
                    Console.WriteLine("ERROR: " + ex.Message);
                    Console.ReadKey();
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

            // write csv
            if (File.Exists(Environment.CurrentDirectory + @"\out\MissingImages.csv"))
                File.Delete(Environment.CurrentDirectory + @"\out\MissingImages.csv");
            File.Create(Environment.CurrentDirectory + @"\out\MissingImages.csv").Close();

            using (StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + @"\out\MissingImages.csv")) {
                for (int i = 0; i < icsList.Count; i++) {
                    sw.WriteLine(String.Format("{0},{1},{2},{3},{4},{5}", icsList[i].dept, ((img)icsList[i]).productId, ((img)icsList[i]).type, ((img)icsList[i]).color, ((img)icsList[i]).imageName, ((img)icsList[i]).nav_from));
                }
            }

            // Output
            logger.Info(String.Format("{0}/{1} images processed", count, icsList.Count));
        }




    }
}
