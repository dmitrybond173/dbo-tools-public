/*
 * Log Facts Extractor: UI for creating new logs parsing project.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-08-24
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogFactExtractor4
{
    public partial class FormNewProject : Form
    {
        public static bool Execute(Form pOwner, LogFactsExtractorEngine pEngine, out ParserProject pProject)
        {
            pProject = null;
            using (FormNewProject frm = new FormNewProject())
            {
                frm.engine = pEngine;
                frm.display();
                bool isOk = (frm.ShowDialog() == DialogResult.OK);
                if (isOk)
                {
                    frm.commit();
                    pProject = frm.project;
                }
                return isOk;
            }
        }

        public FormNewProject()
        {
            InitializeComponent();
        }

        private LogFactsExtractorEngine engine;
        private ParserProject project;
        private string filename;
        private List<FileInfo> filesList = new List<FileInfo>();

        private void display()
        {
            cmbLogTypes.Items.Clear();
            foreach (LogTypeConfig lt in this.engine.Config.LogTypes)
            {
                cmbLogTypes.Items.Add(lt);
            }
        }

        private void commit()
        {
            this.engine.Progress(EProgressAction.Setup, this.filesList.Count + 1, null);

            LogTypeConfig lt = (LogTypeConfig)cmbLogTypes.SelectedItem;
            this.engine.Progress(EProgressAction.Step, 1, string.Format("Creating project [{0}; {1}]...", lt.Name, this.filename));
            this.project = ParserProject.CreateProject(this.engine, (LogTypeConfig)cmbLogTypes.SelectedItem, this.filename, txtComment.Text);
            if (this.project == null)
            {
                return;
            }

            int idx = 0;
            foreach (FileInfo fi in this.filesList)
            {
                idx++;
                lt = this.engine.SelectLogType(fi.Name);
                if (lt != null)
                {
                    this.engine.Progress(EProgressAction.Step, 1, string.Format("Adding file({0}/{1})[{2}; {3}]...", idx, this.filesList.Count, lt.Name, fi.Name));
                    this.project.AddFile(fi.FullName, lt);
                }
            }
            this.engine.Progress(EProgressAction.Completed, 1, string.Format("Project #{0} created.", this.project.ProjectId));
        }

        private bool validate()
        {
            this.filename = txtLocation.Text;
            bool isOk = (chkSingleFile.Checked ? File.Exists(this.filename) : Directory.Exists(this.filename));
            if (!isOk)
            {
                MessageBox.Show(string.Format("{0} ({1}) is not found!", (chkSingleFile.Checked ? "File" : "Directory"), this.filename),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtLocation.Focus();                
            }
            
            if (isOk)
            {
                selectLogType();
                isOk = (cmbLogTypes.SelectedItem != null);
                if (!isOk)
                {
                    MessageBox.Show("Log-type is not selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cmbLogTypes.Focus();
                }
            }

            if (isOk)
            {
                isOk = (this.filesList.Count > 0);
                if (!isOk)
                {
                    MessageBox.Show(string.Format("Cannot find any log files!"),
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtLocation.Focus();
                }
            }

            return isOk;
        }

        private void selectLogType()
        {
            this.filesList.Clear();

            FileInfo[] files;
            if (chkSingleFile.Checked)
                files = new FileInfo[] { new FileInfo(this.filename) };
            else
            { 
                DirectoryInfo dir = new DirectoryInfo(txtLocation.Text);
                files = dir.GetFiles("*.*", SearchOption.AllDirectories);
            }
            int maxUse = 0;
            LogTypeConfig currentLt = null;
            Dictionary<LogTypeConfig, int> ltRefs = new Dictionary<LogTypeConfig, int>();
            foreach (FileInfo fi in files)
            {
                LogTypeConfig lt = this.engine.SelectLogType(fi.Name);
                if (lt != null)
                { 
                    int n = 0;
                    if (ltRefs.TryGetValue(lt, out n))
                        n++;
                    else
                        n = 1;
                    ltRefs[lt] = n;
                    if (n > maxUse)
                    {
                        maxUse = n;
                        currentLt = lt;
                    }
                }
            }

            if (currentLt != null)
            {
                cmbLogTypes.SelectedItem = currentLt;
                this.filesList.AddRange(files);
            }
            else
            {
                MessageBox.Show(string.Format("Cannot recognize any of supported lot-types!"),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Form Events Handlers

        private void cmbLogTypes_Format(object sender, ListControlConvertEventArgs e)
        {
            LogTypeConfig lt = (LogTypeConfig)e.ListItem;
            e.Value = string.Format("{0}", lt.ToString());
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (chkSingleFile.Checked)
            {
                if (dlgOpenFile.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                txtLocation.Text = dlgOpenFile.FileName;
            }
            else
            {
                string p = txtLocation.Text.Trim();
                if (!string.IsNullOrEmpty(p))
                    dlgSelectFolder.SelectedPath = p;
                if (dlgSelectFolder.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                txtLocation.Text = dlgSelectFolder.SelectedPath;
            }
            this.filename = txtLocation.Text;
            selectLogType();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!validate())
                return;

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void FormNewProject_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\x1B')
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        #endregion // Form Events Handlers

    }
}
