/*
 * CONFIG file components: ExternalCommands.
 * ExternalCommands is a class which is loaded from <ExternalCommands> App.Config file XML section.
 * It can execute defined commands.
 * Written by Dmitry Bond. at Dec 2011.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using XService.Utils;

namespace XService.Configuration
{
    /// <summary>
    /// ExternalCommands
    /// Class to load configuration of external tools configured for this application.
    /// Please ensure your application config file has appropriate configuration section defined:
    ///   <configSections>
    ///     <section name="ExternalCommands" type="XService.Configuration.ExternalCommands,XService.Net2" />
    ///   </configSections>
    /// Example of command configuration:
    ///   <Command name="MyCmdName1" 
    ///           type="Program|ShellScript" 
    ///           program="%comSpec% | script="%BtoOfflinePollerHome%\scripts\doReBind.cmd"
    ///           arguments="$(ScriptFilename)" 
    ///           outputFile="%BtoOfflinePollerHome%\logs\shell.log" 
    ///           environment="BTOHOME=$(BtoHome);MYVAR1=123"
    ///           syncEvents="-EvtToReset;+EvtToSet;?EvtToWaitBeforeStartIt"
    ///           options="hidden|..."
    ///           />
    /// </summary>
    public class ExternalCommands : IConfigurationSectionHandler
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("ExternalCommands", "ExternalCommands");

        // interface: IConfigurationSectionHandler
        public object Create(object parent, object configContext, XmlNode section)
        {
            ExternalCommands commands = new ExternalCommands();
            commands.Load(section);
            return commands;
        }

        public List<ExternalCommand> Commands = new List<ExternalCommand>();
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();

        public void EnsureCommandsDefined(string[] pCommandNames)
        {
            EnsureCommandsDefined(pCommandNames, true);
        }

        public bool EnsureCommandsDefined(string[] pCommandNames, bool pThrowError)
        {
            bool isOk = true;
            string missingList = "";
            foreach (string cmdName in pCommandNames)
            {
                isOk = isOk && (this.IndexOf(cmdName) >= 0);
                if (!isOk)
                {
                    if (!string.IsNullOrEmpty(missingList)) missingList += ",";
                    missingList += cmdName;
                }
            }
            if (!isOk && pThrowError)
                throw new ToolConfigError(string.Format("Following commands missing in configuration ({0})!", missingList));
            return isOk;
        }

        public ExternalCommand FindCommand(string pCommandName)
        {
            for (int i = 0; i < this.Commands.Count; i++)
            {
                if (string.Compare(pCommandName, this.Commands[i].Name, true) == 0)
                    return this.Commands[i];
            }
            return null;
        }

        public int IndexOf(string pCommandName)
        {
            for (int i = 0; i < this.Commands.Count; i++)
            {
                if (string.Compare(pCommandName, this.Commands[i].Name, true) == 0)
                    return i;
            }
            return -1;
        }

        private bool getPrmValue(string pPrmName, out string pPrmValue, object pContext)
        {
            pPrmValue = null;
            Dictionary<string, string> prms = (Dictionary<string, string>)pContext;
            if (prms.TryGetValue(pPrmName, out pPrmValue)) return true;

            foreach (KeyValuePair<string, string> prm in prms)
            {
                if (StrUtils.IsSameText(prm.Key, pPrmName))
                {
                    pPrmValue = prm.Value;
                    return true;
                }
            }
            return false;
        }

        public string ExpandParameters(string pStr, Dictionary<string, string> pParameters, bool pExpandEnvironmentVars)
        {
            StrUtils.ExpandParameters(ref pStr, "$(", ")", getPrmValue, pParameters);

            /*            
            foreach (KeyValuePair<string, string> prm in pParameters)
            {
                string id = "$(" + prm.Key.ToUpper() + ")";
                int p = pStr.ToUpper().IndexOf(id);
                if (p >= 0)
                {
                    pStr = pStr.Remove(p, id.Length);
                    pStr = pStr.Insert(p, prm.Value);
                }
            }
            */

            if (pExpandEnvironmentVars)
                pStr = Environment.ExpandEnvironmentVariables(pStr);
            return pStr;
        }

        /// <summary>Execute command</summary>
        /// <param name="pCommandName">Name of command to execute</param>
        /// <param name="pParamsDefs">String which contains additional run-time parameters command execution (key1=value1;...)</param>
        /// <param name="pExtraParameters">Extra run-time parameters</param>
        /// <returns></returns>
        public int Execute(string pCommandName, string pParamsDefs, Dictionary<string, string> pExtraParameters)
        {
            string scp = null;
            int result = performExecute(pCommandName, pParamsDefs, pExtraParameters, ref scp);
            return result;
        }

        public string GenerateScript(string pCommandName, string pParamsDefs, Dictionary<string, string> pExtraParameters)
        {
            string scp = "";
            int result = performExecute(pCommandName, pParamsDefs, pExtraParameters, ref scp);
            if (result >= 0)
                return scp;
            return null;
        }

        protected int performExecute(string pCommandName, string pParamsDefs, Dictionary<string, string> pExtraParameters, ref string pCmdScript)
        {
            if (pParamsDefs == null)
                pParamsDefs = string.Empty;

            Trace.WriteLineIf(TrcLvl.TraceWarning, "");
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                " - ExecuteCommand[ {0} ]( {1} )...", pCommandName, pParamsDefs) : "");

            bool isScriptOnly = (pCmdScript != null);
            this.envValues.Clear();

            int idx = this.IndexOf(pCommandName);
            if (idx < 0)
                throw new ToolError(string.Format("Command ({0}) is not found!", pCommandName));

            int exitCode = 0;
            string fn;
            string savedTitle = null;
            try { savedTitle = Console.Title; }
            catch { }
            ExternalCommand cmd = this.Commands[idx];

            // container for actual runtime parameters
            Dictionary<string, string> runtimeParameters = new Dictionary<string, string>();
            try
            {
                if (cmd.IsRunningNow)
                {
                    Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                        " - Command[{0}] is running now! Run-level is {1} ", pCommandName, cmd.RunLevel) : "");
                    if (cmd.IsSingleInstance)
                    {
                        Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                            " - Command[{0}] is not allowed to start seconds instance!", pCommandName) : "");
                        return -1;
                    }
                }

                // merge global parameters into actual runtime parameters
                StrUtils.Merge(runtimeParameters, cmd.ExtraProperties, false);

                // merge global parameters into actual runtime parameters
                StrUtils.Merge(runtimeParameters, this.Parameters, false);

                cmd.RunLevel++;

                // copy command parameters (program name, arguments, environment)
                string prog = "", args = "", extraEnv = "";
                switch (cmd.Type)
                {
                    case ExternalCommand.ECommandType.Program:
                        prog = Environment.ExpandEnvironmentVariables(cmd.Program);
                        args = cmd.Arguments;
                        runtimeParameters["ExeHomeDirectory"] = Path.GetDirectoryName(cmd.Program);
                        runtimeParameters["CommandHomeDirectory"] = runtimeParameters["ExeHomeDirectory"];
                        fn = ExpandParameters(prog, runtimeParameters, true);
                        if (!File.Exists(fn))
                            throw new ToolError(string.Format("Program ({0}) is not found!", fn));
                        break;

                    case ExternalCommand.ECommandType.ShellScript:
                        int iShell = this.IndexOf("Shell");
                        if (iShell >= 0)
                        {
                            ExternalCommand shellCmd = this.Commands[iShell];
                            prog = shellCmd.Program;
                            args = shellCmd.Arguments;
                            extraEnv = shellCmd.ExtraEnvironment;
                        }
                        else
                        {
                            prog = Environment.ExpandEnvironmentVariables("%ComSpec%");
                            args = "/c \"$(ScriptFilename)\"";
                        }
                        runtimeParameters["ScriptHomeDirectory"] = Path.GetDirectoryName(cmd.Script);
                        runtimeParameters["CommandHomeDirectory"] = runtimeParameters["ScriptHomeDirectory"];
                        fn = ExpandParameters(cmd.Script, runtimeParameters, true);
                        if (!File.Exists(fn))
                            throw new ToolError(string.Format("Script ({0}) is not found!", fn));
                        break;
                }
                if (!string.IsNullOrEmpty(cmd.ExtraEnvironment))
                    extraEnv += cmd.ExtraEnvironment;

                parseParamsDefs(runtimeParameters, pParamsDefs, pExtraParameters);

                if (cmd.Type == ExternalCommand.ECommandType.ShellScript)
                {
                    runtimeParameters["ScriptFilename"] = cmd.Script;
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                        args += (" " + cmd.Arguments);
                }

                if (!string.IsNullOrEmpty(extraEnv))
                {
                    this.envValues.Clear();
                    extraEnv = ExpandParameters(extraEnv, runtimeParameters, true);
                    ensureToSetEnvironment(extraEnv);
                }
                if (cmd.HasOption(ExternalCommand.EOptions.ParamsAsEnvironment))
                {
                    ensureToSetEnvironment(runtimeParameters);
                }
                if (isScriptOnly && this.envValues.Count > 0)
                {
                    pCmdScript += string.Format(
                        "{0}set msg=--- %DATE%,%TIME% --- run[{1}]" +
                        "{0}echo." +
                        "{0}echo %msg%" +
                        "", 
                        Environment.NewLine, cmd.Name);
                    foreach (KeyValuePair<string, string> it in this.envValues)
                    {
                        pCmdScript += string.Format("{0}set {1}={2}", Environment.NewLine, it.Key, it.Value);
                    }
                }

                prog = ExpandParameters(prog, runtimeParameters, true);
                args = ExpandParameters(args, runtimeParameters, true);

                cmd.ProcessSyncEvents(true);

                Process proc = new Process();
                ProcessStartInfo startInfo = proc.StartInfo; // new ProcessStartInfo(prog, args);
                proc.StartInfo.FileName = prog;
                proc.StartInfo.Arguments = args;
                string dir = cmd.WorkDirectory;
                if (runtimeParameters.ContainsKey("WorkingDirectory"))
                    dir = runtimeParameters["WorkingDirectory"];
                if (!string.IsNullOrEmpty(dir))
                {
                    if (dir.StartsWith("~"))
                        dir = dir.Replace("~", runtimeParameters["CommandHomeDirectory"]);
                    dir = ExpandParameters(dir, runtimeParameters, true);
                    startInfo.WorkingDirectory = dir;
                    if (isScriptOnly)
                        pCmdScript += string.Format("{0}cd /d \"{1}\"", Environment.NewLine, dir);
                }
                string cmdRedirect = null;
                startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                startInfo.UseShellExecute = cmd.NoWaitedRun;
                cmd.ActualOutputFile = null;
                string outputFn = cmd.OutputFile;
                try
                {
                    startInfo.CreateNoWindow = cmd.IsHidden;

                    if (!string.IsNullOrEmpty(outputFn))
                    {
                        outputFn = ExpandParameters(outputFn, runtimeParameters, true);
                        cmd.ActualOutputFile = outputFn;
                        if (cmd.IsCleanOutput && File.Exists(outputFn))
                        {
                            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                                "   = exec[ {0} ] -> cleanup output: {1}", pCommandName, outputFn) : "");
                            try { File.Delete(outputFn); }
                            catch { }
                        }

                        if (isScriptOnly)
                        {
                            pCmdScript += string.Format(
                                "{0}echo. >> \"{1}\"" +
                                "{0}echo %msg% >> \"{1}\"" + 
                                "{0}title %msg%" +
                                "", 
                                Environment.NewLine, outputFn);
                            cmdRedirect = string.Format(" 2>&1 >> \"{0}\"", outputFn);
                        }
                        else
                        {
                            startInfo.RedirectStandardError = true;
                            startInfo.RedirectStandardOutput = true;

                            this.swStdOut = File.AppendText(outputFn);
                            this.swStdOut.WriteLine(string.Format("\n\n--- Std.Out&Err ({0}) ---", DateTime.Now.ToString("yyyyMMdd-HHmmss.fff")));
                            this.swStdOut.WriteLine(string.Format("[{0}] [{1}] @ dir=[{2}]",
                                (proc.StartInfo.FileName != null ? proc.StartInfo.FileName : "(null)"),
                                (proc.StartInfo.Arguments != null ? proc.StartInfo.Arguments : "(null)"),
                                (proc.StartInfo.WorkingDirectory != null ? proc.StartInfo.WorkingDirectory : "(null)")
                                ));
                        }
                    }
                    if (!isScriptOnly)
                    {
                        proc.ErrorDataReceived += new DataReceivedEventHandler(proc_StdErrDataReceived);
                        proc.OutputDataReceived += new DataReceivedEventHandler(proc_StdOutDataReceived);
                    }

                    Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                        "   = exec[ {0} ] -> WorkDir:\"{1}\"; WinStyle:{2}; ShellExec:{3} ", 
                        pCommandName, startInfo.WorkingDirectory, startInfo.WindowStyle, startInfo.UseShellExecute) : "");

                    Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                        "   = exec[ {0} ] -> StartingProcess: \"{1}\" \"{2}\" ...", pCommandName, prog, args) : "");

                    if (!string.IsNullOrEmpty(outputFn))
                        Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                            "   = exec[ {0} ] -> Output redirected to: \"{1}\"", pCommandName, outputFn) : "");
                    if (cmd.HasOption(ExternalCommand.EOptions.DumpEnv))
                        Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                            "   = exec[ {0} ] -> Environment: \"{1}\"", pCommandName, environmentAsDump()) : "");

                    if (isScriptOnly)
                    {
                        pCmdScript += string.Format("{0}\"{1}\" {2} {3}", Environment.NewLine, proc.StartInfo.FileName, proc.StartInfo.Arguments,
                            (cmdRedirect != null ? cmdRedirect : ""));
                    }
                    else
                    {
                        proc.Start();
                        if (!string.IsNullOrEmpty(outputFn))
                        {
                            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                                "     - exec[ {0} ] -> initializing process Std I/O...", pCommandName) : "");
                            proc.BeginErrorReadLine();
                            proc.BeginOutputReadLine();
                        }

                        Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                            "     - exec[ {0} ] -> waiting for process ({1})...", pCommandName, proc.Id) : "");
                        proc.WaitForExit();

                        if (this.swStdOut != null)
                        {
                            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                                "     - exec[ {0} ] -> closing process Std I/O...", pCommandName) : "");
                            this.swStdOut.Close();
                        }
                    }
                }
                finally { this.swStdOut = null; }

                if (!isScriptOnly)
                {
                    exitCode = proc.ExitCode;
                    cmd.ProcessSyncEvents(false);
                }

                Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                    "     = exec[ {0} ] -> ExitCode: {1}", pCommandName, exitCode) : "");
            }
            finally
            {
                cmd.RunLevel--;

                // just to ensure to return app title
                if (savedTitle != null)
                    try { Console.Title = savedTitle; }
                    catch { }                    
            }

            return exitCode;
        }

        #region Implementation Details

        internal void Load(XmlNode section)
        {
            foreach (XmlNode node in section.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (StrUtils.IsSameText(node.Name, "Parameters"))
                {
                    foreach (XmlNode subNode in node.ChildNodes)
                    {
                        if (subNode.NodeType != XmlNodeType.Element) continue;
                        if (StrUtils.IsSameText(subNode.Name, "add") || StrUtils.IsSameText(subNode.Name, "Parameter"))
                        {
                            XmlElement element = (XmlElement)subNode;
                            string name, value = null;
                            XmlNode attr = element.GetAttributeNode("key");
                            if (attr == null) attr = element.GetAttributeNode("name");
                            if (attr == null) continue;
                            name = attr.Value;
                            attr = element.GetAttributeNode("value");
                            if (attr != null) value = attr.Value;
                            this.Parameters[name] = value;
                        }
                    }
                }
                else if (StrUtils.IsSameText(node.Name, "Command"))
                {
                    ExternalCommand inbox = ExternalCommand.Load(this, node as XmlElement);
                    if (inbox != null) this.Commands.Add(inbox);
                }
            }
        }

        protected void proc_StdErrDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (this.swStdOut != null && e.Data != null)
            {
                this.swStdOut.WriteLine("StdErr: " + e.Data);
            }
        }

        protected void proc_StdOutDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (this.swStdOut != null && e.Data != null)
            {
                this.swStdOut.WriteLine(e.Data);
            }
        }

        private string environmentAsDump()
        {
            IDictionary envVars = Environment.GetEnvironmentVariables();
            string txt = string.Format("Environment_Block( {0} items ):", envVars.Count);
            List<string> items = new List<string>();
            foreach (DictionaryEntry env in envVars)
            {
                items.Add(string.Format("{0}={1}", env.Key.ToString(), env.Value.ToString()));
            }
            items.Sort();
            foreach (string item in items)
            {
                txt += string.Format("{0}\t{1}", Environment.NewLine, item);
            }
            return txt;
        }

        private void ensureToSetEnvironment(string pExtraEnvDefs)
        {
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                "   = SetEnvironment: {0}", pExtraEnvDefs) : "");
            Dictionary<string, string> prms = CollectionUtils.ParseParametersStrEx(pExtraEnvDefs, false);
            ensureToSetEnvironment(prms);
        }

        private void ensureToSetEnvironment(Dictionary<string, string> pValues)
        {
            foreach (KeyValuePair<string, string> prm in pValues)
            {
                Environment.SetEnvironmentVariable(prm.Key, prm.Value);
                this.envValues[prm.Key] = prm.Value;
            }
        }

        private void parseParamsDefs(Dictionary<string, string> pTargetParams, string pParamsDefs, Dictionary<string, string> pExtraParameters)
        {
            if (pExtraParameters != null)
            {
                foreach (KeyValuePair<string, string> prm in pExtraParameters)
                {
                    pTargetParams[prm.Key] = prm.Value;
                }
            }

            Dictionary<string, string> prms = CollectionUtils.ParseParametersStrEx(pParamsDefs, false);
            foreach (KeyValuePair<string, string> prm in prms)
            {
                pTargetParams[prm.Key] = prm.Value;
            }
        }

        private StreamWriter swStdOut = null;
        private Dictionary<string, string> envValues = new Dictionary<string, string>();

        #endregion // Implementation Details
    }


    /// <summary>
    /// ExternalCommand - object to hold configuration infromation about prepconfigured external command
    /// </summary>
    public class ExternalCommand
    {
        public enum ECommandType
        {
            Program,
            ShellScript,
        }

        [Flags]
        public enum EOptions
        {
            None                = 0,
            Enabled             = 0x0001,
            Hidden              = 0x0002,
            NoWaitedRun         = 0x0004,
            SingleInstance      = 0x0008,
            ParamsAsEnvironment = 0x0010,
            CleanOutput         = 0x0020,
            DumpEnv = 0x1000,
        }

        public override string ToString()
        {
            return String.Format("Cmd[{0}/{1}]: file={2}, args={3}", 
                this.Name, this.Type, (this.Type == ECommandType.Program ? this.Program : this.Script), this.Arguments);
        }

        public string Name;
        public string Description;
        public ECommandType Type;
        public string Program;
        public string Script;
        public string Arguments;
        public string WorkDirectory = "";
        public string SyncEventsList;
        public string OutputFile;
        public string ActualOutputFile;
        public EOptions Options = EOptions.None;
        public int RunLevel = 0;
        public List<string> Tags = new List<string>();
        public Dictionary<string, string> ExtraProperties = new Dictionary<string, string>();
        public Dictionary<string, string> CustomEnvironment = new Dictionary<string, string>();

        public string ExtraEnvironment
        {
            get { return composeEnvironment(); }
            set { parseEnvironment(value); }
        }

        public static ExternalCommand Load(ExternalCommands pOwner, XmlElement pNode)
        {
            ExternalCommand cmd = new ExternalCommand();
            cmd.owner = pOwner; 
            cmd.Name = pNode.GetAttribute("name");

            XmlNode attr = pNode.Attributes.GetNamedItem("type");
            if (attr != null)
                cmd.Type = (ECommandType)Enum.Parse(typeof(ECommandType), attr.Value, true);

            switch (cmd.Type)
            {
                case ECommandType.Program:
                    cmd.Program = pNode.GetAttribute("program");
                    attr = pNode.Attributes.GetNamedItem("arguments");
                    if (attr != null) cmd.Arguments = attr.Value;
                    break;

                case ECommandType.ShellScript:
                    cmd.Script = pNode.GetAttribute("script");
                    attr = pNode.Attributes.GetNamedItem("arguments");
                    if (attr != null) cmd.Arguments = attr.Value;
                    break;
            }

            attr = pNode.Attributes.GetNamedItem("directory");
            if (attr != null) cmd.WorkDirectory = attr.Value;

            attr = pNode.Attributes.GetNamedItem("environment");
            if (attr != null) cmd.ExtraEnvironment = attr.Value;

            attr = pNode.Attributes.GetNamedItem("syncEvents");
            if (attr != null) cmd.SyncEventsList = attr.Value;

            attr = pNode.Attributes.GetNamedItem("outputFile");
            if (attr != null) cmd.OutputFile = attr.Value;

            attr = pNode.Attributes.GetNamedItem("description");
            if (attr != null) cmd.Description = attr.Value;

            attr = pNode.Attributes.GetNamedItem("options");
            if (attr != null)
            {
                string s = attr.Value.Trim(StrUtils.STR_SPACES.ToCharArray());
                s = s.Replace(",", ";");
                string[] items = s.Split(';');
                foreach (string item in items)
                {
                    s = item.Trim(StrUtils.STR_SPACES.ToCharArray());
                    if (StrUtils.IsSameText(s, "hidden")) cmd.Options |= EOptions.Hidden;
                    if (StrUtils.IsSameText(s, "nowaited") || StrUtils.IsSameText(s, "shellexec")) cmd.Options |= EOptions.NoWaitedRun;
                    if (StrUtils.IsSameText(s, "single") || StrUtils.IsSameText(s, "singleinstance")) cmd.Options |= EOptions.SingleInstance;
                    if (StrUtils.IsSameText(s, "ParamsAsEnv") || StrUtils.IsSameText(s, "ParamsAsEnvironment")) cmd.Options |= EOptions.ParamsAsEnvironment;
                    if (StrUtils.IsSameText(s, "dumpenv") || StrUtils.IsSameText(s, "dumpenvironment")) cmd.Options |= EOptions.DumpEnv;
                    if (StrUtils.IsSameText(s, "cleanup") || StrUtils.IsSameText(s, "cleanoutput")) cmd.Options |= EOptions.CleanOutput;
                }
            }

            attr = pNode.Attributes.GetNamedItem("tags");
            if (attr != null)
            {
                string s = attr.Value.ToLower().Trim(StrUtils.STR_SPACES.ToCharArray());
                s = s.Replace(",", ";");
                string[] items = s.Split(';');
                foreach (string item in items)
                {
                    cmd.Tags.Add(item.Trim(StrUtils.STR_SPACES.ToCharArray()));
                }
            }

            foreach (XmlNode node in pNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (StrUtils.IsSameText(node.Name, "add") || StrUtils.IsSameText(node.Name, "property"))
                { 
                    XmlElement element = (XmlElement)node;
                    string name, value = null;
                    attr = element.GetAttributeNode("key");
                    if (attr == null) attr = element.GetAttributeNode("name");
                    if (attr == null) continue;
                    name = attr.Value;
                    attr = element.GetAttributeNode("value");
                    if (attr != null) value = attr.Value;
                    cmd.ExtraProperties[name] = value;
                }
            }

            return cmd;
        }

        public bool IsHidden 
        {
            get { return getOption(EOptions.Hidden); }
            set { setOption(EOptions.Hidden, value); } 
        }

        public bool IsCleanOutput
        {
            get { return getOption(EOptions.CleanOutput); }
            set { setOption(EOptions.CleanOutput, value); }
        }

        public bool NoWaitedRun 
        {
            get { return getOption(EOptions.NoWaitedRun); }
            set { setOption(EOptions.NoWaitedRun, value); }
        }
        
        public bool IsSingleInstance
        {
            get { return getOption(EOptions.SingleInstance); }
            set { setOption(EOptions.SingleInstance, value); }
        }

        public bool Enabled
        {
            get { return getOption(EOptions.Enabled); }
            set { setOption(EOptions.Enabled, value); }
        }

        public bool HasOption(EOptions pOption)
        {
            return getOption(pOption);
        }

        public bool IsRunningNow 
        {
            get { return (RunLevel > 0); } 
        }

        public int Run(string pParamsDefs, Dictionary<string, string> pExtraParameters)
        { 
            return this.owner.Execute(this.Name, pParamsDefs, pExtraParameters);
        }

        public string GenerateScript(string pParamsDefs, Dictionary<string, string> pExtraParameters)
        {
            return this.owner.GenerateScript(this.Name, pParamsDefs, pExtraParameters);
        }

        public List<SyncEventDescriptor> SyncEvents 
        { 
            get 
            {
                lock (this)
                {
                    if (this.syncEvents == null)
                    {
                        this.syncEvents = new List<SyncEventDescriptor>();
                        parseSyncEventsDefinitions();
                    }
                    return this.syncEvents;
                }
            } 
        }

        public bool HasAnyWaitEvents(SyncEventDescriptor.EEventUseOption pWaitOptions)
        {
            foreach (SyncEventDescriptor evt in this.syncEvents)
            {
                if ((evt.WaitOption & pWaitOptions) != SyncEventDescriptor.EEventUseOption.None)
                    return true;
            }
            return false;
        }

        public void ProcessSyncEvents(bool pIsBeforeRun)
        {
            SyncEventDescriptor.EEventUseOption useOptionToCheck = (pIsBeforeRun 
                ? SyncEventDescriptor.EEventUseOption.Before 
                : SyncEventDescriptor.EEventUseOption.After);

            string evtList = "";
            foreach (SyncEventDescriptor evtRec in this.SyncEvents)
            {
                bool isMatchWait = ((evtRec.WaitOption & useOptionToCheck) != 0);
                if (isMatchWait)
                {
                    if (evtList != "") evtList += ",";
                    evtList += evtRec.Name;
                }
                bool isMatchSet = ((evtRec.AutoSetOption & useOptionToCheck) != 0);
                if (isMatchSet)
                {
                    Trace.WriteLineIf(ExternalCommands.TrcLvl.TraceWarning, ExternalCommands.TrcLvl.TraceWarning ? string.Format(
                        "     ! exec[ {0} ] -> Auto-Set SyncEvent[ {1} ] on {2}...", this.Name, evtRec.Name, (pIsBeforeRun ? "CmdStart" : "CmdFinish")) : "");
                    evtRec.Event.Set();
                }
                bool isMatchReset = ((evtRec.AutoResetOption & useOptionToCheck) != 0);
                if (isMatchReset)
                {
                    Trace.WriteLineIf(ExternalCommands.TrcLvl.TraceWarning, ExternalCommands.TrcLvl.TraceWarning ? string.Format(
                        "     ! exec[ {0} ] -> Auto-Reset SyncEvent[ {1} ] on {2}...", this.Name, evtRec.Name, (pIsBeforeRun ? "CmdStart" : "CmdFinish")) : "");
                    evtRec.Event.Reset();
                }
            }

            if (!string.IsNullOrEmpty(evtList))
            {
                Trace.WriteLineIf(ExternalCommands.TrcLvl.TraceWarning, ExternalCommands.TrcLvl.TraceWarning ? string.Format(
                    "     ! exec[ {0}] -> Waiting for syncEvents ({1})...", this.Name, evtList) : "");
                bool resultState;
                do
                {
                    resultState = true;
                    foreach (SyncEventDescriptor evtRec in this.SyncEvents)
                    {
                        bool isMatchWait = ((evtRec.WaitOption & useOptionToCheck) != 0);
                        if (!isMatchWait) continue;

                        bool evtState = false;
                        try
                        {
                            evtState = evtRec.Event.WaitOne(100);
                        }
                        catch (Exception exc)
                        {
                            Trace.WriteLineIf(ExternalCommands.TrcLvl.TraceError, ExternalCommands.TrcLvl.TraceError ? string.Format(
                                "     ! exec[ {0} ] -> SyncEvt[{1}].ERR({2}): {3}", this.Name, evtRec.Name, exc.GetType(), exc.Message) : "");
                            evtState = true; // assume event is 'Signaled' in case of error
                        }
                        resultState = resultState & evtState;
                    }
                }
                while (!resultState);
            }
        }

        #region Implementation Details

        /// <summary>
        /// parseSyncEventsDefinitions
        /// Create synchronization events according to a list.
        /// List items delimited only by ';' (semicolon).
        /// Every item in a list has following format:
        ///     [prefix+]{EventName}[ ( {directivesList} ) ]
        /// Where {prefix} is a set of following chars:
        ///     '@' used to use default event state
        ///     '+' used to set event before run a command
        ///     '-' used to reset event before run a command
        ///     '?' used to add event to a wait-list, so it will wait for it before starting a program 
        ///     '$' used to add event to a wait-list, so it will wait for it after program's start completed
        /// Note: is not recommeneded to use use {prefix}, it is deprecated feature.
        /// The directivesList is a pipe-separated list of event processing directives, which are:
        ///     "autosetBefore" - set event automatically before program start
        ///     "autoset" or "autosetAfter" - set event automatically after program finished
        ///     "default" - to create event object but not to change its state directly
        ///     "set" or "reset" - to set or reset event
        ///     "noWait", "waitBefore", "waitAfter" or "waitBoth" - to define event wait option
        /// New format:
        ///     {EventName}[:default|set|reset|waitBefore|waitAfter|waitBoth]
        /// </summary>
        /// <param name="pSyncEventsDefs"></param>
        protected void parseSyncEventsDefinitions()
        {
            if (string.IsNullOrEmpty(this.SyncEventsList))
                return;

            string pSyncEventsDefs = this.SyncEventsList;
            string[] events = pSyncEventsDefs.Split(';');
            List<string> createdEvents = new List<string>();
            foreach (string evt in events)
            {
                string evtName = evt.Trim(StrUtils.CH_SPACES);
                SyncEventDescriptor.EEventState evtState = SyncEventDescriptor.EEventState.Default;
                SyncEventDescriptor.EEventUseOption evtWaitOption = SyncEventDescriptor.EEventUseOption.None;
                SyncEventDescriptor.EEventUseOption evtAutoSetOption = SyncEventDescriptor.EEventUseOption.None;
                SyncEventDescriptor.EEventUseOption evtAutoResetOption = SyncEventDescriptor.EEventUseOption.None;

                // check if eventId has any special prefix chars
                while (evtName != "" && evtName.IndexOfAny("+-$?@".ToCharArray()) == 0)
                {
                    if (evtName.StartsWith("@")) { evtName = evtName.Remove(0, 1); evtState = SyncEventDescriptor.EEventState.Default; }
                    if (evtName.StartsWith("+")) { evtName = evtName.Remove(0, 1); evtState = SyncEventDescriptor.EEventState.Set; }
                    if (evtName.StartsWith("-")) { evtName = evtName.Remove(0, 1); evtState = SyncEventDescriptor.EEventState.Reset; }
                    if (evtName.StartsWith("?"))
                    {
                        evtName = evtName.Remove(0, 1);
                        evtWaitOption = (evtWaitOption == SyncEventDescriptor.EEventUseOption.None
                            ? SyncEventDescriptor.EEventUseOption.Before
                            : SyncEventDescriptor.EEventUseOption.BeforeAndAfter);
                    }
                    if (evtName.StartsWith("$"))
                    {
                        evtName = evtName.Remove(0, 1);
                        evtWaitOption = (evtWaitOption == SyncEventDescriptor.EEventUseOption.None
                            ? SyncEventDescriptor.EEventUseOption.After
                            : SyncEventDescriptor.EEventUseOption.BeforeAndAfter);
                    }
                }

                // check for event processing directives
                int p = evtName.IndexOf('(');
                string badDirectives = "";
                if (p >= 0 && evtName.EndsWith(")"))
                {
                    string directivesStr = evtName.Remove(evtName.Length - 1, 1).Remove(0, p + 1).Trim(StrUtils.CH_SPACES);
                    evtName = evtName.Substring(0, p).Trim(StrUtils.CH_SPACES);
                    string[] directives = directivesStr.Split('|');
                    foreach (string dir in directives)
                    {
                        string s = dir.Trim(StrUtils.CH_SPACES).ToLower();
                        if (string.IsNullOrEmpty(s)) continue;
                        switch (s)
                        {
                            case "default": evtState = SyncEventDescriptor.EEventState.Default; break;
                            case "set": evtState = SyncEventDescriptor.EEventState.Set; break;
                            case "reset": evtState = SyncEventDescriptor.EEventState.Reset; break;
                            case "nowait": evtWaitOption = SyncEventDescriptor.EEventUseOption.None; break;
                            case "waitbefore": evtWaitOption = SyncEventDescriptor.EEventUseOption.Before; break;
                            case "waitafter": evtWaitOption = SyncEventDescriptor.EEventUseOption.After; break;
                            case "waitboth": evtWaitOption = SyncEventDescriptor.EEventUseOption.BeforeAndAfter; break;
                            case "autoset": evtAutoSetOption = SyncEventDescriptor.EEventUseOption.After; break;
                            case "autosetafter": evtAutoSetOption = SyncEventDescriptor.EEventUseOption.After; break;
                            case "autosetbefore": evtAutoSetOption = SyncEventDescriptor.EEventUseOption.Before; break;
                            case "autoreset": evtAutoResetOption = SyncEventDescriptor.EEventUseOption.After; break;
                            case "autoresetafter": evtAutoResetOption = SyncEventDescriptor.EEventUseOption.After; break;
                            case "autoresetbefore": evtAutoResetOption = SyncEventDescriptor.EEventUseOption.Before; break;
                            case "autosetscope":
                                evtAutoSetOption = SyncEventDescriptor.EEventUseOption.Before;
                                evtAutoResetOption = SyncEventDescriptor.EEventUseOption.After;
                                break;
                            case "autoresetscope":
                                evtAutoResetOption = SyncEventDescriptor.EEventUseOption.Before;
                                evtAutoSetOption = SyncEventDescriptor.EEventUseOption.After;
                                break;
                            default:
                                if (!string.IsNullOrEmpty(badDirectives))
                                    badDirectives += ",";
                                badDirectives += s;
                                break;
                        }
                    }
                }

                // if event name finally is empty - skip it
                if (string.IsNullOrEmpty(evtName)) continue;

                // if specified event name was already created - skip it
                if (createdEvents.Contains(evtName.ToLower())) continue;

                // create event descriptor object
                bool isNewEvtCreated;
                SyncEventDescriptor evtDescr = new SyncEventDescriptor(evtName, evtState, out isNewEvtCreated);

                Trace.WriteLineIf(ExternalCommands.TrcLvl.TraceInfo, ExternalCommands.TrcLvl.TraceInfo ? string.Format(
                    "   = exec[ {0} ] -> CreateSyncEvent( {1}, state={2}, waitOption={3}, autoSet={4}, autoReset={5} ). Event was {6}.",
                    this.Name, evtName, evtState, evtWaitOption, evtAutoSetOption, evtAutoResetOption, (isNewEvtCreated ? "CreatedNew" : "Exists")) : "");

                if (!string.IsNullOrEmpty(badDirectives))
                    Trace.WriteLineIf(ExternalCommands.TrcLvl.TraceWarning, ExternalCommands.TrcLvl.TraceWarning ? string.Format(
                        "   = exec[ {0} ] -> Bad event directives: {1}",
                        this.Name, badDirectives) : "");

                evtDescr.WaitOption = evtWaitOption;
                evtDescr.AutoSetOption = evtAutoSetOption;
                evtDescr.AutoResetOption = evtAutoResetOption;
                createdEvents.Add(evtName.ToLower());
                this.syncEvents.Add(evtDescr);
            }
        }

        protected bool getOption(EOptions pOpt) 
        { 
            return ((this.Options & pOpt) == pOpt); 
        }

        protected void setOption(EOptions pOpt, bool pNewState)
        {
            if (pNewState)
                this.Options |= pOpt;
            else
                this.Options &= ~pOpt;
        }

        protected string composeEnvironment()
        {
            string text = "";
            foreach (KeyValuePair<string, string> item in this.CustomEnvironment)
            {
                text += string.Format("{0}={1}\n", item.Key, item.Value);
            }
            return text;
        }

        protected void parseEnvironment(string pText)
        {
            this.CustomEnvironment.Clear();
            if (string.IsNullOrEmpty(pText)) return ;

            string text = StrUtils.AdjustLineBreaks(pText, "\n");
            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                string pn = StrUtils.GetToPattern(line, "=");
                string pv = StrUtils.GetAfterPattern(line, "=");
                this.CustomEnvironment[pn] = pv;
            }
        }

        private ExternalCommands owner;
        private List<SyncEventDescriptor> syncEvents = null;

        #endregion // Implementation Details
    }


    /// <summary>
    /// 
    /// </summary>
    public class SyncEventDescriptor
    {
        [Flags]
        public enum EEventUseOption
        {
            None = 0,
            Before = 0x01,
            After = 0x02,
            BeforeAndAfter = 0x03
        }

        public enum EEventState
        { 
            Default,
            Reset,
            Set,
        }

        public string Name;
        public EventWaitHandle Event;
        public EEventUseOption WaitOption = EEventUseOption.None;
        public EEventUseOption AutoSetOption = EEventUseOption.None;
        public EEventUseOption AutoResetOption = EEventUseOption.None;

        public SyncEventDescriptor(string pName, EEventState pEventState, out bool pIsNewEvtCreated)
        {
            this.Name = pName;

            bool evtState = (pEventState == EEventState.Set);
            this.Event = new EventWaitHandle(evtState, EventResetMode.ManualReset, this.Name, out pIsNewEvtCreated);

            switch (pEventState)
            {
                case EEventState.Reset: this.Event.Reset(); break;
                case EEventState.Set: this.Event.Set(); break;
            }
        }

    }

}
