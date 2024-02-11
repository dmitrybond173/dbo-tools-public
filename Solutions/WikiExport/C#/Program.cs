/*
 * Simple/stright-forward solution to parse MediaWiki export file and try to import it via WebDriver
 * 
 * Problem: I exported data from old MediaWiki (1.19) but I cannot import it to any of modern MediaWiki versions.
 * 
 * by Dmitry Bond. (November 2023)
 *
*/

// See https://aka.ms/new-console-template for more information

//using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Xml;
using Tools.Wiki;
using XService;
using XService.Utils;

TraceConfiguration.Register();
Console.WriteLine("--- {0} trace listeners:", Trace.Listeners.Count);
foreach (TraceListener lsnr in Trace.Listeners)
    Console.WriteLine("  + {0}: {1}", lsnr.Name, lsnr.GetType());

Trace.WriteLine("Exporting wiki...");
try
{
    WikiBrowser wiki = new WikiBrowser(null);

    wiki.LoginToWiki(); // "dmitrybond", "Forget1234");

    wiki.GetPagesList();

    wiki.Close();
}
catch (Exception exc)
{
    Trace.WriteLine(ErrorUtils.FormatErrorMsg(exc));
    Trace.WriteLine("at " + ErrorUtils.FormatStackTrace(exc));
}


