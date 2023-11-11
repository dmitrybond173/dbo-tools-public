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
using ImportWiki;

TraceConfiguration.Register();

foreach (TraceListener lsnr in Trace.Listeners)
    Console.WriteLine("{0}: {1}", lsnr.Name, lsnr.GetType());

Trace.WriteLine("Importing wiki...");

//string fn = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki-sm.xml"; // small piece of wiki for debug purposes
string fn = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki.xml"; // full wiki export
WikiFile file = new WikiFile(fn);
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

string baseUrl = "http://localhost:8080/w";
WikiBrowser saver = new WikiBrowser(baseUrl);
saver.LoginToWiki("dmitrybond", "Forget1234");

foreach (WikiFile.Page pg in file.Pages)
{
    saver.OpenWikiEditor(pg);
    saver.SubmitWikiPage();

    Thread.Sleep(1000);
}