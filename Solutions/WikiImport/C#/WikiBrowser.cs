/*
 * Simple/stright-forward solution to parse MediaWiki export file and try to import it via WebDriver
 *
 * Handler of WebDriver communication
 *
 * by Dmitry Bond. (November 2023)
 *
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using XService.Utils;

namespace Wiki.Import
{
    public class WikiBrowser
    {
        public const string DEFAULT_WikiBaseUrl = "http://localhost:8080/w";
        public const string DEFAULT_Categories = "(Main)\nTalk\nUser\nUser talk\nBTO Wiki\nBTO Wiki talk\n"
            + "File\nFile talk\nMediaWiki\nMediaWiki talk\nTemplate\nTemplate talk\nHelp\nHelp talk\n"
            + "Category\nCategory talk";

        public const string TEMPLATE_PageEditUrl = "${WikiBaseUrl}/index.php?title=${PageTitle}&action=edit";
        public const string TEMPLATE_FilePageUrl = "${WikiBaseUrl}/index.php/${FileTitle}";

        public const int CHUNK_SIZE = 0x2000;

        public WikiBrowser(ToolSettings pSettings) 
        {
            this.Settings = pSettings;
            this.WikiBaseUrl = this.Settings.BaseWikiUrl;
            this.IsNewVersion = true;

            this.Categories = new List<string>();
            this.PagesList = new List<string>();
            this.MacroValues = new Dictionary<string, string>();

            //this.Driver = new ChromeDriver();

            var ffOptions = new FirefoxOptions();
            ffOptions.AcceptInsecureCertificates = true;
            this.Driver = new FirefoxDriver(ffOptions);

            //object x = this.Driver.Capabilities.GetCapability("acceptInsecureCerts");

            loadConfiguration();
        }

        public ToolSettings Settings { get; protected set; }

        public string WikiBaseUrl { get; protected set; }
        public string WikiUser { get; protected set; }
        public string WikiPassword { get; protected set; }
        public bool IsNewVersion { get; protected set; }
        public Dictionary<string, string> MacroValues { get; protected set; }
        public FirefoxDriver Driver { get; protected set; }
        public string CurrentUrl { get; protected set; }
        public WikiFile.Page CurrentPage { get; protected set; }
        public List<string> Categories { get; protected set; }
        public List<string> PagesList { get; protected set; }

        public void Close()
        {
            this.Driver.Close();
        }

        public void LoginToWiki(string pUser, string pPassword) 
        {
            if (pUser == null)
                pUser = this.WikiUser;
            if (pPassword == null)
                pPassword = this.WikiPassword;

            this.CurrentUrl = resolveUrl("${WikiBaseUrl}/index.php?title=Special:UserLogin");
            Driver.Navigate().GoToUrl(this.CurrentUrl);

            var name = Driver.FindElement(By.Name("wpName"));
            Thread.Sleep(100);
            var passw = Driver.FindElement(By.Name("wpPassword"));
            Thread.Sleep(100);
            name.SendKeys(pUser);
            Thread.Sleep(300);
            passw.SendKeys(pPassword);
            Thread.Sleep(300);

            string id = (this.IsNewVersion ? "wploginattempt" : "wpLoginattempt");
            IWebElement loginButton = Driver.FindElement(By.Name(id));
            if (loginButton != null)
                loginButton.Click();
            else
                throw new Exception("Fail to find submit button!");
        }

        public bool OpenWikiFilePage(WikiFile.Page pPage, out string pFileUrl)
        {
            pFileUrl = null;

            this.CurrentPage = pPage;
            this.CurrentUrl = resolveUrl(TEMPLATE_FilePageUrl);
            string title = this.CurrentPage.Title;
            string fn = StrUtils.GetAfterPattern(title, ":");
            //title = CGI.escape(@currentPage.title)
            Trace.WriteLine(string.Format("--- #{0}. Open WikiFilePage[{1}]", pPage.Index, this.CurrentUrl));
            Driver.Navigate().GoToUrl(this.CurrentUrl);

            IWebElement body = this.Driver.FindElement(By.TagName("body"));
            if (body.Text.Contains("No file by this name exists"))
                return false;

            bool result = false;
            IReadOnlyCollection<IWebElement> elements = this.Driver.FindElements(By.TagName("a"));
            Trace.WriteLine(string.Format(" * {0} <a> elements found", elements.Count));
            string altFn = fn.Replace(" ", "_");
            foreach (IWebElement item in elements)
            {
                string txt = item.Text.Trim();
                if (string.IsNullOrEmpty(txt)) continue;

                string link = item.GetAttribute("href");
                bool isMatch = StrUtils.IsSameText(txt, "Original file")
                    || StrUtils.IsSameText(txt, "Full resolution")
                    || StrUtils.IsSameText(txt, fn)
                    || StrUtils.IsSameText(txt, altFn);
                if (isMatch)
                {
                    result = true;
                    pFileUrl = link;
                    break;
                }
            }

            return result;

        }

