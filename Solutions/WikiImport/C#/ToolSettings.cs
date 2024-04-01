/*
 * Simple/stright-forward solution to parse MediaWiki export file and try to import it via WebDriver
 *
 * Handler of app settings
 *
 * by Dmitry Bond. (February 2024)
 *
*/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
//using System.Security.RightsManagement;
using Microsoft.SqlServer.Server;
using System.Web;
using OpenQA.Selenium.DevTools.V117.Network;

#if _UseDB
using System.Data.SQLite;
#endif
using XService.Utils;

namespace Wiki.Import
{
    public class ToolSettings
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("TraceLevel", "TraceLevel");

        public const string DEFAULT_Filespecs = "*.txt";

        #region Tool Messages

        public const string HELP_LICENSE_TEXT =
            "$(AppTitle). \r\n" +
            "$(AppCopyright) \r\n" +
            "";

        public const string HELP_TEXT_TITLE =
            "MediaWiki Import Tool. Version $(version)\r\n" +
            "";

        public const string HELP_TEXT_USAGE =
            "Usage: $(AppName) wikiDbFilename [options]\r\n" +
            "";

        public const string HELP_TEXT =
            "\r\n" +
            HELP_TEXT_USAGE +
            //123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_
            "\r\n" +
            "Supported options:\r\n" +
            "  -?  - print this message\r\n" +
            "  -act, --action={actionName}  - wiki-import action to execute\n" +
            "      Supported values are: Import (default), ImportFiles, ExportFiles\n" +
            "  -cd, --change-dir={directory}  - switch directory when started\n" +
            "  -ipl, --include-pages-list={filename}  - list of pages to include\n" +
            "  -epl, --exclude-pages-list={filename}  - list of pages to exclude\n" +
            "  -p, --pause={list}  - set pause params, comma-separated list of: error, begin, end, always\n" +
            "  -simulation  - simulation mode\n" +
            "  -wu, --wiki-user={wikiUserName}  - wiki user name\n" +
            "  -wp, --wiki-password={wikiUserPassword}  - wiki user password\n" +
            "  -url={baseWikiUrl}  - base wiki url\n" +
            "\r\n" +
            "Examples:\r\n" +
            "  0. Import from specified XML backup into wiki site\r\n" +
            "    $(AppName) MyWikiBckp.xml -url=http://localhost:8080/w -wu=dbondare -wp=Forget1234 -epl=excludedPages.txt\r\n" +
            "  1. Export files from wiki site to disk\r\n" +
            "    $(AppName) MyWikiBckp.xml -act=ExportFiles -url=http://localhost:8080/w -wu=dbondare -wp=Forget1234\r\n" +
            "\r\n" +
            "";

        #endregion // Tool Messages

        [Flags]
        public enum EPause
        {
            None    = 0x00,
            Error   = 0x01,
            Begin   = 0x02,
            End     = 0x04,
            Always  = Error | Begin | End
        };

        public enum EAction
        { 
            Import,
            ExportFiles,
            ImportFiles,
        }

        public static ToolSettings Instance = null;
        public static string[] Arguments = null;
        public static Dictionary<string, string> VersionInfo = new Dictionary<string, string>();

        public string AppTitle, AppCopyright, AppVersion, AppFileVersion;

        public DateTime StartTs;
        public EAction Action = EAction.Import;
        public bool Continue = true;
        public bool Simulation = false;
        public string ChangeDir = null;
        public EPause Pause = EPause.None;
        public FileInfo SourceFile;
        public string BaseWikiUrl = "http://localhost:8080/w";
        public string WikiUser = null;
        public string WikiPassword = null;
        public List<string> ExcludePages = null;
        public List<string> IncludePages = null;
        public string UrlReservedChars = " !#$&'()*+,/:;=?@[]";

