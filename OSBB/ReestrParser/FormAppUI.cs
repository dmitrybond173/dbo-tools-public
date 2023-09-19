/* 
 * Reestr Parser app.
 *
 * UI for Reestr Parser app.
 * Provide basic UI for maintaining CLI parameters and run the parser.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Aug, 2023
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XService.Utils;

namespace ReestrParser
{
    public partial class FormAppUI : Form
    {
        public static TraceSwitch TrcLvl { get { return ToolSettings.TrcLvl; } }

        public FormAppUI()
        {
            InitializeComponent();
        }

        public ToolSettings Settings;

        private ParserEngine engine;

        private void performParse()
        {            
            DateTime t1 = DateTime.Now;
            stLab1.Text = "Parsing...";
            statusStrip1.Refresh();
            bool isOk = false;
            try
            {
                this.engine = new ParserEngine(this.Settings);
                try
                {
                    this.engine.Parse();

                    string msg;
                    msg = string.Format("+++ Parsing completed. Elapsed time: {0} sec", (DateTime.Now - t1).TotalSeconds.ToString("N1"));
                    txtLog.Text += (msg + Environment.NewLine);

                    msg = string.Format("= Statistic: {0} total files, {1} total items", this.engine.ParsedFiles.Count, this.engine.Statistic.totalItems);
                    txtLog.Text += (msg + Environment.NewLine);

                    msg = string.Format("= Statistic: {0} total good files, {1} total wrong files, {2} total failures",
                        this.engine.Statistic.totalGoodFiles, this.engine.Statistic.totalWrongFiles, this.engine.Statistic.totalFailures);
                    txtLog.Text += (msg + Environment.NewLine);

                    msg = string.Format("= Statistic: {0} total sum, {1} total commission, {2} total confirmed",
                        this.engine.Statistic.totalSum, this.engine.Statistic.totalCommission, this.engine.Statistic.totalConfirmed);
                    txtLog.Text += (msg + Environment.NewLine);

                    this.engine.GenerateOutput();

                    isOk = true;
                }
                catch (Exception exc)
                {
                    string msg = string.Format(
                        "{0}\nat {1}", ErrorUtils.FormatErrorMsg(exc), ErrorUtils.FormatStackTrace(exc));

                    Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? msg : "");
                    if (exc.InnerException != null)
                        Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format(
                            "  !ERR.internal: {0}", ErrorUtils.UnrollException(exc)) : "");

                    MessageBox.Show(msg, "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally 
            {
                stLab1.Text = (isOk ? "Completed." : "Failure!");
                statusStrip1.Refresh();
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFilespecs.Text))
                dlgBrowseForDir.SelectedPath = Path.GetDirectoryName(txtFilespecs.Text);

            if (dlgBrowseForDir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string s = PathUtils.IncludeTrailingSlash(dlgBrowseForDir.SelectedPath) + "*.txt";
                txtFilespecs.Text = s;

                this.Settings.SrcFiles.Clear();
                this.Settings.SrcFiles.Add(new ToolSettings.FileSource(this.Settings, dlgBrowseForDir.SelectedPath, "*.txt"));
            }
        }

        private void FormAppUI_Shown(object sender, EventArgs e)
        {
            if (this.Settings.SrcFiles.Count > 0)
            {
                ToolSettings.FileSource fs = this.Settings.SrcFiles[0];
                txtFilespecs.Text = PathUtils.IncludeTrailingSlash(fs.Path) + fs.Filespecs;
            }

            chkDb.Checked = this.Settings.Outputs.Contains("db");
            chkExcel.Checked = this.Settings.Outputs.Contains("excel");

            this.BringToFront();
        }

        private void chkOutput_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            if (chk.Tag == null)
            {
                chk.Checked = false;
                return;
            }

            string id = chk.Tag.ToString();
            if (chk.Checked)
            {
                if (!this.Settings.Outputs.Contains(id))
                    this.Settings.Outputs.Add(id);
            }
            else
            {
                if (this.Settings.Outputs.Contains(id))
                    this.Settings.Outputs.Remove(id);
            }
            stLab2.Text = StrUtils.Join(this.Settings.Outputs.ToArray(), ",");
        }

        private void btnParse_Click(object sender, EventArgs e)
        {
            performParse();
        }
    }
}
