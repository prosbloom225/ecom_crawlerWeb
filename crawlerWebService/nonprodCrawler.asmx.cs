using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Data.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.Services;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.SqlServer.Server;
using System.Drawing;
using System.Data.SqlClient;
using log4net;

using Utilities.RegularExpressions;


namespace crawlerWebService {
    /// <summary>
    /// Crawler Web Service
    /// </summary>
    [WebService(Namespace = "http://localhost/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class nonprodCrawler : System.Web.Services.WebService {
        // DEBUG
        private static int debug = 0;
        private static bool catalogCheckFlag = false;
        private static int crawlID;
        // END DEBUG

        /** VAR DECLARE */
        private static Stopwatch stopWatch = new Stopwatch();
        private static bool isPaused = false;
        private static bool stopCrawlFlag = false;
        private static bool outOfSeeds = false;
        private static int imageCount = 0;
        private static int missingImageCount = 0;
        private static int pagesVisitedCount = 0;
        private static List<string> fakeImageTags;
        private static int maxSeeds = 0;

        private static ConcurrentDictionary<string, nav> pagesToVisit;
        private static ConcurrentDictionary<string, string> pagesVisited;
        private static ConcurrentDictionary<string, string> productsVisited;
        private static Queue<string> seedStorage;

        private static Thread seedThread;
        private static Thread controlThread;
        private static ArrayList threadStorage;
        private static int crawlerThreadCount;


        // FLAGS
        private static int crawlerMaxThreads = 150; // 150 please
        private static int controlThreadTimeout = 100000; // 100000 please
        private static int maxDepth = 6; // 4 seems to be a good depth
        private static bool usedb = true; 
        private static bool checkImages = false;

        // Logging
        private const string userAgent = "Mozilla/5.0 (Windows NT 5.1; rv:31.0) Gecko/20100101 Firefox/31.0 tk93053";
        private static log4net.ILog logger = log4net.LogManager.GetLogger("nonProd");
        private static string conn = @"user id=sa;password=Cbf@1203;server=10.11.75.95\SCHALA;Trusted_Connection=no;database=crawlerDB;connection timeout=10";

        // Locks
        private static object crawlerThreadCountLock = new object();
        private static object consoleLock = new object();

        // Regex
        private static DeadLink deadLinkRegex = new DeadLink();

        /** END VAR DECLARE */
        [WebMethod()]
        public bool test() {
            return true;
        }


        /** Public Methods */

        // Control methods
        [WebMethod]
        public string[] testCrawl() {
            // get a new crawlID
            try {

                int val = 0;
                DataSet ds = dbSelect("select max(crawlID) from crawlerDB.dbo.crawlIDs");
                if (ds != null && ds.Tables.Count > 0) {
                    val = Convert.ToInt16(ds.Tables[0].Rows[0].ItemArray[0]);
                }

                crawlID = val + 1;
                dbInsert(String.Format("INSERT INTO crawlerDB.dbo.crawlIDs VALUES({0})", crawlID));
            } catch (Exception e) {
                logger.Error("Error in getting new crawlID");
            }

            // crawl id has been set


            stopWatch.Reset();
            stopWatch.Start();
            init();
            controlThread = new Thread(crawlerThreadStarter);
            controlThread.Name = "controlThread";
            controlThread.Start();
            Thread.Sleep(3000);
            //ManifestFactory.createMissingImageList(null);
            //ManifestFactory.addDepartmentNumbers(ManifestFactory.createMissingImageList(null));
            dbInsert(String.Format("INSERT INTO crawlerDB.dbo.crawlLog VALUES ('Crawler started', '{0}', {1})", DateTime.Now, crawlID));

            // Initial update of crawl stats
            updateCrawlStats();

            return new string[3] { "Is Active: " + controlThread.IsAlive, "Is Background: " + controlThread.IsBackground, "Thread State: " + controlThread.ThreadState };
        }
        public static void stopCrawl() {
            // Really want to get the next logging entries to get pushed at all costs.
            stopWatch.Stop();
            addCrawlLog("Crawler stopped");
            logger.Info(String.Format("stopCrawl triggered.  Force killing {0} crawl threads.", crawlerThreadCount));
            try {
                // stop the seed thread
                seedThread.Abort();
                // kill the worker threads
                foreach (Thread s in threadStorage) {
                    if (s.ThreadState == System.Threading.ThreadState.Running)
                    s.Abort();
                }
            } catch (Exception e) {
                logger.Error("Failure in stopping crawl threads");
                logger.Error(e.Message);
            }
            try {
                // stop the control thread
                controlThread.Abort();
            } catch {
                // Everything is ok, we just aborted ...ourselves... there has to be a better way
                // TODO - kill control thread cleanly
            }
        }
        public static bool pauseCrawl() {
            stopWatch.Stop();
            isPaused = true;
            addCrawlLog("Crawler paused");
            return true;
        }
        public static bool resumeCrawl() {
            stopWatch.Start();
            isPaused = false;
            addCrawlLog("Crawler resumed");
            return true;
        }

        // DB Methods
        private static bool dbInsert(string sql) {
            if (usedb) {
                try {
                    using (SqlConnection db = new SqlConnection(conn)) {
                        db.Open();
                        SqlCommand command = new SqlCommand(sql, db);
                        return Convert.ToBoolean(command.ExecuteNonQuery());
                    }
                } catch (Exception e) {
                    logger.Error("ERROR making insert");
                    logger.Error(e.Message);
                }
            }

            // default return
            return false;
        }
        private static bool dbDelete(string sql) {
            if (usedb) {
                try {
                    using (SqlConnection db = new SqlConnection(conn)) {
                        db.Open();
                        SqlCommand command = new SqlCommand(sql, db);
                        return Convert.ToBoolean(command.ExecuteNonQuery());
                    }
                } catch (Exception e) {
                    logger.Error("ERROR making delete");
                    logger.Error(e.Message);
                }
            }

            // default return
            return false;
        }
        private static DataSet dbSelect(string sql) {
            if (usedb) {
                try {
                    using (SqlConnection db = new SqlConnection(conn)) {
                        db.Open();
                        SqlCommand command = new SqlCommand(sql, db);
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        return ds;
                    }
                } catch (Exception e) {
                    logger.Error("ERROR making select");
                    logger.Error(e.Message);
                }
            }
            // Default return
            return null;
        }

        public static DataSet getCrawlLog(int size, bool isDistinct ) {
            string sql;
            if (isDistinct) {
                sql = String.Format("SELECT DISTINCT TOP {0} crawlID FROM crawlerDB.dbo.crawlLog ORDER BY crawlID DESC", size);
            } else {
                sql = String.Format("SELECT TOP {0} message, timestamp, crawlID FROM crawlerDB.dbo.crawlLog ORDER BY timestamp DESC", size);
            }
            try {
                return dbSelect(sql);
            } catch (Exception e) {
                logger.Error("Error getting crawler log.");
                logger.Error(e.Message);
            }
            // Default return
            return null;
        }
        public static void addCrawlLog(string msg) { string sql = String.Format("INSERT INTO crawlerDB.dbo.crawlLog VALUES ('{0}', '{1}', {2})", msg, DateTime.Now.ToString(), crawlID); if (!dbInsert(sql)) { logger.Error("Error adding to crawl log.  Data: " + msg);
            }
        }
        public static void addMissingImage(string url, string nav_from) {
            string sql = String.Format("INSERT INTO crawlerDB.dbo.missingImageDump VALUES ('{0}', '{1}', {2}, '{3}', NULL, NULL, NULL)", url, nav_from, crawlID, DateTime.Now);
            if (!dbInsert(sql)) {
                logger.Error("Error adding to missing images table .  Data: " + url);
            }
        }
        public static void addError(string type, string url, string nav_from) {
            string sql = String.Format("INSERT INTO crawlerDB.dbo.errorsDump VALUES ('{0}', '{1}', '{2}', {3}, '{4}')", type, url, nav_from, crawlID, DateTime.Now);
            if (!dbInsert(sql)) {
                logger.Error("Error adding to errors table .  Data: " + type + " - " + url);
            }
        }

        public static DataTable getGraphData(string type) {
            string sql = "";
            switch (type) {
                case "missingImages":
                    sql = String.Format("select crawlID, count(1) from crawlerDB.dbo.missingImageDump where crawlID > (select max(crawlID)-5 from crawlerDB.dbo.crawlIDs) group by crawlID order by crawlID");
                    break;
                case "products":
                    sql = String.Format("select crawlID, productsVisited from crawlerDB.dbo.crawlStats where crawlID > (select max(crawlID)-5 from crawlerDB.dbo.crawlIDs) order by crawlID");
                    break;
                case "pages":
                    sql = String.Format("select crawlID, pagesVisited from crawlerDB.dbo.crawlStats where crawlID > (select max(crawlID)-5 from crawlerDB.dbo.crawlIDs) order by crawlID");
                    break;
            }
            try {
                return dbSelect(sql).Tables[0];
            } catch (Exception e) {
                logger.Error("Error getting graph data.");
                logger.Error(e.Message);
            }
            // Default return
            return null;

        }
        public static DataSet getMissingImages(int crawlID) {
            string sql = String.Format("SELECT url, nav_from FROM crawlerDB.dbo.missingImageDump WHERE crawlID = {0}", crawlID);
            try {
                return dbSelect(sql);
            } catch (Exception e) {
                logger.Error("Error getting graph data.");
                logger.Error(e.Message);
            }
            // Default return
            return null;
        }
        public static IEnumerable<img> getMissingImageManifestEnumerable(int crawlID) {
            string sql = String.Format("SELECT Url, ProductId, Dept, Type, ImageName, Nav_From, Color FROM crawlerDB.dbo.missingImageManifest WHERE crawlID = {0}", crawlID);
            try {
                return dbSelect(sql).Tables[0].AsEnumerable().Select(row =>
                    {
                        return new img {
                            url = row[0].ToString(),
                            productId = row[1].ToString(),
                            dept = row[2].ToString(),
                            type = row[3].ToString(),
                            imageName = row[4].ToString(),
                            nav_from = row[5].ToString(),
                            color = row[6].ToString()
                        };
                    });
            } catch (Exception e) {
                logger.Error("Error getting manifest data.");
                logger.Error(e.Message);
            }
            // Default return
            return null;

        }

        public static DataSet getMissingImageManifest(int crawlID) {
            string sql = String.Format("SELECT Url, ProductId, Dept, Type, ImageName, Nav_From, Color FROM crawlerDB.dbo.missingImageManifest WHERE crawlID = {0}", crawlID);
            try {
                return dbSelect(sql);
            } catch (Exception e) {
                logger.Error("Error getting manifest data.");
                logger.Error(e.Message);
            }
            // Default return
            return null;

        }
        public static bool removeManifest(int crawlID) {
            string sql = String.Format("DELETE FROM crawlerDB.dbo.missingImageManifest WHERE crawlID = {0}", crawlID);
            try {
                return dbDelete(sql);

            } catch (Exception e) {
                logger.Error("Error removing manifest data for crawlID: ." + crawlID);
                logger.Error(e.Message);
            }
            // default return
            return false;
        }
        public static bool insertManifest(List<img> icsList, int crawlId) {

            foreach (img ics in icsList) {
                string sql = String.Format("INSERT INTO crawlerDB.dbo.missingImageManifest (url, nav_from, crawlId, timestamp, imageName, type, color, productId) VALUES ('{0}', '{1}', {2}, '{3}', '{4}', '{5}', '{6}', '{7}')", ics.url, ics.nav_from, crawlId, DateTime.Now, ics.imageName, ics.type, ics.color, ics.productId);
                dbInsert(sql);
            }
            // default return
            return false;
        }
        public static bool updateDeptNums(string[] sqls) {
            bool isError = false;
            foreach (string sql in sqls) {
                try {
                    if (dbInsert(sql))
                        isError = true;
                } catch (Exception e) {
                    logger.Error("Error updating dept nums.");
                    logger.Error(e.Message);
                }
            }

            // default return
            return isError;
        }



        // Getters
        [WebMethod]
        public DataTable getCrawlStats() {
            DataTable dtStats = new DataTable("dtStats");
            dtStats.Columns.Add(new DataColumn("Key", typeof(string)));
            dtStats.Columns.Add(new DataColumn("Val", typeof(string)));
            dtStats.Rows.Add("pagesVisited", getPagesVisitedCount().ToString());
            dtStats.Rows.Add("pagesVisitedCount", pagesVisitedCount);
            dtStats.Rows.Add("pagesToVisit", getPagesToVisit().ToString());
            dtStats.Rows.Add("crawlStatus", getCrawlThreadInfo()[0].ToString());
            return dtStats;
        }
        [WebMethod]
        public void dumpPagesVisited() {

            string path = @"c:\osieckim\Logs\pagesVisited.csv";
            lock (pagesVisited) {
                File.Delete(path);
                foreach (KeyValuePair<string, string> s in pagesVisited) {
                    using (StreamWriter sw = File.AppendText(path)){
                        sw.WriteLine(s.Key + "," + s.Value);
                    }
                }
            }
        }
        [WebMethod]
        public void dumpPagesToVisit() {
            string path = @"c:\osieckim\Logs\pagesToVisit.csv";
            lock (pagesVisited) {
                File.Delete(path);
                foreach (KeyValuePair<string, nav> s in pagesToVisit) {
                    using (StreamWriter sw = File.AppendText(path)){
                        sw.WriteLine(s.Key + "," + s.Value.nav_from + "," + s.Value.depth);
                    }
                }
            }
        }
        public static string[] getCrawlThreadInfo() {
            string isBackground = "ERR";
            string isAlive = "ERR";
            string state = "ERR";
            try {
                isBackground = controlThread.IsBackground.ToString();
            } catch { }
            try {
                isAlive = controlThread.IsAlive.ToString();
            } catch { }
            try {
                state = controlThread.ThreadState.ToString();
            } catch { }

            return new string[3] { "Is Active: " + isAlive, "Is Background: " + isBackground, "Thread State: " + state };
        }
        public static int getPagesVisitedCount() {
            try {
                lock (pagesVisited)
                    return pagesVisited.Count;
            } catch {
                return 0;
            }
        }
        public static int getPagesToVisitCount() {
            lock (pagesToVisit)
                try {
                    return pagesToVisit.Count;
                } catch {
                    return 0;
                }
        }
        public static int getProductsVisitedCount() {
            try {
                return productsVisited.Count;
            } catch {
                return 0;
            }
        }
        public static int getCurrentThreadCount() {
            lock (crawlerThreadCountLock)
                try {
                    return crawlerThreadCount;
                } catch {
                    return 0;
                }
        }
        public static int getSeedsToVisit() {
            try {
                lock (seedStorage) {
                    return seedStorage.Count;
                }
            } catch {
                return 0;
            }
        }
        public static TimeSpan getElapsed() {
            try {
                return stopWatch.Elapsed;
            } catch {
                return new TimeSpan();
            }
        }
        public static int getMissingImageCount() {
            try {
                return missingImageCount;
            } catch {
                return 0;
            }
        }
        public static int getPagesToVisit() {
            lock (pagesToVisit)
                try {
                    return pagesToVisit.Count;
                } catch {
                    return 0;
                }
        }
        public static string[] getControlThreadStatus() {
            return new string[3] { "Is Active: " + controlThread.IsAlive, "Is Background: " + controlThread.IsBackground, "Thread State: " + controlThread.ThreadState };
        }
        public static int getMaxSeeds() {
            try {
                return maxSeeds;
            } catch {
                return 0;
            }
        }
        public static int getCurrentSeeds() {
            try {
                return seedStorage.Count;
            } catch {
                return 0;
            }
        }
        public static bool getStatusStoppedStarted() {
            // Maybe???
            return stopCrawlFlag;

        }

        /** Private Methods */
        private static bool init() {
            try {
                if (controlThread.IsAlive) {
                    return true;
                }
            } catch { }
            // Init dataStructures
            pagesToVisit = new ConcurrentDictionary<string, nav>();
            pagesVisited = new ConcurrentDictionary<string, string>();
            productsVisited = new ConcurrentDictionary<string, string>();
            seedStorage = new Queue<string>();
            seedThread = new Thread(seedMethod);
            seedThread.Name = "seedThread";
            threadStorage = new ArrayList();

            // Reset vars
            stopCrawlFlag = false;
            outOfSeeds = false;


            // Static webMethod vars
            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = 10000;


            // Set initial seeding for homepage
            seedHomepage();

            // Start the seedThread
            logger.Debug("Kicking of seedThread");
            try {
                seedThread.Start();
            } catch (Exception e) {
                logger.Error("Error starting seedThread");
                logger.Error(e.Message);
            }

            // Init logging
            string logFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\Log4Net.config";
            FileInfo finfo = new FileInfo(logFilePath);
            log4net.Config.XmlConfigurator.ConfigureAndWatch(finfo);
            //log4net.Config.XmlConfigurator.Configure();
            logger.Debug("Init complete.");
            return false;
        }


        private static void output() {
        }
        private static void nonlockingOutput() {
            //new Task(output).Start();
        }
        private static void threadWaitingRoom(Thread thread) {
            logger.Debug("Thread has entered the waiting room.");
            while (isPaused) {
                Thread.Sleep(3000);
            }
            logger.Debug("Thread has exited the waiting room.");
        }
        private static void updateCrawlStats() {
            dbInsert(String.Format("INSERT INTO crawlerDB.dbo.crawlStats_tmp VALUES ({0}, {1}, {2}, {3}, {4})", crawlID, pagesVisited.Count, productsVisited.Count, missingImageCount, stopWatch.ElapsedMilliseconds));
            // Merge
            dbInsert("MERGE INTO crawlerDB.dbo.crawlStats as target USING crawlerDB.dbo.crawlStats_tmp AS source ON source.crawlID = target.crawlID WHEN MATCHED THEN UPDATE SET target.pagesVisited = source.pagesVisited, target.productsVisited = source.productsVisited, target.missingImages = source.missingImages, target.elapsed = source.elapsed WHEN NOT MATCHED BY target THEN INSERT VALUES (source.crawlID, source.pagesVisited, source.productsVisited, source.missingImages, source.elapsed);");
            // Clear out the tmp table
            dbInsert("DELETE FROM crawlerDB.dbo.crawlStats_tmp");
        }

        private static void crawlSite() {
            logger.Debug("Spinning up a new worker thread.");
            String targetPage = "";
            String navFrom = "";
            while (true) {
                // pause functionality.... finish the current page then pause
                if (isPaused) {
                    threadWaitingRoom(Thread.CurrentThread);
                }
                try {
                    targetPage = "";
                    navFrom = "";

                    //lock (pagesToVisit) {
                    if (pagesToVisit.Count != 0) {
                        foreach (KeyValuePair<string, nav> kp in pagesToVisit) {
                            // lets have a prefer for images, just get them out of the way
                            if (kp.Key.Contains("edgesuite.net")) {
                                targetPage = kp.Key;
                                navFrom = kp.Value.nav_from;
                                break;
                            }
                        }
                        if (targetPage == "")
                            foreach (KeyValuePair<string, nav> kp in pagesToVisit) {
                                // lets have a prefer for products now
                                if (kp.Key.Contains("prd-")) {
                                    targetPage = kp.Key;
                                    navFrom = kp.Value.nav_from;
                                    break;
                                }
                            }

                        // Ok we have no images or products queue'd, lets move on to the rest
                        if (targetPage == "")
                            foreach (KeyValuePair<string, nav> kp in pagesToVisit) {
                                targetPage = kp.Key;
                                navFrom = kp.Value.nav_from;
                                break;
                            }

                    } else {
                        // We're out of pagesToVisit, check stopCrawlFlag and suicide
                        if (stopCrawlFlag == true) {
                            crawlerThreadCount--;
                            return;
                        } else {
                            // No pages to crawl, wait for the seeds to repopulate
                            Thread.Sleep(5000);
                        }
                    }

                    // Lock Bracket
                    //}

                    // Ok, we're going to getPage, remove from pagesToVisit
                    nav triedRemove;
                    pagesToVisit.TryRemove(targetPage, out triedRemove);
                    getPage(targetPage, triedRemove);

                    nonlockingOutput();

                } catch (Exception ex) {
                    logger.Error("Error in crawlSite. This is a big deal, giving a stack trace too...");
                    logger.Error(ex.Message);
                    logger.Error(ex.StackTrace);
                }
            }
            // somehow the thread died
            lock (crawlerThreadCountLock) {
                crawlerThreadCount--;
            }

        }

        private static void crawlerThreadStarter() {
            logger.Debug("Crawl thread started");


            while (true) {
                // Interrupts
                if (isPaused) {
                    threadWaitingRoom(Thread.CurrentThread);
                    logger.Debug("Control thread hit a pause.  Going into storage.");
                }
                if (outOfSeeds) {
                    // We're out of seeds, watch for the workers to complete and then break.
                    logger.Debug("outOfSeeds hit.  Watiting for workers then breaking the controlThread loop.");
                    lock (crawlerThreadCountLock) {
                        while (crawlerThreadCount > 0) {
                            Thread.Sleep(1000);
                            lock (pagesToVisit) {
                                if (pagesToVisit.Count == 0) {
                                    foreach (Thread s in threadStorage) {
                                        if (s.ThreadState != System.Threading.ThreadState.Stopped) {
                                        }
                                    }
                                    stopCrawlFlag = true;
                                }
                            }
                        }
                    }
                    break;
                }
                Thread.Sleep(3000);

                // Thread Starter
                lock (crawlerThreadCountLock) {
                    while (crawlerThreadCount <= crawlerMaxThreads - 1) {
                        Thread sitecrawler = new Thread(crawlSite);
                        sitecrawler.IsBackground = true;
                        sitecrawler.Start();
                        threadStorage.Add(sitecrawler);
                        sitecrawler.Name = "workerThread" + crawlerThreadCount;
                        lock (crawlerThreadCountLock)
                            crawlerThreadCount++;
                        Thread.Sleep(200);
                    }
                }
            }

            // We're done with the crawl.  Let's wait for the workers to complete
            // But set a had timeout, just in case
            Stopwatch controlThreadStopwatch = new Stopwatch();
            controlThreadStopwatch.Start();
            while (crawlerThreadCount > 0 || controlThreadStopwatch.ElapsedMilliseconds > controlThreadTimeout) {
                Thread.Sleep(1000);
            }
            controlThreadStopwatch.Stop();
            // Ok, we're done

            // Tidy things up and then close out
            // trying putting this in its own thread so controlthread can be killed cleanly
            // stopcrawl();
            new Thread(stopCrawl).Start();
            logger.Info("Crawl completed at: " + DateTime.Now);
            logger.Info("Pages visited: " + pagesVisited.Count);
            logger.Info("Products visited: " + productsVisited.Count);
            logger.Info("Time Elapsed: " + stopWatch.Elapsed);
        }


        private static void seedHomepage() {
            logger.Debug("Seeding homepage.  Aww yiss");
            try {
                //seedStorage.Enqueue(@"http://www.kohls.com/feature/brands.jsp?cc=for_thehome-LN4.0-S-shopallbrands");
                seedStorage.Enqueue(@"http://www.kohls.com");
            } catch (Exception ex) {
                logger.Error("Error in seeding homepage.");
                logger.Error(ex.Message);
            }
        }

        private static void seedMethod() {
            while (1 == 1) {
                updateCrawlStats();
                lock (pagesToVisit) {
                    if (seedStorage.Count == 0 && pagesToVisit.Count == 0) {
                        // We're done....
                        // Stop seed thread and set outOfSeeds so controlThread watches for workers and then completes
                        logger.Debug("Seed thread is out of seeds.");
                        outOfSeeds = true;
                        // We're done here, suicide
                        return;
                    } else {
                        if (pagesToVisit.Count <= 0) {
                            // seedThread can handle updating crawlStats as she isn't gonna flood the db with queries
                            //updateCrawlStats();
                            lock (pagesToVisit) {
                                pagesToVisit.TryAdd(seedStorage.Peek(), new nav("seed",0));
                                seedStorage.Dequeue();
                            }
                        }
                    }
                }
                Thread.Sleep(1000);
            }

        }
        private static bool checkImage(string url) {
            // build list of fake image ETags if not exist
            if (fakeImageTags == null) {
                fakeImageTags = new List<string>();
                getFakeImages(new Size(20, 20));
                getFakeImages(new Size(30, 30));
                getFakeImages(new Size(50, 50));
                getFakeImages(new Size(75, 75));
                getFakeImages(new Size(350, 350));
                getFakeImages(new Size(400, 400));
                getFakeImages(new Size(1000, 1000));
            }

            try {
                WebRequest req = WebRequest.Create(url);
                req.Method = "HEAD";
                using (WebResponse response = req.GetResponse()) {
                    // get the size
                    string uri = response.ResponseUri.ToString();


                    string width = "20";

                    string tried;
                    foreach (string s in fakeImageTags) {
                        if (response.Headers["ETag"].Equals(s))
                            return true;
                    }
                }
            } catch (Exception ex) {
                logger.Debug("ERROR with request for url - " + url);
                logger.Debug(ex.Message);
            }
            return false;
        }
        private static void getFakeImages(Size size) {
            WebRequest req =
                WebRequest.Create(
                    String.Format(
                        "http://media.kohls.com.edgesuite.net/is/image/kohls/1fake_image?wid={0}&amp;hei={0}",
                        size.Width));
            using (WebResponse response = req.GetResponse()) {
                fakeImageTags.Add(response.Headers["ETag"]);
            }
        }

        private static bool checkVisited(string url) {
            lock (pagesVisited)
                if (pagesVisited.ContainsKey(url))
                    return true;
                else
                    return false;
        }

        private static bool shouldVisit(string url, nav nav_from) {
            // TODO - Check for depth
            if (nav_from.depth >= maxDepth) {
                return false;
            }
            // Drop all product pages
            if (url.Contains("prd-"))
                return false;
            if (url.Contains("edgesuite.net"))
                nav_from.nav_from = "img";
            // Ignore filetypes
            if (url.Contains(".ico") || url.Contains(".css"))
                return false;
            // Ignore non-kohls pages
            if (!url.Contains("kohls.com") && !url.Contains("kohlsecommerce.com"))
                return false;
            // Ignore catalog.jsp pages, as we've seeded them all already
            if (nav_from.nav_from.Contains("catalog.jsp") && url.Contains("catalog"))
                return false;
            // Trying removing all catalog.jsp's.  They should only be added via the seedThread
            if (url.Contains("catalog.jsp")) //&& url.Contains("N=0"))
                return false;
            if (url.Contains("cs.kohls.com"))
                return false;
            if (url.Contains("search.jsp") || url.Contains("/search/"))
                return false;
            if (url.Contains(".shtml"))
                return false;

            // Lets not let a catalog page call a catalog page
            if (url.Contains("catalog") && nav_from.nav_from.Contains("catalog"))
                return false;
            // Ignore catalog pages, wayy too many to count, might have to add them later
            // TODO - add catalog back in, have to strip the queryString first
            //if (url.Contains("catalog/"))
            //   return false;
            return true;
        }

        private static void createManifest(string pageText, string url, nav nav_from) {

            // Generate nav object
            nav currNav = new nav(url, nav_from.depth+1);

            // Ignore js/css
            foreach (Match m in Regex.Matches(url, @"(js\b|css\b)", RegexOptions.Singleline))
                return;

            // Find all links in responseFromServer
            string textMatch = "";
            string pattern = @"((href|src)=""[^""]+|url\((""|')[^(""|')]+)";

            foreach (Match m in Regex.Matches(pageText, pattern, RegexOptions.Singleline)) {

                textMatch = m.ToString().Replace("href=\"", "");
                if (textMatch.Contains("/null"))
                    continue;
                if (textMatch.Contains("#"))
                    continue;
                if (textMatch.Contains("mobile.kohls.com"))
                    continue;
                textMatch = textMatch.ToString().Replace("url(\"", "");
                textMatch = textMatch.ToString().Replace("url('", "");
                textMatch = textMatch.Replace("src=\"", "");
                textMatch = textMatch.Replace("javascript:launchCorporate('", "");
                textMatch = textMatch.Replace("')", "");
                // TODO - this is temp, move to prd-id for checking product pages
                // Strip parameters from products
                if (textMatch.Contains("prd-") && textMatch.Contains("?"))
                    textMatch = textMatch.Substring(0, textMatch.IndexOf("?"));
                if (!textMatch.Contains("javascript") &&
                    (!textMatch.Contains("jsessionid") || url == "http://www.kohls.com") &&
                    !textMatch.Contains("inc_omniture_akamai.jsp")) {

                    if (!textMatch.Contains("www.") && !textMatch.ToLower().Contains("http")) {
                        Uri result;
                        Uri.TryCreate(new Uri(url), textMatch, out result);
                        textMatch = result.ToString();

                    }
                    // Check if we should visit this url
                    if (shouldVisit(textMatch, currNav)) {
                        try {
                            // final check, somehow null is sneaking through - probably fix later to improve performance
                            if (textMatch.Contains(@"/null"))
                                continue;
                            // Add to manifest 
                            if (pagesToVisit.TryAdd(textMatch, currNav))
                                // Log the page manifest creation
                                logger.Debug("queueing: " + textMatch + " - from: " + url);
                            else logger.Debug("Queue failed, duplicate detected: " + url);
                        }
                            // We have a duplicate, move along
                        catch (Exception e) {
                            logger.Debug("Queue failed at tryAdd, look into this as shouldVisit should have caught it");
                            logger.Debug(e.Message);
                            continue;
                        }
                    }
                }
            }

        }

        private static string getHtml(string url) {
            string responseFromServer = "";
            HttpWebResponse response = null;
            Stream dataStream = null;
            StreamReader reader = null;
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.Timeout = 3600000;
                request.UserAgent = userAgent;

                response = (HttpWebResponse)request.GetResponse();
                dataStream = response.GetResponseStream();
                reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();
                try {
                    if (dataStream != null)
                        dataStream.Close();
                    reader.Close();
                    response.Close();
                } catch (Exception ex) {
                    // tk93053 - debug output
                    logger.Error("Exception in reading from stream.");
                    logger.Error(ex.Message);
                }
                if (nonprodCrawler.debug == 1)
                    logger.Info(url + " - " + responseFromServer.Length);
            } catch (Exception ex) {
                logger.Debug("Exception in getHtml for page: " + url);
                logger.Debug(ex.Message);
            }

            return responseFromServer;
        }
        private static void getPage(string url, nav nav_from) {
            // Check if visited, if so, return
            if (checkVisited(url))
                return;
            // Handle akamaized images
            if (!checkImages && (url.Contains("media") && url.Contains("edgesuite.net")))
                return;
            if (url.Contains("media") && url.Contains("edgesuite.net") && !url.Contains(".swf")) {
                if (checkImage(url)) {
                    // TEST

                    string html = getHtml(nav_from.nav_from);
                    if (!html.Contains(url)) {
                        // we have a problem with manifest creation
                        logger.Debug("Error with manifest creation.  URL: " + url + " - Nav_From: " + nav_from);
                        return;
                    }
                    // END TEST
                    if ((nav_from.nav_from.Contains("catalog.jsp") && catalogCheckFlag) || !nav_from.nav_from.Contains("catalog.jsp")) {
                        addMissingImage(url, nav_from.nav_from);
                    }
                    logger.Debug("Image coming soon:" + url + " - " + nav_from);
                    missingImageCount++;
                } else {
                    imageCount++;
                }
                pagesVisited.TryAdd(url, nav_from.nav_from);
                return;
            }


            string responseFromServer = "";
            responseFromServer = getHtml(url);
            // Counter purposes
            // is Product?
            string productCode = "";
            if (url.Contains("prd-")) {
                foreach (Match m2 in Regex.Matches(url, @"prd[^/]+", RegexOptions.Singleline)) {
                    productCode = m2.ToString();
                    break;
                }
                if (productCode != "") {
                    // Try and add the product, if it fails, we've already crawled it
                    try {
                        productsVisited.TryAdd(productCode, "");
                    } catch {
                        //logger.Info("Error adding product to productsVisited" + productCode);
                    }
                }
            }
            logger.Debug("Starting to regex HTML received Size: " + responseFromServer.Length);


            // Ok we have some html, lets regex it
            string startString = "<div id=\"breadcrumb\"";
            string endString = "</div><!-- #dimensions -->";
            string startStringATG = "<div class=\"clearfix\" id=\"breadcrumb\"";
            string endStringATG = "<ul id=\"image-size-toggle\"";
            responseFromServer = Regex.Replace(responseFromServer, startString + ".+?" + endString, "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, startStringATG + ".+?" + endStringATG, "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, @":80", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, @":443", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "<link rel=\"canonical.+?>", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "'[+].*[+]'", "(IGNORE)", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "MORE TO CONSIDER" + ".+?" + "<!-- ForEach Ends -->", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "MORE TO CONSIDER" + ".+?" + "<!-- Right panel Ends -->", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "<script type=\"text/javascript\"" + ".+?" + "</script>", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, ";jsessionid=[a-zA-Z0-9]+!\\-*[0-9]+!\\-*[0-9]+", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, ";jsessionid=[a-zA-Z0-9]+!\\-*[0-9]+", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "WS=0\\&", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "\\&WS=0", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, @"&S=\d", "", RegexOptions.Singleline);
            startString = "<ul id=\"navigation\">";
            endString = "</noscript>";
            responseFromServer = Regex.Replace(responseFromServer, startString + ".+?" + endString, "", RegexOptions.Singleline);
            startString = "<!-- BEGIN RELATED -->";
            endString = "<!-- END MORE-RESULTS -->";
            responseFromServer = Regex.Replace(responseFromServer, startString + ".+?" + endString, "",
                RegexOptions.Singleline);
            // Remove cs.kohls.com chainlinks..  lets do it now to speed up building manifest
            responseFromServer = Regex.Replace(responseFromServer, @"http(s)?:\/\/cs\.kohls\.com\/.+?;$", "", RegexOptions.Singleline);

            logger.Debug("Regex complete.  Size: " + responseFromServer.Length);
            if (responseFromServer.Contains("wrongSite is assigned"))
                logger.Debug("wrongSite ERROR: " + url);

            // Check for dead links
            /*
            if (deadLinkRegex.Matches(responseFromServer).Count > 0) {
                logger.Debug("NEW DEAD LINK FOUND ON PAGE: " + url);
                logger.Debug("LINKED FROM PAGE: " + nav_from.nav_from);
                addError("DEAD", url, nav_from.nav_from);
            }
            */
            if (responseFromServer.Contains(@"</span>1 &ndash; 0")) {
                logger.Debug("NEW DEAD LINK FOUND ON PAGE: " + url);
                logger.Debug("LINKED FROM PAGE: " + nav_from.nav_from);
                addError("DEAD", url, nav_from.nav_from);
            }

            foreach (Match m in Regex.Matches(responseFromServer, @"<img[^s]+src=""""", RegexOptions.Singleline)) {
                logger.Error("IMAGE NOT FOUND: " + m.Value.ToString());
            }
                   

            // We're done processing url, add to pagesVisited
            //lock (pagesVisited)
            Interlocked.Increment(ref pagesVisitedCount);
            pagesVisited.TryAdd(url, nav_from.nav_from);

            // Not a product, lets generate a manifest
            createManifest(responseFromServer, url, nav_from);

        }


    }
    struct nav {
        public string nav_from;
        public int depth;
        public nav(string Nav_from, int Depth) {
            nav_from = Nav_from;
            depth = Depth;
        }
    }
}