        public ToolSettings()
        {
            Instance = this;            

            this.StartTs = DateTime.Now;

            TraceConfiguration.Register();

            Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            TypeUtils.CollectVersionInfoAttributes(VersionInfo, asm);

            if (!VersionInfo.TryGetValue("title", out this.AppTitle))
                this.AppTitle = "ReestrParser";
            if (!VersionInfo.TryGetValue("copyright", out this.AppCopyright))
                this.AppCopyright = "Copyright (©) Dmitry Bond. 2023";
            if (!VersionInfo.TryGetValue("version", out this.AppVersion))
                this.AppVersion = "2023.09.19.1007";
            if (!VersionInfo.TryGetValue("fileversion", out this.AppFileVersion))
                this.AppFileVersion = "1.0.0.0";

            VersionInfo["processtype"] = asm.GetName().ProcessorArchitecture.ToString();

            loadDefaults();
        }

        protected void dupOut(string s)
        {
            Trace.WriteLine(s);            
            Console.WriteLine(s);
        }

        public void DisplayStartHeader()
        {
            string msg = ToolSettings.HELP_TEXT_TITLE;
            msg = ExplandMacroValues(msg);
            dupOut(msg);

            dupOut(string.Format("* exe[pid={0}]: {1}", Process.GetCurrentProcess().Id, TypeUtils.ApplicationExecutableFullPath));
            dupOut(string.Format("{0}. Version {1}", this.AppTitle, TypeUtils.ApplicationVersionStr));
            dupOut(string.Format("{0}", this.AppCopyright));

            dupOut(string.Format("* args[{0} items]: {1}", ToolSettings.Arguments.Length, StrUtils.Join(ToolSettings.Arguments, "  ")));
            dupOut(string.Format("* dir: {0}", Directory.GetCurrentDirectory()));
            dupOut(string.Format("* HostInfo: {0}\n", CommonUtils.HostInfoStamp()));
        }

        public string UrlEncode(string pText)
        {
            if (pText.IndexOfAny(UrlReservedChars.ToCharArray()) < 0) 
                return pText;

            string result = "";
            foreach (char ch in pText)
            { 
                if (UrlReservedChars.IndexOf(ch) >= 0)
                    result += string.Format("%{0}", ((int)ch).ToString("X2"));
                else
                    result += ch;
            }
            return result;
        }

        public bool ParseCmdLine(string[] args)
        {
            Arguments = args;

            string dir = Directory.GetCurrentDirectory();

            bool result = true;
            this.Continue = true;
            //int byPassArgIdx = -1;
            for (int iArg = 0; iArg < args.Length; iArg++)
            {
                string arg = args[iArg];

                string pn = arg.Trim(StrUtils.CH_SPACES);
                if (string.IsNullOrEmpty(pn)) continue;

                if (pn[0] == '-' || pn[0] == '/')
                {
                    bool isLongOpt = pn.StartsWith("--");
                    if (isLongOpt) pn = pn.Remove(0, 2);
                    else pn = pn.Remove(0, 1);

                    int p;
                    string pv = null;
                    // _12345678
                    // user=alta
                    if ((p = pn.IndexOfAny(":=".ToCharArray())) >= 0)
                    {
                        pv = pn.Remove(0, p + 1).Trim();
                        pn = pn.Substring(0, p).Trim();
                    }

                    if (!parseCliParam(arg, pn, pv))
                        break;
                }
                else
                {
                    setSourceFile(arg);
                }
            }

            return result;
        }

        public string ExplandMacroValues(string pText)
        {
            var x = ConfigurationManager.AppSettings["x"];

            string result = pText;
            result = StrUtils.ReplaceCI(result, "$(AppName)", TypeUtils.ApplicationName);
            result = StrUtils.ReplaceCI(result, "$(AppTitle)", this.AppTitle);
            result = StrUtils.ReplaceCI(result, "$(AppCopyright)", this.AppCopyright);
            result = StrUtils.ReplaceCI(result, "$(FileVersion)", this.AppFileVersion);
            result = StrUtils.ReplaceCI(result, "$(Version)", TypeUtils.ApplicationVersionStr);

            //result = StrUtils.ReplaceCI(result, "$(Version)", TypeUtils.ApplicationVersionStr);
            StrUtils.ExpandParameters(ref result, "$(", ")", getPrmValue, this);

            return result;
        }

