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

namespace ImportWiki
{
    public class WikiBrowser
    {
        public const string TEMPLATE_PageEditUrl = "${WikiBaseUrl}/index.php?title=${PageTitle}&action=edit";
        public const int CHUNK_SIZE = 0x2000;

        public WikiBrowser(string pWikiBaseUrl) 
        {
            this.WikiBaseUrl = pWikiBaseUrl;
            this.Driver = new ChromeDriver();
        }

        public string WikiBaseUrl { get; protected set; }
        public ChromeDriver Driver { get; protected set; }
        public string CurrentUrl { get; protected set; }
        public WikiFile.Page CurrentPage { get; protected set; }

        public void LoginToWiki(string pUser, string pPassword) 
        {
            this.CurrentUrl = resolveUrl("${WikiBaseUrl}/index.php?title=Special:UserLogin");
            Driver.Navigate().GoToUrl(this.CurrentUrl);

            var name = Driver.FindElement(By.Name("wpName"));
            var passw = Driver.FindElement(By.Name("wpPassword"));
            name.SendKeys(pUser);
            passw.SendKeys(pPassword);

            IWebElement loginButton = Driver.FindElement(By.Name("wploginattempt"));
            loginButton.Click();
        }

        public void OpenWikiEditor(WikiFile.Page pPage)
        {
            this.CurrentPage = pPage;
            this.CurrentUrl = resolveUrl(TEMPLATE_PageEditUrl);
            string title = this.CurrentPage.Title;
            //title = CGI.escape(@currentPage.title)
            Console.WriteLine(string.Format("--- Open WikiEditor[{0}]", this.CurrentUrl));
            Driver.Navigate().GoToUrl(this.CurrentUrl);
        }

        public void SubmitWikiPage()
        {
            var textArea = Driver.FindElement(By.Name("wpTextbox1"));
            string txt = CurrentPage.LatestRevision.Text;
            string[] chunks = splitToChunks(txt);
            Console.WriteLine(string.Format(" * Sending text( {0} chars; {1} chunks )...", txt.Length, chunks.Length));
            int idx = 0;
            foreach (string chunk in chunks)
            {
                idx++;
                Console.WriteLine(string.Format("   + chunk# {0}", idx));
                textArea.SendKeys(chunk);
                Thread.Sleep(1000);
            }
            Console.WriteLine(string.Format("   = text sent."));

            var button = Driver.FindElement(By.Name("wpSave"));
            if (button == null)
                throw new Exception("[Save] button not found on the page!");

            Console.WriteLine(string.Format(" * Saving changes..."));
            button.Click();
            Console.WriteLine(string.Format("   = Saved."));
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
                result = StrUtils.ReplaceCI(result, "${PageTitle}", this.CurrentPage.Title);
            return result;
        }

        #endregion
    }
}
