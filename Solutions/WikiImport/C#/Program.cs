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

            foreach (TraceListener lsnr in Trace.Listeners)
                Console.WriteLine("{0}: {1}", lsnr.Name, lsnr.GetType());

            Trace.WriteLine("Importing wiki...");

            //string fn = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki-sm.xml"; // small piece of wiki for debug purposes
            WikiFile file = new WikiFile(this.settings.SourceFile.FullName);
            file.Parse();

            int idx = 0;
            long sumSize = 0;
            int maxLen = 0;
            int fileCnt = 0;
            Dictionary<int, int> sizeStat = new Dictionary<int, int>();
            foreach (WikiFile.Page pg in file.Pages)
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
            long avgSize = sumSize / file.Pages.Count;
            Trace.WriteLine("--- Some statistic ---");
            string info = string.Format(
                "{1} notes in total{0}" +
                "{2} max note length{0}" +
                "{3} average note length{0}" +
                "{4} summary length of all notes{0}" +
                "{5} file:* records{0}" +
                "{6} sizes in statistic:",
                "\n",
                file.Pages.Count, maxLen, avgSize, sumSize, fileCnt, sizeStat.Count);
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

            WikiBrowser saver = new WikiBrowser(this.settings.BaseWikiUrl);

            if (this.settings.WikiUser != null)
                saver.LoginToWiki(this.settings.WikiUser, this.settings.WikiPassword);

            foreach (WikiFile.Page pg in file.Pages)
            {
                saver.OpenWikiEditor(pg);
                saver.SubmitWikiPage();

                Thread.Sleep(1000);
            }
        }

        protected ToolSettings settings = new ToolSettings();
    }

}