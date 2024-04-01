/*
 * Component for bi-directional communication with running console process.
 * Written by Dmitry Bond. at March 14, 2021

>prompt /?
Changes the cmd.exe command prompt.

PROMPT [text]

  text    Specifies a new command prompt.

Prompt can be made up of normal characters and the following special codes:

  $A   & (Ampersand)
  $B   | (pipe)
  $C   ( (Left parenthesis)
  $D   Current date
  $E   Escape code (ASCII code 27)
  $F   ) (Right parenthesis)
  $G   > (greater-than sign)
  $H   Backspace (erases previous character)
  $L   < (less-than sign)
  $N   Current drive
  $P   Current drive and path
  $Q   = (equal sign)
  $S     (space)
  $T   Current time
  $V   Windows version number
  $_   Carriage return and linefeed
  $$   $ (dollar sign)
 
 prompt $p$C$$$F$g$_
 * c:\($)>

If Command Extensions are enabled the PROMPT command supports
the following additional formatting characters:

  $+   zero or more plus sign (+) characters depending upon the
       depth of the PUSHD directory stack, one character for each
       level pushed.

  $M   Displays the remote name associated with the current drive
       letter or the empty string if current drive is not a network
       drive.

 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using XService.Configuration;
using XService.Utils;

namespace XService.Utils.IO
{
    /// <summary>
    /// Interactive shell which supports executing shell commands 
    /// </summary>
    public class InteractiveShellHandler
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("InteractiveShellHandler", "InteractiveShellHandler");

        public static string CMD_FULLPATH = "%ComSpec%";
        public static string CMD_PROMPT = "prompt $p$C$$$F$g$_"; // need to have EOL in prompt
        public static string CMD_MARKER = "($)>"; // marker how to recognize if command execution completed

        public InteractiveShellHandler()
        {
            this.ShellCommand = CMD_FULLPATH;
            this.ShellInitialization = CMD_PROMPT;
            this.ShellMarker = CMD_MARKER;

            this.ActiveCommands = new List<string>();
            this.Output = new List<string>();
            this.CmdOutput = new StringBuilder(0x1000);
        }

        public override string ToString()
        {
            return String.Format("ShellHandler[{0}; {1}]", (this.IsAlive ? "Alive" : "Stopped"), this.ShellCommand);
        }

        public delegate void OutputReceivedMethod(object sender, string pText);
        public OutputReceivedMethod OutputReceived;

        /// <summary>Notification on command execution. When pText is null that means - command execution just started</summary>
        public delegate void CmdExecNotificationMethod(object sender, DateTime pTs, StringBuilder pText);
        public CmdExecNotificationMethod CmdExecNotification;

        /// <summary>Shell command to start interactive shell processor</summary>
        public string ShellCommand { get; set; }

        /// <summary>Shell initialization command (optional)</summary>
        public string ShellInitialization { get; set; }

        /// <summary>Shell marker how to recognize end of interactive command execition</summary>
        public string ShellMarker { get; set; }

        /// <summary>String to mark log output</summary>
        public string LogMarker { get; set; }

        public List<string> ActiveCommands { get; protected set; }

        /// <summary>Currently executing command</summary>
        public string CurrentCommand 
        {
            get 
            { 
                lock (this.ActiveCommands)
                {
                    if (this.ActiveCommands.Count > 0)
                        return this.ActiveCommands[this.ActiveCommands.Count - 1];
                    else
                        return "???";
                }
            }
        }

        /// <summary>Full output of interactive session</summary>
        public List<string> Output { get; protected set; }

        /// <summary>Output of interactive command execition (last executed command)</summary>
        public StringBuilder CmdOutput { get; protected set; }

        /// <summary>When shell was started (if still alive)</summary>
        public DateTime Started
        {
            get
            {
                try { return (this.IsAlive ? this.prc.StartTime : DateTime.MinValue); }
                catch { return DateTime.MinValue; }
            }
        }

        /// <summary>When last command started</summary>
        public DateTime CommandStarted { get; protected set; }

        /// <summary>When last command completed</summary>
        public DateTime CommandCompleted { get; protected set; }

        /// <summary>Elapsed execution time of last command</summary>
        public TimeSpan ExecutionTime { get; protected set; }

        /// <summary>Returns true if CMD still alive</summary>
        public bool IsAlive
        {
            get 
            { 
                try { return (this.prc != null && !this.prc.HasExited); } 
                catch { return false; } 
            }
        }

        /// <summary>Returns true if shell still executing some command</summary>
        public bool IsBusy
        {
            get { return (this.IsAlive && this.isBusy); }
        }

        /// <summary>Ensure CMD started</summary>
        public void Start()
        {
            if (this.prc != null && !this.prc.HasExited) return;

            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("-> InteractiveShellHandler.Start()...") : "");

            this.st = new ProcessStartInfo();
            st.FileName = this.ShellCommand;
            if (st.FileName.IndexOf('%') >= 0)
                st.FileName = Environment.ExpandEnvironmentVariables(st.FileName);
            st.RedirectStandardInput = true;
            st.RedirectStandardError = true;
            st.RedirectStandardOutput = true;
            st.UseShellExecute = false;
            st.CreateNoWindow = true;
            st.WindowStyle = ProcessWindowStyle.Minimized;

            this.Output.Clear();
            this.prc = Process.Start(st);
            this.prc.Exited += cmd_Exited;
            ThreadPool.QueueUserWorkItem(this.waitStdOut);
            ThreadPool.QueueUserWorkItem(this.waitStdErr);
            
            Trace.WriteLine(string.Format("CMD started: pid={0}, h={1}, sessId={2}", 
                this.prc.Id, this.prc.Handle, this.prc.SessionId));

            if (!string.IsNullOrEmpty(this.ShellInitialization))
            {
                ExecuteCommands(this.ShellInitialization, true, "INIT");
            }
        }

        /// <summary>Execute specified interactive command, set specified LogMarker while executing</summary>
        /// <param name="pCmd">Interactive command to execute</param>
        /// <param name="pLogMarker">Log marker to use while command running</param>
        public void ExecuteCmd(string pCmd, string pLogMarker)
        {
            this.savedLogMakrer = this.LogMarker;
            this.LogMarker = pLogMarker;
            Trace.WriteLine(string.Format("= ShellHandler.set LogMarker = {0} /saved={1}", this.LogMarker, this.savedLogMakrer));

            // Note: we need to keep assigned LogMarker because this call exits but execution continue!
            ExecuteCmd(pCmd);
        }

        /// <summary>Execute specified interactive command</summary>
        /// <param name="pCmd">Interactive command to execute</param>
        public void ExecuteCmd(string pCmd)
        {
            if (!this.IsAlive)
                throw new ObjectError("Shell is not running!", this);
 
            //this.CmdOutput.Clear();
            this.CmdOutput.Length = 0;

            this.CommandStarted = DateTime.Now;
            this.CommandCompleted = DateTime.MinValue;
            this.ExecutionTime = TimeSpan.MinValue;
            if (this.CmdExecNotification != null)
                this.CmdExecNotification(this, this.CommandStarted, null);

            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("-> ShellHandler.Exec> {0}", pCmd) : "");
            lock (this.ActiveCommands)
                this.ActiveCommands.Add(pCmd);
            this.isBusy = true;
            this.prc.StandardInput.WriteLine(pCmd);
        }

        public void ExecuteCommands(string pCommands, bool pWait, string pMarker)
        {
            string[] items = StrUtils.AdjustLineBreaks(pCommands, "\n").Trim(StrUtils.CH_SPACES).Split('\n');
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("--> ExecCommands: {0} items", items.Length) : "");
            foreach (string it in items)
            {
                string stmt = it.Trim(StrUtils.CH_SPACES);
                if (string.IsNullOrEmpty(stmt)) continue;
                ExecuteCmd(stmt, pMarker);
                if (pWait)
                    WaitCmdCompletion();
            }
        }

        public void WaitCmdCompletion()
        {
            WaitCmdCompletion(1);
        }

        public void WaitCmdCompletion(int pPauseMs)
        {
            if (pPauseMs > 0)
                Thread.Sleep(pPauseMs);

            int counter = 0;
            do
            {
                Thread.Sleep(100);
                counter++;
                if ((counter % 50) == 0)
                    Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(" ! waiting for command completuion (#{0}) ", counter) : "");
            }
            while (this.IsBusy); // !this.executionCompleted);
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(" ! Command [{0}] completed. Elapsed time = {1} sec", 
                this.CurrentCommand, this.ExecutionTime.TotalSeconds.ToString("N1")) : "");
            
            lock (this.ActiveCommands)
            {
                if (this.ActiveCommands.Count > 0)
                    this.ActiveCommands.RemoveAt(0);
            }
        }

        #region Implementation details

        private void cmd_Exited(object sender, EventArgs e)
        {
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("! ShellHandler> shell exited!") : "");
            this.st = null;
            if (this.prc != null)
            {
                Process p = this.prc;
                this.prc = null;
                p.Dispose();
            }
        }

        private void waitStdOut(object state)
        {
            StreamReader sr = this.prc.StandardOutput;
            while (this.IsAlive)
            {
                int ch = sr.Peek();
                if (ch != -1)
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lock (this.Output)
                            this.Output.Add(line);

                        Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(" + StdOut{0}: {1}", 
                            (this.LogMarker != null ? string.Format("[{0}]", this.LogMarker) : ""), line) : "");

                        bool isEndOfCommand = line.EndsWith(this.ShellMarker);

                        if (isEndOfCommand)
                        {
                            this.CommandCompleted = DateTime.Now;
                            this.ExecutionTime = this.CommandCompleted - this.CommandStarted;

                            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                                " + ShellHandler: command completed. Elapsed time = {0} sec", this.ExecutionTime.TotalSeconds.ToString("N2")) : "");
                            this.isBusy = false;
                        }

                        if (!isEndOfCommand)
                        {
                            lock (this.CmdOutput)
                                this.CmdOutput.AppendLine(line);
                        }

                        if (this.OutputReceived != null)
                        {
                            if (!string.IsNullOrEmpty(this.LogMarker))
                                line = this.LogMarker + line;
                            this.OutputReceived(this, line);
                        }

                        if (isEndOfCommand && this.CmdExecNotification != null)
                        {
                            this.CmdExecNotification(this, DateTime.Now, this.CmdOutput);

                            if (this.savedLogMakrer != null)
                            {
                                this.LogMarker = this.savedLogMakrer;
                                this.savedLogMakrer = null;
                                Trace.WriteLine(string.Format("= ShellHandler.Restored LogMarker = {0}", this.LogMarker));
                            }
                        }
                    }
                }
                Thread.Sleep(20);
            }
        }

        private void waitStdErr(object state)
        {
            StreamReader sr = this.prc.StandardError;
            while (this.IsAlive)
            {
                int ch = sr.Peek();
                if (ch != -1)
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lock (this.Output)
                            this.Output.Add(line);

                        Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(" + StdErr{0}: {1}",
                            (this.LogMarker != null ? string.Format("[{0}]", this.LogMarker) : ""), line) : "");

                        lock (this.CmdOutput)
                            this.CmdOutput.AppendLine(line);
                    }
                }
                Thread.Sleep(100);
            }
        }

        private Process prc;
        private ProcessStartInfo st;
        private bool isBusy = false;
        private string savedLogMakrer;

        #endregion // Implementation details

    }
}
