/*
 * BuildToOrder Shopfloor R2: ...
 * BlueYonder.
 *
 * Written by Dmitry Bond. at March 2021.
 * Based on Dmitry Bond's prototype code from June 2012.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using XService.Security;
using XService.Utils;

[assembly: CustomVersionInfo("RCS:FormConsole.cs", "$Date: 2021/03/16 16:05:37Z $ $RCSfile: FormConsole.cs $ $State: Exp $ $Author: dbondare $ $Revision: 1.2 $ $Locker: $ $Name: $")]

namespace XService.UI.CommonForms
{
    /// <summary>
    /// Console UI displaying Trace output.
    /// </summary>
    public partial class FormConsole : Form
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("TraceLevel", "TraceLevel");

        #region API

        /// <summary>
        /// When reach specified maximum of records in ConsoleUI oldest will be removed. 
        /// Can be defined in "ConsoleUi.MaxLogMessages" config item
        /// </summary>
        public static int MaxLogMessageInConsoleUi = 500;
        public static int LogMessageInConsoleUi_Delta = 20;

        /// <summary>Title of Console UI</summary>
        public static string ConsoleTitle
        {
            get { return consoleTitle; }
            set
            {
                consoleTitle = value;
                if (Instance != null && IsActive)
                    Instance.SetCaption(consoleTitle);
            }
        }

        /// <summary>Flag defining if Console UI is active or closed</summary>
        public static bool IsActive = false;

        /// <summary>Initializations to be executed in background when ConsoleUI displayed</summary>
        public static WaitCallback AppInitHandler;

        /// <summary>Finalizations to be executed when ConsoleUI is closed</summary>
        public static WaitCallback AppCloseHandler;

        /// <summary>Instance of ConsoleUI</summary>
        public static FormConsole Instance = null;

        #endregion // API

        public FormConsole()
        {
            Instance = this;
            InitializeComponent();

            loadConfig();
        }

        public delegate void SetCaptionMethod(string pText);
        public void SetCaption(string pText)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new SetCaptionMethod(SetCaption), pText);
            }
            else
            {
                this.Text = pText;
            }
        }

        #region Implementation details

        private List<string> logMessages = new List<string>();
        private BackgroundWorker logMessagesHandler = new BackgroundWorker();

        // Note: have to use logMessagesCopy list to copy trace messages to display
        // So, to detach messages to display from locked context. It is required to avoid GUI thead deadlocks!
        private List<string> logMessagesCopy = new List<string>();

        private void loadConfig()
        {
            int n;
            string s = ConfigurationManager.AppSettings["ConsoleUi.MaxLogMessages"];
            if (!string.IsNullOrEmpty(s) && StrUtils.GetAsInt(s, out n))
            {
                MaxLogMessageInConsoleUi = n;
            }

            s = ConfigurationManager.AppSettings["ConsoleUi.LogMessagesDelta"];
            if (!string.IsNullOrEmpty(s) && StrUtils.GetAsInt(s, out n))
            {
                LogMessageInConsoleUi_Delta = n;
            }

            TraceListener cbListener = null;
            foreach (TraceListener lsnr in Trace.Listeners)
            {
                if (lsnr is CallbackTraceListener)
                    cbListener = (CallbackTraceListener)lsnr;
            }
            if (cbListener == null)
            {
                cbListener = new CallbackTraceListener();
                Trace.Listeners.Add(cbListener);
            }
        }

        private void onTraceWrite(string pMessage, bool pWriteLine)
        {
            lock (this.logMessages)
            {
                this.logMessages.Add(pMessage + (pWriteLine ? Environment.NewLine : ""));
            }
        }

        private delegate void displayTraceMsgsMethod(string[] pMessages, bool q);
        private void displayTraceMsgs(string[] pMessages, bool q)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new displayTraceMsgsMethod(this.displayTraceMsgs), pMessages, q);
            }
            else
            {
                ListViewItem li = null;
                lvLog.BeginUpdate();
                try
                {
                    if (lvLog.Items.Count > (MaxLogMessageInConsoleUi + LogMessageInConsoleUi_Delta))
                        while (lvLog.Items.Count > MaxLogMessageInConsoleUi)
                            lvLog.Items.RemoveAt(0);

                    foreach (string msg in pMessages)
                    {
                        li = new ListViewItem(new string[] {
						    StrUtils.CompactNskTimestampOf(DateTime.Now).Substring(0, 19),
						    msg
						    });
                        lvLog.Items.Add(li);
                    }
                }
                finally
                {
                    lvLog.EndUpdate();
                    if (li != null) li.EnsureVisible();
                }
            }
        }

        private void logMessagesHandler_DoWork(object sender, DoWorkEventArgs e)
        {
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                "* ConsoleUI.Starting messages loop...", e) : "");

            Thread.CurrentThread.Name = "GuiLogMessagesHandler";
            while (IsActive)
            {
                try
                {
                    string[] msgsToDisplay = null;
                    lock (this.logMessages)
                    {
                        if (this.logMessages.Count > 0)
                        {
                            msgsToDisplay = this.logMessages.ToArray();
                            this.logMessages.Clear();
                        }
                    }

                    if (msgsToDisplay != null && msgsToDisplay.Length > 0)
                    {
                        displayTraceMsgs(msgsToDisplay, true);
                    }

                    if (!IsActive)
                        break;

                    Thread.Sleep(200);
                }
                catch { break; }
            }
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                "* ConsoleUI.Exited messages loop...", e) : "");
        }

        #endregion // Implementation details

        #region Form Event Handlers

        private void FormConsole_Load(object sender, EventArgs e)
        {
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                "* ConsoleUI.Load()...") : "");

            this.Text = ConsoleTitle;
            IsActive = true;

            // setup backgropund handler for log messages 
            this.logMessagesHandler.DoWork += new DoWorkEventHandler(logMessagesHandler_DoWork);
            this.logMessagesHandler.RunWorkerAsync();

            // set output for Trace
            CallbackTraceListener.OnWrite = this.onTraceWrite;

            Thread.Sleep(330);

            // ensure to execute initializations
            if (AppInitHandler != null)
                ThreadPool.QueueUserWorkItem(AppInitHandler);
        }

        private void FormConsole_FormClosing(object sender, FormClosingEventArgs e)
        {
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(
                "* ConsoleUI.FormClosing( reason={0} )...", e.CloseReason) : "");

            // ensure to execute finalizations
            if (AppCloseHandler != null)
                AppCloseHandler(this);

            IsActive = false;
            Instance = null;

            Thread.Sleep(550);
        }

        private static string consoleTitle;

        #endregion // Form Event Handlers
    }
}
