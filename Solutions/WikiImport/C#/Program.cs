/*
 * Simple/stright-forward solution to parse MediaWiki export file and try to import it via WebDriver
 *
 * by Dmitry Bond. (November 2023)
 *
*/

// See https://aka.ms/new-console-template for more information
using ImportWiki;

Console.WriteLine("Importing wiki...");

//string fn = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki-sm.xml"; // small piece of wiki
string fn = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki.xml"; // full wiki export
WikiFile file = new WikiFile(fn);
file.Parse();

long sumSize = 0;
int maxLen = 0;
foreach (WikiFile.Page pg in file.Pages)
{
    string txt = pg.LatestRevision.Text;
    sumSize += txt.Length;
    if (maxLen < txt.Length)
        maxLen = txt.Length;
}
long avgSize = sumSize / file.Pages.Count;
Console.WriteLine(string.Format(
    "{1} notes in total{0}" +
    "{2} max note length{0}" + 
    "{3} average note length{0}" + 
    "{4} summary length of all notes{0}", 
    Environment.NewLine,
    file.Pages.Count, maxLen, avgSize, sumSize));

//return ;

string baseUrl = "http://localhost:8080/w";
WikiBrowser saver = new WikiBrowser(baseUrl);
saver.LoginToWiki("dmitrybond", "Forget1234");

foreach (WikiFile.Page pg in file.Pages)
{
    saver.OpenWikiEditor(pg);
    saver.SubmitWikiPage();

    Thread.Sleep(1000);
}