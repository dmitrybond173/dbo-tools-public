/* 
 * App for OSBB Revision.
 *
 * Main UI.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Feb, 2024
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using XService.Utils;
using Excel = Microsoft.Office.Interop.Excel;

namespace OsbbRev2
{
    public partial class FormMain : Form
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("TraceLevel", "TraceLevel");

        public const int MAX_LOGGER_ITEMS = 300;

        public FormMain()
        {
            InitializeComponent();
        }

        private string originalCaption;
        private string sourceWorksheetName = "СВОДНАЯ";
        private string targetWorksheetName = "Статистика";

        private Classificator classificator;

        private Excel.Workbook wBook;
        private Excel.Worksheet wSheet = null;
        private Excel.Worksheet wTrgSheet = null;

        private Excel.Application _app;
        private Excel.Application app
        {
            get
            {
                if (_app == null)
                {
                    _app = new Excel.Application();
                    if (_app == null)
                        throw new Exception("Fail to create Excel app!");

                    _app.Visible = chkAppVisible.Checked;
                    _app.DisplayAlerts = false;
                }
                return _app;
            }
        }

        private void openFile(string pFilename)
        {
            Trace.WriteLine(string.Format("+++ open file: {0}", pFilename));
            DateTime t1 = DateTime.Now;
            setStatus("Loading...", pFilename);
            this.wBook = this.app.Workbooks.Open(pFilename);
            DateTime t2 = DateTime.Now;
            setStatus("Ready.", string.Format("[{0} .. {1}]{2} sec", 
                StrUtils.CompactNskTimestampOf(t1).Substring(0, 15),
                StrUtils.CompactNskTimestampOf(t2).Substring(0, 15),
                (t2 - t1).TotalSeconds.ToString("N1"))
                );
            setCaption(string.Format("{0} @ {1}", Path.GetFileName(pFilename), Path.GetDirectoryName(pFilename)));

            btnAnalyze.Enabled = true;
        }

        private bool preAnalysisCheck(out string pErrMsg)
        {
            pErrMsg = null;

            if (this.wBook == null)
            {
                pErrMsg = "Excel file is not opened!";
                Trace.WriteLine(string.Format("ERR: {0}", pErrMsg));
                MessageBox.Show(pErrMsg, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            this.wSheet = null;
            try
            {
                Trace.WriteLine(string.Format("? Searching for WorkSheet[{0}]", this.sourceWorksheetName));
                this.wSheet = this.wBook.Worksheets[this.sourceWorksheetName];
                if (this.wSheet == null)
                    throw new Exception("Not found");
            }
            catch
            {
                if (this.wSheet == null)
                {
                    pErrMsg = string.Format("Cannot find required worksheet: {0}", this.sourceWorksheetName);
                    Trace.WriteLine(string.Format("ERR: {0}", pErrMsg));
                    MessageBox.Show(pErrMsg, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            this.wTrgSheet = null;
            try
            {
                this.wTrgSheet = this.wBook.Worksheets[this.targetWorksheetName];
                if (this.wTrgSheet == null)
                    throw new Exception("Not found");

                Trace.WriteLine(string.Format("- Deleting old WorkSheet[{0}]", this.targetWorksheetName));
                this.wTrgSheet.Delete();
                this.wTrgSheet = null;
            }
            catch { }
            if (this.wTrgSheet == null)
            {
                Trace.WriteLine(string.Format("+ Creating WorkSheet[{0}]", this.targetWorksheetName));
                this.wTrgSheet = this.wBook.Worksheets.Add(After: wSheet);
                this.wTrgSheet.Name = this.targetWorksheetName;
            }

            return true;
        }

        private void prepareAnalysis()
        {
            // read all required values from UI
            if (chkAddDetalization.Checked)
                classificator.Flags |= Classificator.EFlags.AddDetalisation;
            else
                classificator.Flags &= ~Classificator.EFlags.AddDetalisation;
        }

        private static void runPerformAnalysis(object state)
        { 
            FormMain form = (FormMain)state;
            form.performAnalysis();
        }

        private void performAnalysis()
        {
            string errMsg;
            if (!preAnalysisCheck(out errMsg))
            {
                setStatus("Failure!", errMsg);
                return;
            }

            wSheet.Activate();

            DateTime t1 = DateTime.Now;
            int iRow = 10;
            int iCol = 7;
            bool isOpened = false;
            setStatus("Analyzing...", null);
            List<CategoryDescriptor> list = new List<CategoryDescriptor>();
            while (true)
            {
                Excel.Range row = wSheet.Rows[iRow];
                var vDescr = row.Cells[iCol].Value;

                if (vDescr == null)
                {
                    if (isOpened)
                        break;
                }

                bool isSkip = (StrUtils.IsSameText(vDescr, "xxx"));
                if (!isSkip)
                {
                    if (vDescr != null)
                    {
                        isOpened = true;

                        DataItem item = DataItem.Load(row, iRow);
                        if (item != null)
                        {
                            classificator.Analyze(item, row, iRow);
                        }
                        else
                            Trace.WriteLine(string.Format("SKIP: fail to load data item from row#{0}", iRow));
                    }
                }

                iRow++;
                if ((iRow % 10) == 0)
                    setStatus(null, string.Format("Row# {0}, {1} categories found", iRow, classificator.Data.Count));

                //DBG: if (iRow > 500) break;
            }
            DateTime t2 = DateTime.Now;

            setStatus(null, string.Format("Building statistic..."));
            classificator.FlushData(wTrgSheet);

            setStatus(null, string.Format("Saving file..."));
            wBook.Save();

            app.Visible = true;

            setStatus("Ready.", string.Format("{0} rows, dT[{1}..{2}]={3} sec", 
                iRow, 
                StrUtils.CompactNskTimestampOf(t1).Substring(0, 15),
                StrUtils.CompactNskTimestampOf(t2).Substring(0, 15),
                (t2 - t1).TotalSeconds.ToString("N1")) 
                );
        }

        #region Update UI

        private delegate void setCaptionMethod(string pText);
        private void setCaption(string pCaption)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new setCaptionMethod(setCaption), pCaption);
            }
            else
            {
                Trace.WriteLine(string.Format("= Caption: [{0}]",
                    (pCaption != null ? pCaption : "(null)") ));

                this.Text = this.originalCaption;
                if (pCaption != null) 
                    this.Text += string.Format(" [{0}]", pCaption);
                this.Refresh();
            }
        }

        private delegate void setStatusMethod(string pLab1, string pLab2);
        private void setStatus(string pLab1, string pLab2)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new setStatusMethod(setStatus), pLab1, pLab2);
            }
            else
            {
                Trace.WriteLine(string.Format("= Status: [{0}] [{1}]",
                    (pLab1 != null ? pLab1 : "(null)"),
                    (pLab2 != null ? pLab2 : "(null)")
                    ));

                if (pLab1 != null) setStLab(stLab1, pLab1); // { stLab1.Text = pLab1; }
                if (pLab2 != null) setStLab(stLab2, pLab2); // { stLab2.Text = pLab2; }

                statusStrip1.Refresh();
            }
        }

        private void setStLab(ToolStripStatusLabel pLab, string pMsg)
        {
            //pLab.Text = pMsg;

            if (pMsg.StartsWith("+"))
            {
                int n = pLab.Text.IndexOf("+");
                if (n >= 0)
                    pLab.Text = pLab.Text.Substring(0, n);

                pLab.Text += pMsg;
            }
            else
                pLab.Text = pMsg;
        }

        private void setStatusLab2(string pLab2)
        {
            setStatus(null, pLab2);
        }

        #endregion // Update UI

        #region UI.Logger

        private void writeLogger(string pMessage, bool pWriteLine)
        {
            string ts = StrUtils.NskTimestampOf(DateTime.Now).Substring(11, 12);

            while (lvLogger.Items.Count > MAX_LOGGER_ITEMS)
            {
                lvLogger.Items.RemoveAt(0);
            }

            ListViewItem li = null;
            if (!pWriteLine)
            {
                if (lvLogger.Items.Count > 0)
                {
                    li = lvLogger.Items[lvLogger.Items.Count - 1];
                    li.SubItems[0].Text = ts;
                    li.SubItems[1].Text += pMessage;
                }
                else
                    pWriteLine = true;
            }
            if (pWriteLine)
            {
                if (pMessage.IndexOf('\n') >= 0)
                {
                    string[] lines = pMessage.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                    foreach (string line in lines)
                    {
                        li = new ListViewItem(new string[] { ts, line });
                        lvLogger.Items.Add(li);
                    }
                }
                else
                {
                    li = new ListViewItem(new string[] { ts, pMessage });
                    lvLogger.Items.Add(li);
                }
            }
            if (li != null)
                li.EnsureVisible();
        }

        private void onTraceWrite(string pMessage, bool pWriteLine)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new CallbackTraceListener.OnWriteProc(this.writeLogger), pMessage, pWriteLine);
            }
            else
            {
                this.writeLogger(pMessage, pWriteLine);
            }
        }

        #endregion // UI.Logger

        #region Form Events Handlers

        private void FormMain_Load(object sender, EventArgs e)
        {
            CallbackTraceListener.OnWrite += this.onTraceWrite;

            Trace.WriteLine(string.Format("--- FormMain_Load()"));

            this.originalCaption = this.Text;

            string s = ConfigurationManager.AppSettings["Filename"];
            if (!string.IsNullOrEmpty(s))
                txtFilename.Text = s;

            s = ConfigurationManager.AppSettings["WorksheetName"];
            if (!string.IsNullOrEmpty(s))
                this.sourceWorksheetName = s;

            s = ConfigurationManager.AppSettings["AppVisible"];
            if (!string.IsNullOrEmpty(s))
                this.chkAppVisible.Checked = StrUtils.GetAsBool(s);

            s = ConfigurationManager.AppSettings["AddDetalization"];
            if (!string.IsNullOrEmpty(s))
                this.chkAddDetalization.Checked = StrUtils.GetAsBool(s);

            s = ConfigurationManager.AppSettings["UseBackgroundWorker"];
            if (string.IsNullOrEmpty(s))
                s = ConfigurationManager.AppSettings["UseWorker"];
            if (!string.IsNullOrEmpty(s))
                this.chkUseWorker.Checked = StrUtils.GetAsBool(s);
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            Trace.WriteLine(string.Format("--- FormMain_Shown()"));

            setStatus("Loading config...", "");
            this.classificator = new Classificator();
            this.classificator.UpdateStatusBar += this.setStatusLab2;
            setStatus("Ready.", "");
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Trace.WriteLine(string.Format("--- FormMain_FormClosed( {0} )", e.CloseReason));

            if (_app != null)
            {
                Excel.Application a = _app;
                _app = null;
                a.Quit();
            }
        }

        private void FormMain_ResizeEnd(object sender, EventArgs e)
        {
            Trace.WriteLine(string.Format("--- FormMain_ResizeEnd( cli={0}x{1} )",
                statusStrip1.ClientRectangle.Width, statusStrip1.ClientRectangle.Height ));

            stLab2.Width = statusStrip1.ClientRectangle.Width - stLab1.Width - 16;
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            Trace.WriteLine(string.Format("--- FormMain:btnAnalyze_Click()"));

            prepareAnalysis();

            if (chkUseWorker.Checked)
            {
                ThreadPool.QueueUserWorkItem(runPerformAnalysis, this);
            }
            else
                performAnalysis();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            Trace.WriteLine(string.Format("--- FormMain:btnBrowse_Click()"));

            string fn = txtFilename.Text.Trim();
            if (fn.Length > 0)
            {
                dlgOpen.InitialDirectory = Path.GetDirectoryName(fn);
                dlgOpen.FileName = Path.GetFileName(fn);
            }
            if (dlgOpen.ShowDialog() != DialogResult.OK) return;
            txtFilename.Text = dlgOpen.FileName;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            Trace.WriteLine(string.Format("--- FormMain:btnOpen_Click()"));

            string fn = txtFilename.Text.Trim();
            if (!File.Exists(fn)) 
            {
                MessageBox.Show(string.Format("File [{0}] is not found!", fn), "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            openFile(fn);
        }

        private void chkAppVisible_CheckedChanged(object sender, EventArgs e)
        {
            Trace.WriteLine(string.Format("--- FormMain:chkAppVisible_CheckedChanged()"));

            if (_app != null)
                this.app.Visible = chkAppVisible.Checked;
        }

        #endregion // Form Events Handlers

    }
}
