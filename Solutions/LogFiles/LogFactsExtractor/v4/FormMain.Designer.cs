namespace LogFactExtractor4
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mmiNewProject = new System.Windows.Forms.ToolStripMenuItem();
            this.mmiDeleteSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.mmiOpenSQLiteDb = new System.Windows.Forms.ToolStripMenuItem();
            this.mmiRefresh = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuProject = new System.Windows.Forms.ToolStripMenuItem();
            this.mmiParseFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mmiParseAllFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mmiUsefulLinks = new System.Windows.Forms.ToolStripMenuItem();
            this.mmiAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tbOpenDb = new System.Windows.Forms.ToolStripButton();
            this.tbRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tbNewProject = new System.Windows.Forms.ToolStripButton();
            this.tbDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tbParseOne = new System.Windows.Forms.ToolStripButton();
            this.tbParseAll = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tbPlugin = new System.Windows.Forms.ToolStripDropDownButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.stProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.stLab1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.panLeft = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.txtProjInfo = new System.Windows.Forms.RichTextBox();
            this.lvProjects = new System.Windows.Forms.ListView();
            this.chPrjId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chPrjLocation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chPrjLogType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.panClient = new System.Windows.Forms.Panel();
            this.lvFiles = new System.Windows.Forms.ListView();
            this.chLogId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogFilename = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogParsed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogParseTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogFacts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogErrors = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLogLastError = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imglProjFiles = new System.Windows.Forms.ImageList(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panLeft.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panClient.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuProject,
            this.mnuHelp});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1008, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mmiNewProject,
            this.mmiDeleteSelected,
            this.toolStripMenuItem1,
            this.mmiOpenSQLiteDb,
            this.mmiRefresh});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(37, 20);
            this.mnuFile.Text = "&File";
            // 
            // mmiNewProject
            // 
            this.mmiNewProject.Image = global::LogFactExtractor4.Properties.Resources.new1;
            this.mmiNewProject.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.mmiNewProject.Name = "mmiNewProject";
            this.mmiNewProject.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.mmiNewProject.Size = new System.Drawing.Size(266, 22);
            this.mmiNewProject.Text = "New Project...";
            this.mmiNewProject.Click += new System.EventHandler(this.mmiNewProject_Click);
            // 
            // mmiDeleteSelected
            // 
            this.mmiDeleteSelected.Image = global::LogFactExtractor4.Properties.Resources.delete_explorer1a;
            this.mmiDeleteSelected.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.mmiDeleteSelected.Name = "mmiDeleteSelected";
            this.mmiDeleteSelected.Size = new System.Drawing.Size(266, 22);
            this.mmiDeleteSelected.Text = "&Delete selected...";
            this.mmiDeleteSelected.Click += new System.EventHandler(this.mmiDeleteSelected_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(263, 6);
            // 
            // mmiOpenSQLiteDb
            // 
            this.mmiOpenSQLiteDb.Image = global::LogFactExtractor4.Properties.Resources.icon_db_sm;
            this.mmiOpenSQLiteDb.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.mmiOpenSQLiteDb.Name = "mmiOpenSQLiteDb";
            this.mmiOpenSQLiteDb.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
            this.mmiOpenSQLiteDb.Size = new System.Drawing.Size(266, 22);
            this.mmiOpenSQLiteDb.Text = "Open SQLite Database";
            this.mmiOpenSQLiteDb.Click += new System.EventHandler(this.mmiOpenSQLiteDb_Click);
            // 
            // mmiRefresh
            // 
            this.mmiRefresh.Image = global::LogFactExtractor4.Properties.Resources.refresh1;
            this.mmiRefresh.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.mmiRefresh.Name = "mmiRefresh";
            this.mmiRefresh.Size = new System.Drawing.Size(266, 22);
            this.mmiRefresh.Text = "&Refresh";
            this.mmiRefresh.Click += new System.EventHandler(this.mmiRefresh_Click);
            // 
            // mnuProject
            // 
            this.mnuProject.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mmiParseFile,
            this.mmiParseAllFiles});
            this.mnuProject.Name = "mnuProject";
            this.mnuProject.Size = new System.Drawing.Size(56, 20);
            this.mnuProject.Text = "&Project";
            // 
            // mmiParseFile
            // 
            this.mmiParseFile.Image = global::LogFactExtractor4.Properties.Resources.compile1;
            this.mmiParseFile.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.mmiParseFile.Name = "mmiParseFile";
            this.mmiParseFile.ShortcutKeys = System.Windows.Forms.Keys.F9;
            this.mmiParseFile.Size = new System.Drawing.Size(223, 22);
            this.mmiParseFile.Text = "Parse File";
            this.mmiParseFile.Click += new System.EventHandler(this.mmiParseFile_Click);
            // 
            // mmiParseAllFiles
            // 
            this.mmiParseAllFiles.Image = ((System.Drawing.Image)(resources.GetObject("mmiParseAllFiles.Image")));
            this.mmiParseAllFiles.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.mmiParseAllFiles.Name = "mmiParseAllFiles";
            this.mmiParseAllFiles.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F9)));
            this.mmiParseAllFiles.Size = new System.Drawing.Size(223, 22);
            this.mmiParseAllFiles.Text = "Parse All Files";
            this.mmiParseAllFiles.Click += new System.EventHandler(this.mmiParseAllFiles_Click);
            // 
            // mnuHelp
            // 
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mmiUsefulLinks,
            this.mmiAbout});
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Size = new System.Drawing.Size(44, 20);
            this.mnuHelp.Text = "&Help";
            // 
            // mmiUsefulLinks
            // 
            this.mmiUsefulLinks.Name = "mmiUsefulLinks";
            this.mmiUsefulLinks.Size = new System.Drawing.Size(137, 22);
            this.mmiUsefulLinks.Text = "Useful Links";
            // 
            // mmiAbout
            // 
            this.mmiAbout.Name = "mmiAbout";
            this.mmiAbout.Size = new System.Drawing.Size(137, 22);
            this.mmiAbout.Text = "&About...";
            this.mmiAbout.Click += new System.EventHandler(this.mmiAbout_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbOpenDb,
            this.tbRefresh,
            this.toolStripSeparator1,
            this.tbNewProject,
            this.tbDelete,
            this.toolStripSeparator2,
            this.tbParseOne,
            this.tbParseAll,
            this.toolStripSeparator3,
            this.tbPlugin});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1008, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tbOpenDb
            // 
            this.tbOpenDb.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbOpenDb.Image = ((System.Drawing.Image)(resources.GetObject("tbOpenDb.Image")));
            this.tbOpenDb.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbOpenDb.Name = "tbOpenDb";
            this.tbOpenDb.Size = new System.Drawing.Size(23, 22);
            this.tbOpenDb.Text = "Open local db";
            this.tbOpenDb.Click += new System.EventHandler(this.mmiOpenSQLiteDb_Click);
            // 
            // tbRefresh
            // 
            this.tbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbRefresh.Image = ((System.Drawing.Image)(resources.GetObject("tbRefresh.Image")));
            this.tbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbRefresh.Name = "tbRefresh";
            this.tbRefresh.Size = new System.Drawing.Size(23, 22);
            this.tbRefresh.Text = "Refresh";
            this.tbRefresh.Click += new System.EventHandler(this.mmiRefresh_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tbNewProject
            // 
            this.tbNewProject.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbNewProject.Image = ((System.Drawing.Image)(resources.GetObject("tbNewProject.Image")));
            this.tbNewProject.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbNewProject.Name = "tbNewProject";
            this.tbNewProject.Size = new System.Drawing.Size(23, 22);
            this.tbNewProject.Text = "New project";
            this.tbNewProject.Click += new System.EventHandler(this.mmiNewProject_Click);
            // 
            // tbDelete
            // 
            this.tbDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbDelete.Image = ((System.Drawing.Image)(resources.GetObject("tbDelete.Image")));
            this.tbDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbDelete.Name = "tbDelete";
            this.tbDelete.Size = new System.Drawing.Size(23, 22);
            this.tbDelete.Text = "Delete selected item";
            this.tbDelete.Click += new System.EventHandler(this.mmiDeleteSelected_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // tbParseOne
            // 
            this.tbParseOne.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbParseOne.Image = ((System.Drawing.Image)(resources.GetObject("tbParseOne.Image")));
            this.tbParseOne.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbParseOne.Name = "tbParseOne";
            this.tbParseOne.Size = new System.Drawing.Size(23, 22);
            this.tbParseOne.Text = "Parse selected file";
            this.tbParseOne.Click += new System.EventHandler(this.mmiParseFile_Click);
            // 
            // tbParseAll
            // 
            this.tbParseAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbParseAll.Image = ((System.Drawing.Image)(resources.GetObject("tbParseAll.Image")));
            this.tbParseAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbParseAll.Name = "tbParseAll";
            this.tbParseAll.Size = new System.Drawing.Size(23, 22);
            this.tbParseAll.Text = "Parse all files";
            this.tbParseAll.Click += new System.EventHandler(this.mmiParseAllFiles_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // tbPlugin
            // 
            this.tbPlugin.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbPlugin.Image = ((System.Drawing.Image)(resources.GetObject("tbPlugin.Image")));
            this.tbPlugin.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbPlugin.Name = "tbPlugin";
            this.tbPlugin.Size = new System.Drawing.Size(29, 22);
            this.tbPlugin.Text = "Activate visualization plugins...";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stProgress,
            this.stLab1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 479);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1008, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // stProgress
            // 
            this.stProgress.Name = "stProgress";
            this.stProgress.Size = new System.Drawing.Size(160, 16);
            // 
            // stLab1
            // 
            this.stLab1.AutoSize = false;
            this.stLab1.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.stLab1.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.stLab1.Name = "stLab1";
            this.stLab1.Size = new System.Drawing.Size(520, 17);
            this.stLab1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panLeft
            // 
            this.panLeft.Controls.Add(this.panel1);
            this.panLeft.Controls.Add(this.lvProjects);
            this.panLeft.Controls.Add(this.label1);
            this.panLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panLeft.Location = new System.Drawing.Point(0, 49);
            this.panLeft.Name = "panLeft";
            this.panLeft.Size = new System.Drawing.Size(306, 430);
            this.panLeft.TabIndex = 6;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.txtProjInfo);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 330);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(306, 100);
            this.panel1.TabIndex = 7;
            // 
            // txtProjInfo
            // 
            this.txtProjInfo.BackColor = System.Drawing.SystemColors.Info;
            this.txtProjInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtProjInfo.Location = new System.Drawing.Point(0, 0);
            this.txtProjInfo.Name = "txtProjInfo";
            this.txtProjInfo.ReadOnly = true;
            this.txtProjInfo.Size = new System.Drawing.Size(306, 100);
            this.txtProjInfo.TabIndex = 0;
            this.txtProjInfo.Text = "";
            // 
            // lvProjects
            // 
            this.lvProjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chPrjId,
            this.chPrjLocation,
            this.chPrjLogType});
            this.lvProjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvProjects.FullRowSelect = true;
            this.lvProjects.HideSelection = false;
            this.lvProjects.Location = new System.Drawing.Point(0, 13);
            this.lvProjects.MultiSelect = false;
            this.lvProjects.Name = "lvProjects";
            this.lvProjects.Size = new System.Drawing.Size(306, 417);
            this.lvProjects.TabIndex = 6;
            this.lvProjects.UseCompatibleStateImageBehavior = false;
            this.lvProjects.View = System.Windows.Forms.View.Details;
            this.lvProjects.SelectedIndexChanged += new System.EventHandler(this.lvProjects_SelectedIndexChanged);
            // 
            // chPrjId
            // 
            this.chPrjId.Text = "Project Id";
            // 
            // chPrjLocation
            // 
            this.chPrjLocation.Text = "Location";
            this.chPrjLocation.Width = 160;
            // 
            // chPrjLogType
            // 
            this.chPrjLogType.Text = "Default Log Type";
            this.chPrjLogType.Width = 90;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Projects:";
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(306, 49);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 430);
            this.splitter1.TabIndex = 7;
            this.splitter1.TabStop = false;
            // 
            // panClient
            // 
            this.panClient.Controls.Add(this.lvFiles);
            this.panClient.Controls.Add(this.label2);
            this.panClient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panClient.Location = new System.Drawing.Point(309, 49);
            this.panClient.Name = "panClient";
            this.panClient.Size = new System.Drawing.Size(699, 430);
            this.panClient.TabIndex = 8;
            // 
            // lvFiles
            // 
            this.lvFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chLogId,
            this.chLogFilename,
            this.chLogSize,
            this.chLogParsed,
            this.chLogType,
            this.chLogParseTime,
            this.chLogFacts,
            this.chLogErrors,
            this.chLogLastError});
            this.lvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvFiles.FullRowSelect = true;
            this.lvFiles.HideSelection = false;
            this.lvFiles.Location = new System.Drawing.Point(0, 13);
            this.lvFiles.MultiSelect = false;
            this.lvFiles.Name = "lvFiles";
            this.lvFiles.Size = new System.Drawing.Size(699, 417);
            this.lvFiles.SmallImageList = this.imglProjFiles;
            this.lvFiles.TabIndex = 6;
            this.lvFiles.UseCompatibleStateImageBehavior = false;
            this.lvFiles.View = System.Windows.Forms.View.Details;
            this.lvFiles.SelectedIndexChanged += new System.EventHandler(this.lvFiles_SelectedIndexChanged);
            // 
            // chLogId
            // 
            this.chLogId.Text = "Id";
            this.chLogId.Width = 40;
            // 
            // chLogFilename
            // 
            this.chLogFilename.Text = "Filename";
            this.chLogFilename.Width = 160;
            // 
            // chLogSize
            // 
            this.chLogSize.Text = "Size";
            this.chLogSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.chLogSize.Width = 80;
            // 
            // chLogParsed
            // 
            this.chLogParsed.Text = "Parsed";
            this.chLogParsed.Width = 120;
            // 
            // chLogType
            // 
            this.chLogType.Text = "Log Type";
            this.chLogType.Width = 120;
            // 
            // chLogParseTime
            // 
            this.chLogParseTime.Text = "Parse Time(sec)";
            // 
            // chLogFacts
            // 
            this.chLogFacts.Text = "Facts";
            // 
            // chLogErrors
            // 
            this.chLogErrors.Text = "Errors";
            // 
            // chLogLastError
            // 
            this.chLogLastError.Text = "Last Error";
            this.chLogLastError.Width = 160;
            // 
            // imglProjFiles
            // 
            this.imglProjFiles.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imglProjFiles.ImageStream")));
            this.imglProjFiles.TransparentColor = System.Drawing.Color.Fuchsia;
            this.imglProjFiles.Images.SetKeyName(0, "circle-gray.bmp");
            this.imglProjFiles.Images.SetKeyName(1, "galka.bmp");
            this.imglProjFiles.Images.SetKeyName(2, "exclamation-16x16.bmp");
            this.imglProjFiles.Images.SetKeyName(3, "stop-icon-16x16.bmp");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Log Files";
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "compile.bmp");
            this.imageList1.Images.SetKeyName(1, "compile-all.bmp");
            this.imageList1.Images.SetKeyName(2, "icon-db-sm.png");
            this.imageList1.Images.SetKeyName(3, "new1.bmp");
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 501);
            this.Controls.Add(this.panClient);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panLeft);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.Text = "Log Facts Extractor V4";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.Shown += new System.EventHandler(this.FormMain_Shown);
            this.Resize += new System.EventHandler(this.FormMain_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panLeft.ResumeLayout(false);
            this.panLeft.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panClient.ResumeLayout(false);
            this.panClient.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mmiNewProject;
        private System.Windows.Forms.ToolStripButton tbNewProject;
        private System.Windows.Forms.Panel panLeft;
        private System.Windows.Forms.ListView lvProjects;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Panel panClient;
        private System.Windows.Forms.ListView lvFiles;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ColumnHeader chPrjId;
        private System.Windows.Forms.ColumnHeader chPrjLocation;
        private System.Windows.Forms.ColumnHeader chPrjLogType;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RichTextBox txtProjInfo;
        private System.Windows.Forms.ColumnHeader chLogId;
        private System.Windows.Forms.ColumnHeader chLogFilename;
        private System.Windows.Forms.ColumnHeader chLogSize;
        private System.Windows.Forms.ColumnHeader chLogParsed;
        private System.Windows.Forms.ColumnHeader chLogType;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tbOpenDb;
        private System.Windows.Forms.ToolStripMenuItem mmiOpenSQLiteDb;
        private System.Windows.Forms.ToolStripButton tbParseOne;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripProgressBar stProgress;
        private System.Windows.Forms.ToolStripStatusLabel stLab1;
        private System.Windows.Forms.ToolStripButton tbParseAll;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ToolStripMenuItem mnuProject;
        private System.Windows.Forms.ToolStripMenuItem mmiParseFile;
        private System.Windows.Forms.ToolStripMenuItem mmiParseAllFiles;
        private System.Windows.Forms.ToolStripButton tbDelete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem mmiDeleteSelected;
        private System.Windows.Forms.ImageList imglProjFiles;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mmiAbout;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripDropDownButton tbPlugin;
        private System.Windows.Forms.ColumnHeader chLogParseTime;
        private System.Windows.Forms.ColumnHeader chLogFacts;
        private System.Windows.Forms.ColumnHeader chLogErrors;
        private System.Windows.Forms.ColumnHeader chLogLastError;
        private System.Windows.Forms.ToolStripButton tbRefresh;
        private System.Windows.Forms.ToolStripMenuItem mmiRefresh;
        private System.Windows.Forms.ToolStripMenuItem mmiUsefulLinks;
    }
}

