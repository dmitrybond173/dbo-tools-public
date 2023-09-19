/* 
 * Reestr Parser app.
 *
 * Holder and handler of app settings/parameters.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Aug, 2023
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
using System.Security.RightsManagement;
using Microsoft.SqlServer.Server;
using System.Web;
#if _UseDB
using System.Data.SQLite;
#endif
using XService.Utils;

namespace ReestrParser
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
            "Convertor of private24 reester-files into db format. Version $(version)\r\n" +
            "";

        public const string HELP_TEXT_USAGE =
            "Usage: $(AppName) filespecs [...filespecs] [options]\r\n" +
            "";

        public const string HELP_TEXT =
            "\r\n" +
            HELP_TEXT_USAGE +
            //123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_
            "\r\n" +
            "Supported options:\r\n" +
            "  -?  - print this message\r\n" +
            "  -cd, --change-dir={directory}  - switch directory when started\n" +
            "  -dbc, --db-cleanup[=1|0]  - perform db cleanup before inserting data; default is 1\n" +
            "  -o, --output={format}  - add output format, one of: db, excel (default output is \'db\')\n" +
            "        you can specify {format} with \'-\' prefix to exclude it\n" +
            "  -p, --pause={list}  - set pause params, comma-separated list of: error, begin, end, always\n" +
            "  -r, --recursive[=1|0]  - recursive scan; default is 1\r\n" +
            "  -ui=[1|0]   - run in UI-mode; default is 1\r\n" +
            "\r\n" +
            "Examples:\r\n" +
            "  0. Run normally (UI mode)\r\n" +
            "    $(AppName)\r\n" +
            "\r\n" +
            "  1. Run from command line\r\n" +
            "    $(AppName) c:\\bank\\private\\data\\*.txt\r\n" +
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

        public static ToolSettings Instance = null;
        public static string[] Arguments = null;
        public static Dictionary<string, string> VersionInfo = new Dictionary<string, string>();

        public string AppTitle, AppCopyright, AppVersion, AppFileVersion;

        public DateTime StartTs;
        public bool Continue = true;
        public string ChangeDir = null;
        public bool Recursive = true;
        public bool UiMode = true;
        public bool CleanupDb = true;
        public EPause Pause = EPause.None;
        public List<FileSource> SrcFiles = new List<FileSource>();
        public List<string> Outputs = new List<string>();

        public ToolSettings()
        {
            Instance = this;            

            this.StartTs = DateTime.Now;

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
                    string path = Path.GetDirectoryName(arg);
                    if (string.IsNullOrEmpty(path))
                        path = dir;
                    string fs = Path.GetFileName(arg);
                    this.SrcFiles.Add(new FileSource(this, path, fs));
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
            else if (StrUtils.IsSameText(pn, "cd") || StrUtils.IsSameText(pn, "change-dir"))
            {
                if (!Directory.Exists(pv))
                    throw new ToolError(string.Format("Directory [{0}] is not found!", pv));
                this.ChangeDir = pv;
            }
            else if (StrUtils.IsSameText(pn, "dbc") || StrUtils.IsSameText(pn, "db-cleanup"))
            {
                if (string.IsNullOrEmpty(pv)) pv = "1";
                this.CleanupDb = StrUtils.GetAsBool(pv);
            }
            else if (pn == "o" || pn == "output")
            {
                setOutput(pv);
            }
            else if (pn == "p" || pn == "pause")
            {
                setPause(pv);
            }
            else if (StrUtils.IsSameText(pn, "r") || StrUtils.IsSameText(pn, "recursive"))
            {
                if (string.IsNullOrEmpty(pv)) pv = "1";
                this.Recursive = StrUtils.GetAsBool(pv);
            }
            else if (StrUtils.IsSameText(pn, "ui"))
            {
                if (string.IsNullOrEmpty(pv)) pv = "1";
                this.UiMode = StrUtils.GetAsBool(pv);
            }
            else
                Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format("!CLI.IGNORED: {0}", arg) : "");

            return result;
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

        private void setOutput(string pv)
        {
            string[] items = pv.Split(',');
            foreach (string it in items)
            {
                string item = it.ToLower().Trim();
                if (string.IsNullOrEmpty(item)) continue;
                bool isNeg = item.StartsWith("-");
                if (isNeg) item = item.Remove(0, 1);
                if (isNeg)
                {
                    if (this.Outputs.Contains(item))
                        this.Outputs.Remove(item);
                }
                else
                    this.Outputs.Add(item);
            }
        }

        private void loadDefaults()
        {
            this.Outputs.Add("db");

            // load default CLI params from config
            foreach (string k in ConfigurationManager.AppSettings.AllKeys)
            {
                if (!k.ToLower().StartsWith("cli:")) continue;

                string pn = k.Remove(0, 4).Trim().ToLower();
                string s = ConfigurationManager.AppSettings[k];

                // special case: CLI:input processed separately
                if (StrUtils.IsSameText(pn, "input"))
                {
                    string path = Path.GetDirectoryName(s);
                    string fs = Path.GetFileName(s);
                    this.SrcFiles.Add(new FileSource(this, path, fs));
                }
                else
                {
                    string pv = s;
                    string arg = string.Format("cfg[{0}] = {1}", k, pv);
                    parseCliParam(arg, pn, pv);
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

        /// <summary>Source of files (dir + list of filemasks)</summary>
        public class FileSource
        {
            public FileSource(ToolSettings pOwner, string pPath, string pFilespecs)
            {
                this.Owner = pOwner;
                this.Filespecs = pFilespecs;
                this.Path = pPath;
                if (string.IsNullOrEmpty(this.Path))
                    this.Path = Directory.GetCurrentDirectory();
            }

            public override string ToString()
            {
                return String.Format("FileSrc[{0}; {1}]", this.Path, this.Filespecs);
            }

            public ToolSettings Owner { get; protected set; }
            public string Path;
            public string Filespecs = DEFAULT_Filespecs;
            public List<FileInfo> Files = new List<FileInfo>();
            public long TotalFilesSize = 0;

            public int ResolveFiles()
            {
                Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("? resolving [{0}] in [{1}]...", this.Filespecs, this.Path) : "");

                string fs = this.Filespecs;
                this.TotalFilesSize = 0;
                int cnt = LoadFilesList(this.Files, this.Path, fs, this.Owner.Recursive);
                foreach (FileInfo fi in this.Files)
                    this.TotalFilesSize += fi.Length;
                
                Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("  = {0} files of {1} bytes found.", cnt, this.TotalFilesSize) : "");

                return cnt;
            }
        }

    }
}
