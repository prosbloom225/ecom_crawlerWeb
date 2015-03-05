using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.SqlServer.Server;
using log4net;

namespace crawlerConsole {
    internal class Program {
        private static void Main(string[] args) {
            //Crawler.init();
            //Crawler.crawlerThreadStarter();
            //ManifestFactory.createMissingImageList(null);
            ManifestFactory.addDepartmentNumbers(ManifestFactory.createMissingImageList(null));

        }
    }

    public class Crawler {
        // DEBUG
        private static int debug = 0;
        private static bool catalogCheckFlag = false;
        // END DEBUG

        /** VAR DECLARE */
        private static Stopwatch stopWatch = new Stopwatch();
        private static int imageCount = 0;
        private static int missingImageCount = 0;
        private static List<string> fakeImageTags;

        private static ConcurrentDictionary<string, string> pagesToVisit;
        private static ConcurrentDictionary<string, string> pagesVisited;
        private static ConcurrentDictionary<string, string> productsVisited;
        private static Queue<string> seedStorage;

        private static int seedThreadRestarts = -1;
        private static Thread seedThread;
        private static int crawlerThreadCount;
        // TODO - temp set threads for testing
        private static int crawlerMaxThreads = 150;
        private static bool doneCrawling = false;

        private const string userAgent = "Mozilla/5.0 (Windows NT 5.1; rv:31.0) Gecko/20100101 Firefox/31.0";
        private static log4net.ILog logger = log4net.LogManager.GetLogger("Main");
        private static log4net.ILog icsLog = log4net.LogManager.GetLogger("ICS");

        // Locks
        private static object crawlerThreadCountLock = new object();
        private static object consoleLock = new object();

        /** END VAR DECLARE */

        public static void init() {
            // Init dataStructures
            pagesToVisit = new ConcurrentDictionary<string, string>();
            pagesVisited = new ConcurrentDictionary<string, string>();
            productsVisited = new ConcurrentDictionary<string, string>();
            seedStorage = new Queue<string>();
            seedThread = new Thread(seedMethod);
            seedThread.Name = "seedThread";

            // Static webMethod vars
            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = 10000;


            // Set initial seeding for products
            seedProducts();


            // Init logging
            log4net.Config.XmlConfigurator.Configure();
            logger.Debug("Init complete.");
            Console.Clear();
        }
        public int getPagesVisitedCount() {
            lock (pagesVisited)
                return Crawler.pagesVisited.Count;
        }
        public int getPagesToVisitCount() {
            lock (pagesToVisit)
                return Crawler.pagesToVisit.Count;
        }

        public int getProductsVisitedCount() {
            return Crawler.productsVisited.Count;
        }

        public int getCurrentThreadCount() {
            lock (crawlerThreadCountLock)
                return Crawler.crawlerThreadCount;
        }

        /** Test methods */

        public void testCrawl() {
            Console.Clear();
            Crawler.init();
            Crawler.crawlerThreadStarter();
        }
        public static void output() {
            if (Monitor.TryEnter(consoleLock, new TimeSpan(0, 0, 1))) {
                try {
                    Console.SetCursorPosition(0, 0);
                    Console.Write(new string(' ', Console.WindowWidth) + "\r");
                    Console.Write("\rProducts: " + productsVisited.Count + " - " + "Threads: " + crawlerThreadCount);
                    Console.SetCursorPosition(0, 1);
                    Console.Write(new string(' ', Console.WindowWidth) + "\r");
                    Console.Write("\rImages: " + imageCount + " - " + "Missing Images: " + missingImageCount);
                    Console.SetCursorPosition(0, 2);
                    Console.Write(new string(' ', Console.WindowWidth) + "\r");
                    Console.Write("\rPages Visited: " + pagesVisited.Count + " - " + "Pages to Visit: " + pagesToVisit.Count + " - " + "Seeds: " + seedStorage.Count);
                    Console.SetCursorPosition(0, 3);
                    Console.Write(new string(' ', Console.WindowWidth) + "\r");
                    Console.Write("\rElapsed: " + (stopWatch.ElapsedMilliseconds / 1000));
                } finally {
                    Monitor.Exit(consoleLock);
                }
            }
        }
        public static void nonlockingOutput() {
            new Task(output).Start();
        }

        /** Begin static methods */

