namespace OsbbRev2
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
            this.dlgOpen = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.stLab1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.stLab2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.panLogger = new System.Windows.Forms.Panel();
            this.lvLogger = new System.Windows.Forms.ListView();
            this.chTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chEvt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.labLog = new System.Windows.Forms.Label();
            this.panMain = new System.Windows.Forms.Panel();
            this.chkAddDetalization = new System.Windows.Forms.CheckBox();
            this.chkAppVisible = new System.Windows.Forms.CheckBox();
            this.btnAnalyze = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtFilename = new System.Windows.Forms.TextBox();
            this.labFilename = new System.Windows.Forms.Label();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.statusStrip1.SuspendLayout();
            this.panLogger.SuspendLayout();
            this.panMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // dlgOpen
            // 
            this.dlgOpen.DefaultExt = "xlsx";
            this.dlgOpen.FileName = "openFileDialog1";
            this.dlgOpen.Filter = "Excel Files|*.xlsx;*.xlsm;*.xls|All Files|*.*";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stLab1,
            this.stLab2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 296);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(680, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // stLab1
            // 
            this.stLab1.AutoSize = false;
            this.stLab1.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.stLab1.Name = "stLab1";
            this.stLab1.Size = new System.Drawing.Size(118, 17);
            this.stLab1.Text = "Ready.";
            this.stLab1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // stLab2
            // 
            this.stLab2.AutoSize = false;
            this.stLab2.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.stLab2.Name = "stLab2";
            this.stLab2.Size = new System.Drawing.Size(540, 17);
            this.stLab2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panLogger
            // 
            this.panLogger.Controls.Add(this.lvLogger);
            this.panLogger.Controls.Add(this.labLog);
            this.panLogger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panLogger.Location = new System.Drawing.Point(0, 106);
            this.panLogger.Name = "panLogger";
            this.panLogger.Size = new System.Drawing.Size(680, 190);
            this.panLogger.TabIndex = 8;
            // 
            // lvLogger
            // 
            this.lvLogger.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chTime,
            this.chEvt});
            this.lvLogger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvLogger.FullRowSelect = true;
            this.lvLogger.HideSelection = false;
            this.lvLogger.Location = new System.Drawing.Point(0, 17);
            this.lvLogger.MultiSelect = false;
            this.lvLogger.Name = "lvLogger";
            this.lvLogger.Size = new System.Drawing.Size(680, 173);
            this.lvLogger.TabIndex = 1;
            this.lvLogger.UseCompatibleStateImageBehavior = false;
            this.lvLogger.View = System.Windows.Forms.View.Details;
            // 
            // chTime
            // 
            this.chTime.Text = "Time";
            this.chTime.Width = 80;
            // 
            // chEvt
            // 
            this.chEvt.Text = "Event";
            this.chEvt.Width = 580;
            // 
            // labLog
            // 
            this.labLog.Dock = System.Windows.Forms.DockStyle.Top;
            this.labLog.Location = new System.Drawing.Point(0, 0);
            this.labLog.Name = "labLog";
            this.labLog.Size = new System.Drawing.Size(680, 17);
            this.labLog.TabIndex = 0;
            this.labLog.Text = "Log:";
            // 
            // panMain
            // 
            this.panMain.Controls.Add(this.chkAddDetalization);
            this.panMain.Controls.Add(this.chkAppVisible);
            this.panMain.Controls.Add(this.btnAnalyze);
            this.panMain.Controls.Add(this.btnOpen);
            this.panMain.Controls.Add(this.btnBrowse);
            this.panMain.Controls.Add(this.txtFilename);
            this.panMain.Controls.Add(this.labFilename);
            this.panMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.panMain.Location = new System.Drawing.Point(0, 0);
            this.panMain.Name = "panMain";
            this.panMain.Size = new System.Drawing.Size(680, 103);
            this.panMain.TabIndex = 9;
            // 
            // chkAddDetalization
            // 
            this.chkAddDetalization.AutoSize = true;
            this.chkAddDetalization.Location = new System.Drawing.Point(15, 80);
            this.chkAddDetalization.Name = "chkAddDetalization";
            this.chkAddDetalization.Size = new System.Drawing.Size(101, 17);
            this.chkAddDetalization.TabIndex = 13;
            this.chkAddDetalization.Text = "Add &detalization";
            this.chkAddDetalization.UseVisualStyleBackColor = true;
            // 
            // chkAppVisible
            // 
            this.chkAppVisible.AutoSize = true;
            this.chkAppVisible.Location = new System.Drawing.Point(15, 57);
            this.chkAppVisible.Name = "chkAppVisible";
            this.chkAppVisible.Size = new System.Drawing.Size(77, 17);
            this.chkAppVisible.TabIndex = 12;
            this.chkAppVisible.Text = "App &visible";
            this.chkAppVisible.UseVisualStyleBackColor = true;
            this.chkAppVisible.CheckedChanged += new System.EventHandler(this.chkAppVisible_CheckedChanged);
            // 
            // btnAnalyze
            // 
            this.btnAnalyze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAnalyze.Enabled = false;
            this.btnAnalyze.Location = new System.Drawing.Point(593, 53);
            this.btnAnalyze.Name = "btnAnalyze";
            this.btnAnalyze.Size = new System.Drawing.Size(75, 23);
            this.btnAnalyze.TabIndex = 11;
            this.btnAnalyze.Text = "&Analyze";
            this.btnAnalyze.UseVisualStyleBackColor = true;
            this.btnAnalyze.Click += new System.EventHandler(this.btnAnalyze_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpen.Location = new System.Drawing.Point(593, 24);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 10;
            this.btnOpen.Text = "&Open";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(517, 24);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 9;
            this.btnBrowse.Text = "Bro&wse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtFilename
            // 
            this.txtFilename.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilename.Location = new System.Drawing.Point(13, 26);
            this.txtFilename.Name = "txtFilename";
            this.txtFilename.Size = new System.Drawing.Size(498, 20);
            this.txtFilename.TabIndex = 8;
            // 
            // labFilename
            // 
            this.labFilename.AutoSize = true;
            this.labFilename.Location = new System.Drawing.Point(12, 9);
            this.labFilename.Name = "labFilename";
            this.labFilename.Size = new System.Drawing.Size(78, 13);
            this.labFilename.TabIndex = 7;
            this.labFilename.Text = "Execl Filename";
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 103);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(680, 3);
            this.splitter1.TabIndex = 10;
            this.splitter1.TabStop = false;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 318);
            this.Controls.Add(this.panLogger);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panMain);
            this.Controls.Add(this.statusStrip1);
            this.Name = "FormMain";
            this.Text = "OSBB Revision Extractor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormMain_FormClosed);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.Shown += new System.EventHandler(this.FormMain_Shown);
            this.ResizeEnd += new System.EventHandler(this.FormMain_ResizeEnd);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panLogger.ResumeLayout(false);
            this.panMain.ResumeLayout(false);
            this.panMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog dlgOpen;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel stLab1;
        private System.Windows.Forms.ToolStripStatusLabel stLab2;
        private System.Windows.Forms.Panel panLogger;
        private System.Windows.Forms.ListView lvLogger;
        private System.Windows.Forms.ColumnHeader chTime;
        private System.Windows.Forms.ColumnHeader chEvt;
        private System.Windows.Forms.Label labLog;
        private System.Windows.Forms.Panel panMain;
        private System.Windows.Forms.CheckBox chkAddDetalization;
        private System.Windows.Forms.CheckBox chkAppVisible;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtFilename;
        private System.Windows.Forms.Label labFilename;
        private System.Windows.Forms.Splitter splitter1;
    }
}