        public void GetPagesList()
        {
            string linkTemplate = "${WikiBaseUrl}/index.php?title=Special:AllPages&namespace=${NsIndex}";
            if (!this.IsNewVersion)
                linkTemplate += "&from=*&to=*";
            int nsIndex = 0;
            do
            {
                this.MacroValues["NsIndex"] = nsIndex.ToString();
                this.CurrentUrl = resolveUrl(linkTemplate);

                Trace.WriteLine(string.Format("--- GO-TO: {0}", this.CurrentUrl));
                Driver.Navigate().GoToUrl(this.CurrentUrl);

                IWebElement body = this.Driver.FindElement(By.TagName("body"));
                if (body.Text.Contains("does not have namespace"))
                {
                    Trace.WriteLine(string.Format(" <-- END"));
                    break;
                }

                string categoryName = "";
                if (nsIndex < this.Categories.Count)
                    categoryName = this.Categories[nsIndex];
                Trace.WriteLine(string.Format(" * Category#{0}: [{1}]", nsIndex, categoryName));

                IWebElement nextPage;
                do
                {
                    nextPage = null;

                    int cnt = this.PagesList.Count;

                    IReadOnlyCollection<IWebElement> elements = this.Driver.FindElements(By.TagName("a"));
                    Trace.WriteLine(string.Format(" * {0} <a> elements found", elements.Count));
                    foreach (IWebElement item in elements)
                    {
                        string txt = item.Text.Trim();
                        if (string.IsNullOrEmpty(txt)) continue;

                        string title = item.GetAttribute("title");
                        string link = item.GetAttribute("href");
                        if (txt.ToLower().StartsWith("next page"))
                            nextPage = item;

                        if (link.ToLower().Contains("/index.php/"))
                        {
                            string id = title;
                            if (title != null && isMatchTitle(title, txt, categoryName))
                            {
                                if (!txt.Contains(':') && nsIndex > 0)
                                    id = string.Format("{0}:{1}", categoryName, txt);
                                if (!string.IsNullOrEmpty(id))
                                    this.PagesList.Add(id);
                            }
                            else
                                Trace.WriteLine(string.Format(" --- SKIP: [{0}] / [{1}]", title, txt));
                        }
                    }
                    Trace.WriteLine(string.Format(" + {0} page added to list", this.PagesList.Count - cnt));

                    if (nextPage != null)
                    {
                        Trace.WriteLine(string.Format(" * jump to next portion"));
                        nextPage.Click();
                    }
                    else
                    {
                        nsIndex++;
                        this.PagesList.Add("");
                    }
                }
                while (nextPage != null);
            }
            while (true);

            string fn = "AllPagesList.txt";
            Trace.WriteLine(string.Format(" +++ saving pages list to file: {0}", fn));
            using (StreamWriter sw = File.CreateText(fn))
            {
                foreach (string pg in this.PagesList)
                    sw.WriteLine(pg);
            }
        }

        public void OpenWikiEditor(WikiFile.Page pPage)
        {
            this.CurrentPage = pPage;
            this.CurrentUrl = resolveUrl(TEMPLATE_PageEditUrl);
            string title = this.CurrentPage.Title;
            //title = CGI.escape(@currentPage.title)
            Trace.WriteLine(string.Format("--- #{0}. Open WikiEditor[{1}]", pPage.Index, this.CurrentUrl));
            Driver.Navigate().GoToUrl(this.CurrentUrl);
        }

        public void SubmitWikiPage()
        {
            Trace.WriteLine(string.Format("--- Wiki.SubmitPage()"));

            WebElement textArea = (WebElement)Driver.FindElement(By.Name("wpTextbox1"));
            textArea.Clear();
            //textArea.SendKeys(Keys.CONTROL, 'a');
            //textArea.SendKeys(Keys.DELETE);

            string txt = CurrentPage.LatestRevision.Text;
            if (this.Settings.Replacer != null)
            {
                int cnt = this.Settings.Replacer.Replace(ref txt);
                if (cnt > 0)
                    Trace.WriteLine(string.Format(" ! {0} replacements were made", cnt));
            }

            // WARNING!
            // Seems WebDriver cannot set long text into TextArea!
            // Even when trying to do that with chunks...
            string[] chunks = splitToChunks(txt);
            Trace.WriteLine(string.Format(" * Sending text( {0} chars; {1} chunks )...", txt.Length, chunks.Length));
            int idx = 0;
            foreach (string chunk in chunks)
            {
                idx++;
                Trace.WriteLine(string.Format("   + chunk# {0}", idx));
                textArea.SendKeys(chunk);
                Thread.Sleep(1000);
            }
            Trace.WriteLine(string.Format("   = text sent."));

            var button = Driver.FindElement(By.Name("wpSave"));
            if (button == null)
                throw new Exception("[Save] button not found on the page!");

            Trace.WriteLine(string.Format(" * Saving changes..."));
            button.Click();
            Trace.WriteLine(string.Format("   = Saved."));
        }

