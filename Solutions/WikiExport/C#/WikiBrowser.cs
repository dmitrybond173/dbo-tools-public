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
using System.Text;
using System.Xml;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using XService.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace Tools.Wiki
{
    public class WikiBrowser
    {
        public const string DEFAULT_WikiBaseUrl = "http://localhost:8080/w";
        public const string TEMPLATE_PageEditUrl = "${WikiBaseUrl}/index.php?title=${PageTitle}&action=edit";

        public const string DEFAULT_Categories = "(Main)\nTalk\nUser\nUser talk\nBTO Wiki\nBTO Wiki talk\n"
            + "File\nFile talk\nMediaWiki\nMediaWiki talk\nTemplate\nTemplate talk\nHelp\nHelp talk\n"
            + "Category\nCategory talk";

        public const int CHUNK_SIZE = 0x2000;

        public WikiBrowser(string pWikiBaseUrl) 
        {
            this.Categories = new List<string>();
            this.PagesList = new List<string>();
            this.MacroValues = new Dictionary<string, string>();

            this.WikiBaseUrl = pWikiBaseUrl;
            this.IsNewVersion = true;

            //this.Driver = new ChromeDriver();
            this.Driver = new FirefoxDriver();

            loadConfiguration();
        }

        public string WikiBaseUrl { get; protected set; }
        public string WikiUser { get; protected set; }
        public string WikiPassword { get; protected set; }
        public bool IsNewVersion { get; protected set; }
        public Dictionary<string, string> MacroValues { get; protected set; }
        public FirefoxDriver Driver { get; protected set; }
        public string CurrentUrl { get; protected set; }
        public List<string> Categories { get; protected set; }
        public List<string> PagesList { get; protected set; }

        public void Close()
        { 
            this.Driver.Close();
        }

        public void LoginToWiki(string pUser = null, string pPassword = null) 
        {
            if (pUser == null)
                pUser = this.WikiUser;
            if (pPassword == null)
                pPassword = this.WikiPassword;

            this.CurrentUrl = resolveUrl("${WikiBaseUrl}/index.php?title=Special:UserLogin");
            Driver.Navigate().GoToUrl(this.CurrentUrl);

            var name = Driver.FindElement(By.Name("wpName"));
            var passw = Driver.FindElement(By.Name("wpPassword"));
            name.SendKeys(pUser);
            passw.SendKeys(pPassword);

            string id = (this.IsNewVersion ? "wploginattempt" : "wpLoginattempt");
            IWebElement loginButton = Driver.FindElement(By.Name(id));
            if (loginButton != null)
                loginButton.Click();
            else
                throw new Exception("Fail to find submit button!");
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

        public void OpenWikiEditor()
        {
            //title = CGI.escape(@currentPage.title)
            Trace.WriteLine(string.Format("--- #. Open WikiEditor[]"));
            Driver.Navigate().GoToUrl(this.CurrentUrl);
        }

        public void SubmitWikiPage()
        {
            var textArea = Driver.FindElement(By.Id("namespace"));

            // WARNING!
            // Seems WebDriver cannot set long text into TextArea!
            // Even when trying to do that with chunks...
            /*
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
            */
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
            foreach (KeyValuePair<string, string> kvp in this.MacroValues)
            {
                result = StrUtils.ReplaceCI(result, "${" + kvp.Key + "}", kvp.Value);
            }

            //if (this.CurrentPage != null)
            //    result = StrUtils.ReplaceCI(result, "${PageTitle}", this.CurrentPage.Title);

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
