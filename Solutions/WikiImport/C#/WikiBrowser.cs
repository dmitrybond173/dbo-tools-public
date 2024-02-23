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
using System.IO;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using XService.Utils;

namespace Wiki.Import
{
    public class WikiBrowser
    {
        public const string TEMPLATE_PageEditUrl = "${WikiBaseUrl}/index.php?title=${PageTitle}&action=edit";
        public const int CHUNK_SIZE = 0x2000;

        public WikiBrowser(ToolSettings pSettings) 
        {
            this.Settings = pSettings;
            this.WikiBaseUrl = this.Settings.BaseWikiUrl;

            //this.Driver = new ChromeDriver();

            var ffOptions = new FirefoxOptions();
            ffOptions.AcceptInsecureCertificates = true;
            this.Driver = new FirefoxDriver(ffOptions);

            //object x = this.Driver.Capabilities.GetCapability("acceptInsecureCerts");
        }

        public ToolSettings Settings { get; protected set; }

        public string WikiBaseUrl { get; protected set; }
        public FirefoxDriver Driver { get; protected set; }
        public string CurrentUrl { get; protected set; }
        public WikiFile.Page CurrentPage { get; protected set; }

        public void LoginToWiki(string pUser, string pPassword) 
        {
            Trace.WriteLine(string.Format("--- Wiki.Login[{0}, *****]", pUser));

            this.CurrentUrl = resolveUrl("${WikiBaseUrl}/index.php?title=Special:UserLogin");
            Driver.Navigate().GoToUrl(this.CurrentUrl);

            var name = Driver.FindElement(By.Name("wpName"));
            name.SendKeys(pUser);

            if (pPassword != null)
            {
                var passw = Driver.FindElement(By.Name("wpPassword"));
                passw.SendKeys(pPassword);
            }

            IWebElement loginButton = Driver.FindElement(By.Name("wploginattempt"));
            loginButton.Click();
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

        #region Implementation details

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
            }
            return result;
        }

        #endregion
    }
}