        public bool DownloadByApiCall(string pFileUrl, out byte[] data)
        {
            Trace.WriteLine(string.Format(" = downloading: {0}", pFileUrl));

            var uri = new Uri(this.Driver.Url);
            var path = pFileUrl;

            data = null;
            bool result = false;
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(path);

                webRequest.CookieContainer = new CookieContainer();
                foreach (var cookie in this.Driver.Manage().Cookies.AllCookies)
                    webRequest.CookieContainer.Add(new System.Net.Cookie(cookie.Name, cookie.Value,
                        cookie.Path, string.IsNullOrWhiteSpace(cookie.Domain) ? uri.Host : cookie.Domain));

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var ms = new MemoryStream();
                var responseStream = webResponse.GetResponseStream();
                responseStream.CopyTo(ms);
                data = ms.ToArray();
                responseStream.Close();
                webResponse.Close();

                result = true;
            }
            catch (WebException webex)
            {
                var errResp = webex.Response;
                using (var respStream = errResp.GetResponseStream())
                {
                    var reader = new StreamReader(respStream);
                    Trace.WriteLine(string.Format("WEB-FAILURE: code={0}, msg={1}", webex.Status, webex.Message));
                }
            }

            return result;
        }

        #region Implementation details

        private bool isMatchTitle(string pTitle, string pText, string pCategoryName)
        {
            if (pTitle.Contains(':') && pTitle.ToLower().StartsWith(pCategoryName.ToLower()))
                pTitle = pTitle.Remove(0, pCategoryName.Length + 1); // remove CategoryName + ':'
            return StrUtils.IsSameText(pTitle, pText);
        }

        private string[] splitToChunks(string pText)
        {
            List<string> result = new List<string>();            
            while (pText.Length > 0)
            { 
                string portion = (pText.Length > CHUNK_SIZE ? pText.Substring(0, CHUNK_SIZE) :  pText);
                pText = pText.Remove(0, portion.Length);    
                result.Add(portion);
            }
            return result.ToArray();
        }

        private string resolveUrl(string pUrl)
        {
            string result = pUrl;
            result = StrUtils.ReplaceCI(result, "${WikiBaseUrl}", this.WikiBaseUrl);
            if (this.CurrentPage != null)
            {
                string title = this.Settings.UrlEncode(this.CurrentPage.Title);
                result = StrUtils.ReplaceCI(result, "${PageTitle}", title);
                result = StrUtils.ReplaceCI(result, "${FileTitle}", this.CurrentPage.Title);
            }

            foreach (KeyValuePair<string, string> kvp in this.MacroValues)
            {
                result = StrUtils.ReplaceCI(result, "${" + kvp.Key + "}", kvp.Value);
            }

            return result;
        }

        private void loadConfiguration()
        {
            string baseUrl = DEFAULT_WikiBaseUrl;
            string s = ConfigurationManager.AppSettings["Wiki.BaseUrl"];
            if (!string.IsNullOrEmpty(s))
                this.WikiBaseUrl = s;

            s = ConfigurationManager.AppSettings["Wiki.IsNewVersion"];
            if (!string.IsNullOrEmpty(s))
                this.IsNewVersion = StrUtils.GetAsBool(s);

            s = ConfigurationManager.AppSettings["Wiki.Username"];
            if (!string.IsNullOrEmpty(s))
                this.WikiUser = s;
            s = ConfigurationManager.AppSettings["Wiki.Password"];
            if (!string.IsNullOrEmpty(s))
                this.WikiPassword = s;

            s = DEFAULT_Categories;
            XmlElement cfgNode = (XmlElement)ConfigurationManager.GetSection("CategoriesToExport");
            if (cfgNode != null)
            {
                s = XmlUtils.LoadText(cfgNode);
            }
            string[] list = s.Split('\n');
            foreach (string str in list)
            {
                s = str.Trim();
                if (string.IsNullOrEmpty(s)) continue;
                this.Categories.Add(s);
            }
        }

        #endregion
    }
}