        private static void crawlSite() {
            stopWatch.Start();
            String targetPage = "";
            String navFrom = "";
            while (true) {
                try {
                    targetPage = "";
                    navFrom = "";

                    //lock (pagesToVisit) {
                        if (pagesToVisit.Count != 0) {
                            foreach (KeyValuePair<string, string> kp in pagesToVisit) {
                                // lets have a prefer for images, just get them out of the way
                                if (kp.Key.Contains("edgesuite.net")) {
                                    targetPage = kp.Key;
                                    navFrom = kp.Value;
                                    break;
                                }
                            }
                            if (targetPage == "")
                                foreach (KeyValuePair<string, string> kp in pagesToVisit) {
                                    // lets have a prefer for products now
                                    if (kp.Key.Contains("prd-")) {
                                        targetPage = kp.Key;
                                        navFrom = kp.Value;
                                        break;
                                    }
                                }

                            // Ok we have no images or products queue'd, lets move on to the rest
                            if (targetPage == "")
                                foreach (KeyValuePair<string, string> kp in pagesToVisit) {
                                    targetPage = kp.Key;
                                    navFrom = kp.Value;
                                    break;
                                }

                        } else {
                            // No pages to crawl, wait for the seeds to repopulate
                            Thread.Sleep(5000);
                        }

                    // Lock Bracket
                    //}

                    // Ok, we're going to getPage, remove from pagesToVisit
                    string triedRemove;
                    pagesToVisit.TryRemove(targetPage, out triedRemove);
                    getPage(targetPage, navFrom);
                    // Debug output
                    if (debug==1){
                    //lock (consoleLock) {
                        /*
                        if (pagesToVisit.Count > 30000) {
                            using (var writer = new StreamWriter(Environment.CurrentDirectory + @"\pagesToVisit.csv")) {
                                foreach (var pair in pagesToVisit) {
                                    writer.WriteLine("{0};{1};", pair.Key, pair.Value);
                                }
                            }
                        }
                        if (pagesVisited.Count > 50000) {
                            using (var writer = new StreamWriter(Environment.CurrentDirectory + @"\pagesVisited.csv")) {
                                foreach (var pair in pagesVisited) {
                                    writer.WriteLine("{0};{1};", pair.Key, pair.Value);
                                }
                            }
                        }
                        */

                    }
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

        public static void crawlerThreadStarter() {
            while (true) {
                // check on the seedThread, he may get sleepy...
                if (!seedThread.IsAlive) {
                    // Clear!
                    seedThread.Start();
                    seedThreadRestarts++;
                    // wait a little bit for the seed thread to do its thing
                    Thread.Sleep(1000);
                }
                Thread.Sleep(3000);
                lock (crawlerThreadCountLock) {
                    while (crawlerThreadCount <= crawlerMaxThreads - 1) {
                        Thread sitecrawler = new Thread(crawlSite);
                        sitecrawler.IsBackground = true;
                        sitecrawler.Start();
                        sitecrawler.Name = "crawlerThread" + crawlerThreadCount;
                        lock (crawlerThreadCountLock)
                            crawlerThreadCount++;
                        Thread.Sleep(200);
                    }
                }
                if (doneCrawling) {
                    return;
                }
                // We're done, finish the crawl and exit the loop
                if (pagesToVisit.Count == 0 && crawlerThreadCount == 0) {
                    doneCrawling = true;
                }
            }
        }

        public static void seedProducts() {
            for (int i = 0; i < 200000; i = i + 96) {
                try {
                    seedStorage.Enqueue(@"http://www.kohls.com/catalog.jsp?N=0&WS=" + i);
                } catch (Exception ex) {
                    logger.Error("Error in seeding products");
                    logger.Error(ex.Message);
                }
            }

        }

        private static void seedMethod() {
            while (1 == 1) {
                if (seedStorage.Count <= 0) {
                    // We're done.... or beginning?
                    seedProducts();
                } else {
                    if (pagesToVisit.Count <= 0) {
                        lock (pagesToVisit) {
                            pagesToVisit.TryAdd(seedStorage.Peek(), "seed");
                            seedStorage.Dequeue();
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

                    //Console.WriteLine(response.Headers["ETag"]);
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
        public static void getFakeImages(Size size) {
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

        private static bool shouldVisit(string url, string nav_from) {
            if (url.Contains("edgesuite.net"))
                nav_from = "img";
            // Ignore filetypes
            if (url.Contains(".ico") || url.Contains(".css"))
                return false;
            // Ignore non-kohls pages
            if (!url.Contains("kohls.com"))
                return false;
            // Ignore catalog.jsp pages, as we've seeded them all already
            if (nav_from.Contains("catalog.jsp") && url.Contains("catalog"))
                return false;
            if (url.Contains("catalog.jsp") && !url.Contains("N=0"))
                return false;
            if (url.Contains("cs.kohls.com"))
                return false;
            if (url.Contains("search.jsp") || url.Contains("/search/"))
                return false;
            if (url.Contains(".shtml"))
                return false;

            // Ignore catalog pages, wayy too many to count, might have to add them later
            // TODO - add catalog back in, have to strip the queryString first
            if (url.Contains("catalog/"))
                return false;
            return true;
        }

        private static void createManifest(string pageText, string url, string nav_from) {
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
                    if (shouldVisit(textMatch, nav_from)) {
                        try {
                            /*
                            // Product?
                            if (textMatch.Contains("prd-"))
                                foreach (Match m2 in Regex.Matches(textMatch, @"prd[^/]+", RegexOptions.Singleline)) {
                                    string tried;
                                    productsToVisit.TryAdd(m2.ToString(), url);
                                    break;
                                }
                            */
                            // final check, somehow null is sneaking through - probably fix later to improve performance
                            if (textMatch.Contains(@"/null"))
                                continue;
                            // Add to manifest
                            if (pagesToVisit.TryAdd(textMatch, url))
                                // Log the page manifest creation
                                logger.Debug("queueing: " + textMatch + " - from: " + url);
                            else logger.Debug("Queue failed, duplicate detected: " + url);
                        }
                            // We have a duplicate, move along
                        catch {
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
                if (Crawler.debug==1)
                    logger.Info(url + " - " + responseFromServer.Length);
            } catch (Exception ex) {
                logger.Debug("Exception in getHtml for page: " + url);
                logger.Debug(ex.Message);
            }

            return responseFromServer;
        }
        private static void getPage(string url, string nav_from) {
            // Check if visited, if so, return
            if (checkVisited(url))
                return;
            // Handle akamaized images
            if (url.Contains("media") && url.Contains("edgesuite.net") && !url.Contains(".swf")) {
                if (checkImage(url)) {
                    // TEST

                    string html = getHtml(nav_from);
                    if (!html.Contains(url)) {
                        // we have a problem with manifest creation
                        logger.Error("Error with manifest creation.  URL: " + url + " - Nav_From: " + nav_from);
                        return;
                    }
                    // END TEST
                    if ((nav_from.Contains("catalog.jsp") && catalogCheckFlag) || !nav_from.Contains("catalog.jsp"))
                        icsLog.Info(url + "," + nav_from);
                    logger.Error("Image coming soon:" + url + " - " + nav_from);
                    missingImageCount++;
                } else {
                    imageCount++;
                }
                pagesVisited.TryAdd(url, nav_from);
                return;
            }

            string responseFromServer = "";
            string department = "";
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
            responseFromServer = Regex.Replace(responseFromServer, startString + ".+?" + endString, "",
                RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, startStringATG + ".+?" + endStringATG, "",
                RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, @":80", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, @":443", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "<link rel=\"canonical.+?>", "",
                RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "'[+].*[+]'", "(IGNORE)", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "MORE TO CONSIDER" + ".+?" + "<!-- ForEach Ends -->",
                "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer,
                "MORE TO CONSIDER" + ".+?" + "<!-- Right panel Ends -->", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer,
                "<script type=\"text/javascript\"" + ".+?" + "</script>", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, ";jsessionid=[a-zA-Z0-9]+!\\-*[0-9]+!\\-*[0-9]+", "",
                RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, ";jsessionid=[a-zA-Z0-9]+!\\-*[0-9]+", "",
                RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "WS=0\\&", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, "\\&WS=0", "", RegexOptions.Singleline);
            responseFromServer = Regex.Replace(responseFromServer, @"&S=\d", "", RegexOptions.Singleline);
            startString = "<ul id=\"navigation\">";
            endString = "</noscript>";
            responseFromServer = Regex.Replace(responseFromServer, startString + ".+?" + endString, "",
                RegexOptions.Singleline);
            startString = "<!-- BEGIN RELATED -->";
            endString = "<!-- END MORE-RESULTS -->";
            responseFromServer = Regex.Replace(responseFromServer, startString + ".+?" + endString, "",
                RegexOptions.Singleline);
            // Remove cs.kohls.com chainlinks..  lets do it now to speed up building manifest
            responseFromServer = Regex.Replace(responseFromServer, @"http(s)?:\/\/cs\.kohls\.com\/.+?;$", "", RegexOptions.Singleline);

            logger.Debug("Regex complete.  Size: " + responseFromServer.Length);
            if (responseFromServer.Contains("wrongSite is assigned"))
                logger.Debug("wrongSite ERROR: " + url);

            // We're done processing url, add to pagesVisited
            //lock (pagesVisited)
            pagesVisited.TryAdd(url, nav_from);

            // Not a product, lets generate a manifest
            createManifest(responseFromServer, url, nav_from);

        }

    }
}
