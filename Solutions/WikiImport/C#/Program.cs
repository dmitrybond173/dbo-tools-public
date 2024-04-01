/*
 * Simple/stright-forward solution to parse MediaWiki export file and try to import it via WebDriver
 * 
 * Problem: I exported data from old MediaWiki (1.19) but I cannot import it to any of modern MediaWiki versions.
 * 
 * by Dmitry Bond. (November 2023)
 *
*/

// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using System.Configuration;
using Wiki.Import;
using XService.Utils;

namespace Wiki.Import
{

    public class Program
    {
        public static TraceSwitch TrcLvl { get { return ToolSettings.TrcLvl; } }


        public static int Main(string[] args)
        {
            ToolSettings.Arguments = args;

            Program tool = new Program();
            return tool.Run(args);

        }

        Program()
        {
            TraceConfiguration.Register();
        }

        public int Run(string[] args)
        {
            this.settings.DisplayStartHeader();
            try
            {
                if (!this.settings.ParseCmdLine(args))
                    return 1;

                execute();

            }
            catch (Exception exc)
            {
                //Console.WriteLine("#chk.pnt.ERR");

                Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format(
                    "!ERR: {0}\nat {1}", ErrorUtils.FormatErrorMsg(exc), ErrorUtils.FormatStackTrace(exc)) : "");
                if (exc.InnerException != null)
                    Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format(
                        "  !ERR.internal: {0}", ErrorUtils.UnrollException(exc)) : "");

                if ((this.settings.Pause & ToolSettings.EPause.Error) == ToolSettings.EPause.Error)
                {
                    Trace.WriteLine("Press any key to abort...");
                    Console.ReadKey();
                }
            }