        public static void DumpTraceListeners()
        {
            Console.WriteLine(string.Format("--- {0} trace listeners", Trace.Listeners.Count));
            Trace.WriteLine("test!!!");
            int idx = 0;
            foreach (TraceListener lsnr in Trace.Listeners)
            {
                idx++;
                Console.WriteLine(string.Format("  #{0}: {1}", idx, lsnr));
            }
            Console.WriteLine(string.Format(" * TrcLvl: {0}", TrcLvl.Level));
        }

        #region Implementation details

        private bool getPrmValue(string pPrmName, out string pPrmValue, object pContext)
        {
            pPrmValue = null;

            foreach (KeyValuePair<string, string> prm in VersionInfo)
            {
                if (StrUtils.IsSameText(prm.Key, pPrmName))
                {
                    pPrmValue = prm.Value;
                    return true;
                }
            }

            return (pPrmValue != null);
        }

        private void displayHelp()
        {
            string txt = ExplandMacroValues(ToolSettings.HELP_TEXT);
            Trace.WriteLine(txt);
        }

        private void displayEULA()
        {
            string txt = ExplandMacroValues(ToolSettings.HELP_LICENSE_TEXT);
            Trace.WriteLine(txt);
        }

        private bool parseCliParam(string arg, string pn, string pv)
        {
            bool result = true;

            if (StrUtils.IsSameText(pn, "?") || StrUtils.IsSameText(pn, "help"))
            {
                this.Continue = false;
                displayHelp();
                return false;
            }
            else if (StrUtils.IsSameText(pn, "act") || StrUtils.IsSameText(pn, "action"))
            {
                setAction(pv);
            }
            else if (StrUtils.IsSameText(pn, "cd") || StrUtils.IsSameText(pn, "change-dir"))
            {
                if (!Directory.Exists(pv))
                    throw new ToolError(string.Format("Directory [{0}] is not found!", pv));
                this.ChangeDir = pv;
            }
            else if (StrUtils.IsSameText(pn, "epl") || StrUtils.IsSameText(pn, "exclude-pages-list"))
            {
                if (!File.Exists(pv))
                    throw new ToolError(string.Format("File [{0}] is not found!", pv));
                this.ExcludePages = loadFilesList(pv);
            }
            else if (StrUtils.IsSameText(pn, "ipl") || StrUtils.IsSameText(pn, "include-pages-list"))
            {
                if (!File.Exists(pv))
                    throw new ToolError(string.Format("File [{0}] is not found!", pv));
                this.IncludePages = loadFilesList(pv);
            }
            else if (pn == "p" || pn == "pause")
            {
                setPause(pv);
            }
            else if (pn == "simulation")
            {
                this.Simulation = true;
            }

            else if (StrUtils.IsSameText(pn, "wu") || StrUtils.IsSameText(pn, "wiki-user"))
            {
                this.WikiUser = pv;
            }
            else if (StrUtils.IsSameText(pn, "wp") || StrUtils.IsSameText(pn, "wiki-password"))
            {
                this.WikiPassword = pv;
            }
            else if (StrUtils.IsSameText(pn, "url"))
            {
                this.BaseWikiUrl = pv;
            }

            else
            {
                result = false;
                Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format("!CLI.IGNORED: {0}", arg) : "");
            }

            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("* CLI.{0}[{1}]: {2}", 
                (result ? "OK" : "Err/Skip"), pn, pv) : "");

            return result;
        }

        private List<string> loadFilesList(string pFilename)
        {
            List<string> list = new List<string>();            
            using (StreamReader sr = File.OpenText(pFilename))
            {
                while (!sr.EndOfStream)
                {
                    string ln = sr.ReadLine().Trim().ToLower();
                    if (string.IsNullOrEmpty(ln)) continue;
                    list.Add(ln);
                }
            }
            return list;
        }

        private void setAction(string pv)
        {
            if (pv == "") pv = EAction.Import.ToString();
            try
            {
                EAction v = (EAction)Enum.Parse(typeof(EAction), pv, true);
                this.Action = v;
            }
            catch (Exception exc)
            {
                Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format("!CLI.WrongValue(action={0}): {1}", 
                    pv, ErrorUtils.FormatErrorMsg(exc)) : "");
            }
        }

        private void setPause(string pv)
        {
            if (pv == "") pv = "End";
            string[] items = pv.Split(',');
            this.Pause = EPause.None;
            foreach (string it in items)
            {
                EPause pau = (EPause)Enum.Parse(typeof(EPause), it.Trim(), true);
                this.Pause |= pau;
            }
        }

        private void setSourceFile(string arg)
        {
            if (!File.Exists(arg))
                throw new Exception(string.Format("File is not found - {0}", arg));
            this.SourceFile = new FileInfo(arg);
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("* CLI.input: {0} => {1}",
                arg, this.SourceFile.FullName) : "");
        }

        private void loadDefaults()
        {

            // load default CLI params from config
            foreach (string k in ConfigurationManager.AppSettings.AllKeys)
            {
                string s = ConfigurationManager.AppSettings[k];

                if (StrUtils.IsSameText(k, "UrlReservedChars"))
                {
                    UrlReservedChars = s;
                }
                else if (k.ToLower().StartsWith("cli:"))
                {
                    string pn = k.Remove(0, 4).Trim().ToLower();

                    // special case: CLI:input processed separately
                    if (StrUtils.IsSameText(pn, "input"))
                    {
                        setSourceFile(s);
                    }
                    else
                    {
                        string pv = s;
                        string arg = string.Format("cfg[{0}] = {1}", k, pv);
                        parseCliParam(arg, pn, pv);
                    }
                }
            }
        }

        #endregion // Implementation details

        #region Static API

        /// <summary>Load list of files matching specified filemask from specified path.</summary>
        /// <param name="pTargetList">List to add files into</param>
        /// <param name="pPath">path to search files in</param>
        /// <param name="pFilespec">filespec (files mask), could be multiple delimited by "|" (pipe char)</param>
        /// <returns>Number of files added to list</returns>
        public static int LoadFilesList(List<FileInfo> pTargetList, string pPath, string pFilespec)
        {
            int savedCount = pTargetList.Count;
            DirectoryInfo dir = new DirectoryInfo(pPath);
            string[] filespecs = pFilespec.Split('|');
            foreach (string fs in filespecs)
            {
                FileInfo[] files = dir.GetFiles(fs);
                if (files.Length > 0)
                    pTargetList.Capacity += files.Length;
                foreach (FileInfo item in files)
                {
                    pTargetList.Add(item);
                }
            }
            return pTargetList.Count - savedCount;
        }

        /// <summary>Load list of files matching specified filemask from specified path.</summary>
        /// <param name="pTargetList">List to add files into</param>
        /// <param name="pPath">path to search files in</param>
        /// <param name="pFilespec">filespec (files mask), could be multiple delimited by "|" (pipe char)</param>
        /// <param name="pRecursive">Scan recursively subdirectories</param>
        /// <returns>Number of files added to list</returns>
        public static int LoadFilesList(List<FileInfo> pTargetList, string pPath, string pFilespec, bool pRecursive)
        {
            int cnt = LoadFilesList(pTargetList, pPath, pFilespec);
            if (!pRecursive) return cnt;

            DirectoryInfo dir = new DirectoryInfo(pPath);
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo di in dirs)
            {
                cnt += LoadFilesList(pTargetList, di.FullName, pFilespec, pRecursive);
            }
            return cnt;
        }

        #endregion // Static API

    }
}
