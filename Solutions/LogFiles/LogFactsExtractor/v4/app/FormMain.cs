/*
 * Log Facts Extractor: main UI.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-08-24
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using XService.Utils;
using XService.UI;
using XService.UI.CommonForms;

namespace LogFactExtractor4
{
    public partial class FormMain : Form
    {
        private const string HELP_LICENSE_TEXT =
            "Log Facts Extractor .NET \r\n" +
            "\r\n" +
            "Copyright (c) 2019 Dmitry Bondarenko, Kyiv, Ukraine (dima_ben@ukr.net) \r\n" +
            "Distributed under MIT License\r\n" +
            "";

        public FormMain()
        {
            InitializeComponent();
            this.uiContext = SynchronizationContext.Current; 
        }

        private class ProjectDescriptor
        {
            public int ID;
            public DataRow ProjectRow;
            public ParserProject Project = null;
            public DataTable Files = new DataTable();

            public override string ToString()
            {
                return String.Format("PrjDescr[#{0}; Prj={1}]", this.ID,
                    (this.Project != null ? this.Project.ToString() : "(null)")
                    );
            }
        }

        private class FileMarker
        {
            public ParserProjectFile FileRef;
            public int Marker;
        }

        private LogFactsExtractorEngine engine;
        private DataTable projectsList;
        private readonly SynchronizationContext uiContext;
        private Dictionary<int, ProjectDescriptor> projects = new Dictionary<int, ProjectDescriptor>();

        private void progressor(EProgressAction pAction, object pValue, string pMsg)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ProgressMethod(this.progressor), pAction, pValue, pMsg);
                Thread.Sleep(3);
            }
            else
            {
                lock (stProgress)
                {
                    switch (pAction)
                    {
                        case EProgressAction.Setup:
                            stProgress.Visible = true;
                            stProgress.Enabled = true;
                            stProgress.Value = 0;
                            if (pValue is int)
                            {
                                int value = (int)pValue;
                                stProgress.Maximum = value;
                            }
                            if (!string.IsNullOrEmpty(pMsg))
                                stLab1.Text = pMsg;
                            break;

                        case EProgressAction.Step:
                            {
                                if (pValue is int)
                                {
                                    int value = (int)pValue;
                                    int x = stProgress.Value + value;
                                    if (x <= stProgress.Maximum)
                                        stProgress.Value += value;
                                    else
                                        stProgress.Value = stProgress.Maximum;
                                    if (!string.IsNullOrEmpty(pMsg))
                                        stLab1.Text = pMsg;
                                }
                            }
                            break;

                        case EProgressAction.Completed:
                            {
                                stProgress.Value = 0;
                                stProgress.Maximum = 100;
                                if (!string.IsNullOrEmpty(pMsg))
                                    stLab1.Text = pMsg;
                                if (pValue is ParserProjectFile)
                                {
                                    uiContext.Post(this.setFileMarker, new FileMarker() { FileRef = (ParserProjectFile)pValue, Marker = 1 });
                                }
                            }
                            break;

                        case EProgressAction.Error:
                            if (!string.IsNullOrEmpty(pMsg))
                                stLab1.Text = pMsg;
                            break;
                    }
                }
                //this.Refresh();
                statusStrip1.Refresh();
                statusCtrlUpdate();
                Thread.Sleep(3);
            }            
        }

        private ProjectDescriptor CurrentProject
        {
            get 
            {
                ProjectDescriptor descr = (ProjectDescriptor)(CurrentProjectItem != null ? CurrentProjectItem.Tag : null);
                if (descr != null && descr.Project == null)
                    getProjectFiles((int)descr.ProjectRow["projectId"]);                
                return descr;  
            }
        }

        private ListViewItem CurrentProjectItem
        {
            get { return (lvProjects.SelectedItems.Count > 0 ? lvProjects.SelectedItems[0] : null); }
        }

        private ListViewItem CurrentLogFileItem
        {
            get { return (lvFiles.SelectedItems.Count > 0 ? lvFiles.SelectedItems[0] : null); }
        }

        private void clearProjectDecriptors()
        {
            foreach (KeyValuePair<int, ProjectDescriptor> it in this.projects)
                it.Value.Files.Dispose();
            this.projects.Clear();
        }

        private void setFileMarker(object obj)
        {
            FileMarker ctx = (FileMarker)obj;
            foreach (ListViewItem li in lvFiles.Items)
            {
                if (ctx.FileRef.Equals(li.Tag))
                {
                    populateProfileFile(li, ctx.FileRef);
                    /*
                    li.ImageIndex = getLogStateImage(ctx.FileRef);
                    li.SubItems[3].Text = StrUtils.NskTimestampOf(ctx.FileRef.Parsed).Substring(0, 19);
                    */
                    li.Focused = true;
                    li.Selected = true;
                    li.EnsureVisible();
                    return;
                }
            }
            Trace.WriteLine(string.Format("!setFileMarker.ERR: cannot find ListViewItem related to {0}", ctx.FileRef));
        }

        private ListViewItem populateProfileFile(ListViewItem pItem, ParserProjectFile f)
        {
            int id = f.LogId;
            string[] fields = new string[] { 
                id.ToString(), f.RelativePath, f.FileSize.ToString("N0"), 
                StrUtils.NskTimestampOf(f.Parsed).Substring(0, 19), 
                f.LogType.ToString(),
                f.ParseTime.ToString("N2"),
                f.FactsCount.ToString(),
                f.ErrorsCount.ToString(),
                f.LastError 
                };
            if (pItem == null)
            {
                pItem = new ListViewItem(fields) { Tag = f, Name = string.Format("{0}-{1}", f.Project.ProjectId, f.LogId) };
                pItem.ImageIndex = getLogStateImage(f);
            }
            else
            {
                for (int i=0; i<fields.Length; i++)
                    pItem.SubItems[i].Text = fields[i];
                pItem.ImageIndex = getLogStateImage(f);
            }
            return pItem;
        }

        private void populateProjects(ParserProject pSelectedProject)
        {
            clearProjectDecriptors();

            ListViewItem toSelect = null;
            lvProjects.BeginUpdate();
            try
            {
                lvProjects.Items.Clear();
                                
                foreach (DataRow row in this.projectsList.Rows)
                {
                    int projId = (int)row["projectId"];
                    
                    ListViewItem li = new ListViewItem(new string[] { 
                        projId.ToString(), row["location"].ToString(), row["defaultLogType"].ToString() 
                        });
                    lvProjects.Items.Add(li);

                    ProjectDescriptor descr = new ProjectDescriptor() { ProjectRow = row };
                    descr.ID = projId;
                    this.projects[projId] = descr;
                    li.Tag = descr;

                    if (pSelectedProject != null && pSelectedProject.ProjectId == projId)
                        toSelect = li;
                }
            }
            finally
            {
                lvProjects.EndUpdate();

                if (toSelect != null)
                    toSelect.EnsureVisible();
            }
            statusCtrlUpdate();
        }

        private void populateProjectFiles(ProjectDescriptor pProjDescr)
        {
            ListViewItem toSelect = null;
            lvFiles.BeginUpdate();
            try
            {
                lvFiles.Items.Clear();

                string root = pProjDescr.Project.Location.ToLower();
                foreach (ParserProjectFile f in pProjDescr.Project.LogFiles)
                {
                    int id = f.LogId;
                    string fn = f.RelativePath;
                    ListViewItem li = populateProfileFile(null, f);
                    lvFiles.Items.Add(li);
                }
            }
            finally
            {
                lvFiles.EndUpdate();

                if (toSelect != null)
                    toSelect.EnsureVisible();
            }
            statusCtrlUpdate();
        }

        private int getLogStateImage(ParserProjectFile f)
        {
            int imgIdx = 0;
            if (!DbUtils.IsNullTs(f.Parsed))
            {
                if (f.FactsCount > 0)
                    imgIdx = (f.ErrorsCount > 0 ? 2 : 1);
                if (!string.IsNullOrEmpty(f.LastError))
                    imgIdx = 3;
            }
            else if (!string.IsNullOrEmpty(f.LastError))
                imgIdx = 3;
            return imgIdx;
        }

        private void populatePluginsList()
        {
            ToolStripDropDownMenu menu = (ToolStripDropDownMenu)tbPlugin.DropDown;
            menu.Items.Clear();

            foreach (PluginDescriptor p in this.engine.Plugins)
            {
                ToolStripMenuItem mi = (ToolStripMenuItem)menu.Items.Add(p.Name);
                mi.Tag = p;
                mi.Click += this.miPlugin_Click;
            }
        }

        private ProjectDescriptor getProjectFiles(int pProjectId)
        {
            ProjectDescriptor descr;
            if (!this.projects.TryGetValue(pProjectId, out descr))
            {
                descr = new ProjectDescriptor();
                this.projects[pProjectId] = descr;
            }            

            if (descr.Project == null)
            {
                DataRow dr = (DataRow)descr.ProjectRow;
                LogTypeConfig lt = this.engine.Config.FindLogType(dr["defaultLogType"].ToString());
                descr.Project = new ParserProject(this.engine, lt, dr["location"].ToString()) 
                    { ProjectId = (int)dr["projectId"] };
                descr.Project.LoadFiles();
            }

            return descr;
        }

        private void displaySelectedProjectInfo(ListViewItem pItem)
        {
            txtProjInfo.Text = "";
            if (pItem == null) return ;

            ProjectDescriptor descr = (ProjectDescriptor)pItem.Tag;
            DataRow dr = descr.ProjectRow;

            UiTools.RenderInto(txtProjInfo, string.Format(
                "Id:<b color='navy'>{0}</b>; Default Log Type:<b color='navy'>{1}</b><br/>" +
                "Location:<b color='navy'>{2}</b>; <br/>" +
                "Comment:<b color='navy'>{3}</b>; <br/>" +
                "Created:<b color='navy'>{4}</b>; <br/>" +
                "",
                dr["projectId"].ToString(), dr["defaultLogType"].ToString(),
                dr["location"].ToString(), dr["comment"].ToString(),
                dr["created"].ToString().Substring(0, 19)
                ));

            if (descr.Project == null)
            {
                int projId = (int)dr["projectId"];
                getProjectFiles(projId);
            }

            populateProjectFiles(descr);
        }

        private delegate void statusCtrlUpdateMethod();
        private void statusCtrlUpdate()
        {            
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new statusCtrlUpdateMethod(this.statusCtrlUpdate));
            }
            else
            {
                int apc;
                lock (this.engine.SyncRoot)
                    apc = this.engine.ActiveParserChannels;

                mmiNewProject.Enabled = (apc == 0);
                tbNewProject.Enabled = mmiNewProject.Enabled;
                
                mmiRefresh.Enabled = (apc == 0);
                tbRefresh.Enabled = mmiRefresh.Enabled;

                mmiDeleteSelected.Enabled = (apc == 0);
                tbDelete.Enabled = mmiDeleteSelected.Enabled;

                mmiParseAllFiles.Enabled = (apc == 0 && this.CurrentProject != null);
                tbParseAll.Enabled = mmiParseAllFiles.Enabled;

                mmiParseFile.Enabled = (apc == 0 && this.CurrentProject != null && lvFiles.Items.Count > 0 && lvFiles.SelectedItems.Count > 0);
                tbParseOne.Enabled = mmiParseFile.Enabled;

                tbPlugin.Enabled = (apc == 0 && this.CurrentProject != null);
            }
        }

        private void parseLogFile(ParserProjectFile pItem)
        {            
            try 
            {
                stLab1.Text = "Preparing to parse...";
                statusStrip1.Refresh();
                int idx = lvFiles.Items.IndexOfKey(string.Format("{0}-{1}", pItem.Project.ProjectId, pItem.LogId));
                if (idx >= 0)
                {
                    lvFiles.Items[idx].ImageIndex = 0;
                    lvFiles.Refresh();
                }

                Task.Run(() => pItem.Parse());
                Thread.Sleep(5);
                statusCtrlUpdate();
            }
            finally { statusCtrlUpdate(); }
        }

        private void deleteItem(ListViewItem pItem)
        {
            object obj = pItem.Tag;
            if (obj is ProjectDescriptor)
            {
                ProjectDescriptor item = (ProjectDescriptor)obj;
                DialogResult dr = MessageBox.Show(string.Format("Are you sure you want to delete project# {0} with all content?", item.ID),
                    "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dr != System.Windows.Forms.DialogResult.OK) return ;
                item.Project.DeleteFromDb();
            }
            else if (obj is ParserProjectFile)
            {
                ParserProjectFile item = (ParserProjectFile)obj;
                DialogResult dr = MessageBox.Show(string.Format("Are you sure you want to delete log file# {0} from project# {1}?", item.LogId, item.Project.ProjectId),
                    "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dr != System.Windows.Forms.DialogResult.OK) return;
                item.DeleteFromDb();
            }
            pItem.Remove();

            lvFiles.Items.Clear();
            this.projectsList.Clear();
            ParserProject.ListOfProjects(this.engine, this.projectsList);
            populateProjects(null);            
        }

        private void displayUsefulLinks()
        {
            Dictionary<string, string> links = (Dictionary<string, string>)ConfigurationManager.GetSection("UsefulLinks");
            foreach (KeyValuePair<string, string> link in links)
            {
                ToolStripItem mi = mmiUsefulLinks.DropDownItems.Add(link.Key);
                mi.Tag = link.Value;
                mi.Click += this.miUsefulLink_Click;
            }
            mmiUsefulLinks.Enabled = (mmiUsefulLinks.DropDownItems.Count > 0);
        }

        #region Form Event Handlers

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.engine = new LogFactsExtractorEngine();
            this.engine.Progressor += this.progressor;
            this.engine.LoadConfig();

            displayUsefulLinks();

            this.projectsList = new DataTable();
            ParserProject.ListOfProjects(this.engine, this.projectsList);
            populateProjects(null);

            populatePluginsList();
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {

        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            stLab1.Width = statusStrip1.ClientRectangle.Width - stProgress.Width - 32;
        }

        private void lvProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            displaySelectedProjectInfo(this.CurrentProjectItem);
            statusCtrlUpdate();
        }

        private void lvFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            statusCtrlUpdate();
        }

        private void miPlugin_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = (ToolStripMenuItem)sender;
            PluginDescriptor p = (PluginDescriptor)mi.Tag;

            ListViewItem li = null;
            if (p.PerFile)
            {
                li = this.CurrentLogFileItem;
                if (li == null) return;
                ParserProjectFile f = (ParserProjectFile)li.Tag;
                if (!f.LogType.Equals(p.LogTypeRef))
                {
                    MessageBox.Show(string.Format("Plugin[{0}] is only for LogType[{1}]!", p.Name, p.LogTypeName), "Error");
                    return;
                }

                DbProviderFactory factory;
                DbConnection db = this.engine.GetLocalDbConnection(EDbConnectionType.Reader, f.LogType, out factory);
                p.CustomParams["MainForm"] = this;
                p.CustomParams["ProjectFile"] = f;
                p.CustomParams["LogFilename"] = f.FileName;
                p.Activate(db, factory, f.LogType.TableName, f.Project.ProjectId, f.LogId);
            }
            else
            {
                li = this.CurrentProjectItem;
                if (li == null) return;
                ProjectDescriptor descr = (ProjectDescriptor)li.Tag;
                ParserProject prj = descr.Project;

                DbProviderFactory factory;
                DbConnection db = this.engine.GetLocalDbConnection(EDbConnectionType.Reader, prj.DefaultLogType, out factory);
                p.CustomParams["MainForm"] = this;
                p.CustomParams["Project"] = prj;
                p.CustomParams["LogFilename"] = descr.Project.Location;
                p.Activate(db, factory, prj.DefaultLogType.TableName, prj.ProjectId, -1);
            }
        }

        private void miUsefulLink_Click(object sender, EventArgs e)
        {
            ToolStripItem mi = (ToolStripItem)sender;
            ProcessStartInfo st = new ProcessStartInfo();
            st.UseShellExecute = true;
            st.Verb = "open";
            st.FileName = mi.Tag.ToString();
            Process.Start(st);
        }

        private void mmiNewProject_Click(object sender, EventArgs e)
        {
            ParserProject proj;
            if (FormNewProject.Execute(this, this.engine, out proj))
            {
                this.projectsList.Clear();
                ParserProject.ListOfProjects(this.engine, this.projectsList);
                populateProjects(proj);
            }
            statusCtrlUpdate();
        }

        private void mmiOpenSQLiteDb_Click(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = DbUtils.LocalDbFilename;
            psi.Verb = "open";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        private void mmiRefresh_Click(object sender, EventArgs e)
        {
            lvFiles.Items.Clear();
            this.projectsList.Clear();
            ParserProject.ListOfProjects(this.engine, this.projectsList);
            populateProjects(null);
        }

        private void mmiParseFile_Click(object sender, EventArgs e)
        {
            if (this.CurrentProjectItem == null) return;
            if (this.CurrentLogFileItem == null) return;

            this.engine.ReleaseSemaphore(-1);
            parseLogFile((ParserProjectFile)this.CurrentLogFileItem.Tag);
        }

        private void mmiParseAllFiles_Click(object sender, EventArgs e)
        {
            if (this.CurrentProjectItem == null) return;

            foreach (ListViewItem li in lvFiles.Items)
                li.ImageIndex = 0;
            lvFiles.Refresh();

            this.engine.ReleaseSemaphore(-1);
            foreach (ParserProjectFile f in this.CurrentProject.Project.LogFiles)
            {
                parseLogFile(f);
            }
        }

        private void mmiDeleteSelected_Click(object sender, EventArgs e)
        {
            if (lvProjects.Focused)
                deleteItem(this.CurrentProjectItem);
            else if (lvFiles.Focused)
                deleteItem(this.CurrentLogFileItem);
        }

        private void mmiAbout_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            TypeUtils.CollectVersionInfoAttributes(props, Assembly.GetEntryAssembly());
            props["ApplicationName"] = "Log Facts Extractor (V4)";
            props["EOL"] = Environment.NewLine;
            //props["url"] = "https://dmitrybond.wordpress.com/2012/10/20/three-queries/";

            Assembly asm = Assembly.GetExecutingAssembly();
            props["HostInfo"] = CommonUtils.HostInfoStamp() + string.Format(" ProcessType:{0};", asm.GetName().ProcessorArchitecture);

            string info = ""
                + "$(ApplicationName).$(EOL)"
                + "Version $(Version) / $(FileVersion)$(EOL)"
                + "Written by Dmitry Bond. (dima_ben@ukr.net)$(EOL)"
                + "$(EOL)"
                + "$(HostInfo)$(EOL)"
                + "$(EOL)"
                //+ "$(url)$(EOL)"
                + "";
            info = StrUtils.ExpandParameters(info, props, true);
            FormAbout.Execute(this, StrUtils.ExpandParameters("About $(ApplicationName)", props, true),
                info, HELP_LICENSE_TEXT);
        }

        #endregion // Form Event Handlers

    }
}