            return 0;
        }

        private void execute()
        {
            //string fn = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki-sm.xml"; // small piece of wiki for debug purposes
            this.wikiFile = new WikiFile(this.settings.SourceFile.FullName);
            this.wikiFile.Parse();

            switch (this.settings.Action)
            {
                case ToolSettings.EAction.Import: executeWikiImport(); break;
                case ToolSettings.EAction.ExportFiles: executeWikiFilesExport(); break;
                case ToolSettings.EAction.ImportFiles: executeWikiFilesImport(); break;
            }
        }

        private void executeWikiImport()
        {
            Trace.WriteLine("Importing wiki text (from XML backup file to wiki site)...");

            int idx = 0;
            long sumSize = 0;
            int maxLen = 0;
            int fileCnt = 0;
            Dictionary<int, int> sizeStat = new Dictionary<int, int>();
            foreach (WikiFile.Page pg in this.wikiFile.Pages)
            {
                pg.Index = idx;
                idx++;

                if (pg.Title.ToLower().StartsWith("file:"))
                    fileCnt++;

                string txt = pg.LatestRevision.Text;
                sumSize += txt.Length;
                if (maxLen < txt.Length)
                    maxLen = txt.Length;

                int szKb = (txt.Length + 1023) / 1024;
                int cnt = 0;
                if (!sizeStat.TryGetValue(szKb, out cnt))
                    cnt = 0;
                sizeStat[szKb] = cnt + 1;
            }
            long avgSize = sumSize / this.wikiFile.Pages.Count;
            Trace.WriteLine("--- Some statistic ---");
            string info = string.Format(
                "{1} notes in total{0}" +
                "{2} max note length{0}" +
                "{3} average note length{0}" +
                "{4} summary length of all notes{0}" +
                "{5} file:* records{0}" +
                "{6} sizes in statistic:",
                "\n",
                this.wikiFile.Pages.Count, maxLen, avgSize, sumSize, fileCnt, sizeStat.Count);
            string[] lines = info.Split('\n');
            foreach (string line in lines) Trace.WriteLine(line);
            List<KeyValuePair<int, int>> szRefs = new List<KeyValuePair<int, int>>();
            foreach (KeyValuePair<int, int> it in sizeStat)
                szRefs.Add(it);
            szRefs.Sort((n1, n2) => n1.Key.CompareTo(n2.Key));
            idx = 0;
            foreach (KeyValuePair<int, int> it in szRefs)
            {
                idx++;
                Trace.WriteLine(string.Format("\t#{0}: {1} kb = {2} records",
                    idx, it.Key, it.Value));
            }

            //return ; //<== debug: if you want to see only statistic

            if (this.settings.Simulation)
            {
                Trace.WriteLine(string.Format("* Nothing to do more in simulation mode."));
                return;
            }

            WikiBrowser saver = new WikiBrowser(this.settings);

            if (this.settings.WikiUser != null)
                saver.LoginToWiki(this.settings.WikiUser, this.settings.WikiPassword);

            foreach (WikiFile.Page pg in this.wikiFile.Pages)
            {
                string id = pg.Title;
                string reason;
                if (!isOkToProceed(id.ToLower(), out reason))
                {
                    Trace.WriteLine(string.Format("--- {0}: {1}", reason, pg.Title));
                    continue;
                }

                saver.OpenWikiEditor(pg);
                saver.SubmitWikiPage();

                Thread.Sleep(1000);
            }
        }

        private void executeWikiFilesImport()
        {
            Trace.WriteLine("Importing image files (from disk to wiki site)...");

            WikiBrowser saver = new WikiBrowser(this.settings);

            if (this.settings.WikiUser != null)
                saver.LoginToWiki(this.settings.WikiUser, this.settings.WikiPassword);

            int idx = 0;
            int fileCnt = 0;
            Dictionary<int, int> sizeStat = new Dictionary<int, int>();
            foreach (WikiFile.Page pg in this.wikiFile.Pages)
            {
                pg.Index = idx;
                idx++;

                if (!pg.Title.ToLower().StartsWith("file:")) continue;

                string id = pg.Title;
                string reason;
                if (!isOkToProceed(id.ToLower(), out reason))
                {
                    Trace.WriteLine(string.Format("--- {0}: {1}", reason, pg.Title));
                    continue;
                }

            }

        }

        private void executeWikiFilesExport()
        {
            Trace.WriteLine("Exporting image files (from wiki site to disk)...");

            WikiBrowser saver = new WikiBrowser(this.settings);

            if (this.settings.WikiUser != null)
                saver.LoginToWiki(this.settings.WikiUser, this.settings.WikiPassword);

            string ts = StrUtils.CompactNskTimestampOf(DateTime.Now).Substring(0, 8 + 1 + 4);
            DirectoryInfo dir = new DirectoryInfo("pics_" + ts);
            if (!dir.Exists)
            {
                Trace.WriteLine(string.Format("+++ Creating dir: [{0}]", dir.FullName));
                dir.Create();
            }

            DateTime t1 = DateTime.Now;
            int idx = 0;
            int okCnt = 0, failCnt = 0, skipCnt = 0;
            Dictionary<int, int> sizeStat = new Dictionary<int, int>();
            foreach (WikiFile.Page pg in this.wikiFile.Pages)
            {
                pg.Index = idx;
                idx++;

                if (!pg.Title.ToLower().StartsWith("file:")) continue;

                string id = pg.Title;
                string fn = StrUtils.GetAfterPattern(id, ":");
                string reason;
                if (!isOkToProceed(id.ToLower(), out reason))
                {
                    skipCnt++;
                    Trace.WriteLine(string.Format("--- {0}: {1}", reason, pg.Title));
                    continue;
                }

                string fileUrl;
                if (!saver.OpenWikiFilePage(pg, out fileUrl))
                {
                    failCnt++;
                    Trace.WriteLine(string.Format("--- FAIL: cannot determine file url: {0}", pg.Title));
                    saveFileRefFailure(dir, pg, "Fail to determine file url");
                    continue;
                }

                byte[] data;
                if (!saver.DownloadByApiCall(fileUrl, out data))
                {
                    failCnt++;
                    Trace.WriteLine(string.Format("--- FAIL: cannot download file: {0}", pg.Title));
                    saveFileRefFailure(dir, pg, "Fail to download");
                    continue;
                }

                FileInfo fi = new FileInfo(PathUtils.IncludeTrailingSlash(dir.FullName) + fn);
                Trace.WriteLine(string.Format(" + Saving {0} bytes to file: {1} => {2}", data.Length, fn, fi.FullName));
                using (FileStream fs = fi.Create()) 
                {
                    fs.Write(data, 0, data.Length);
                }
                fi.Refresh();
                Trace.WriteLine(string.Format("   = Saved. {0} bytes", fi.Length));
                okCnt++;
            }
            Trace.WriteLine(string.Format("=== SUMMARY: {0} files exported. Skip {1} files. Fail to export {2} files. Elapsed time = {3} sec", 
                okCnt, skipCnt, failCnt, (DateTime.Now - t1).TotalSeconds.ToString("N1") ));
        }

        private void saveFileRefFailure(DirectoryInfo pDir, WikiFile.Page pPage, string pReason)
        {
            Trace.WriteLine(string.Format(" -+ Add to failures-list: [{0}] -> {1}", pPage.Title, pReason));
            using (StreamWriter sw = File.AppendText(PathUtils.IncludeTrailingSlash(pDir.FullName) + "failuresList.txt"))
            {
                string info = string.Format("{0}     <== {1}", pPage.Title, pReason);
                sw.WriteLine(info);
            }
        }

        private bool isOkToProceed(string id, out string pReason)
        {
            pReason = null;
            if (this.settings.ExcludePages != null && this.settings.ExcludePages.Contains(id.ToLower()))
            {
                pReason = "page excluded";
                return false;
            }
            if (this.settings.IncludePages != null && !this.settings.IncludePages.Contains(id.ToLower()))
            {
                pReason = "page is not included";
                return false;
            }
            return true;
        }


        protected ToolSettings settings = new ToolSettings();
        protected WikiFile wikiFile = null;
    }

}