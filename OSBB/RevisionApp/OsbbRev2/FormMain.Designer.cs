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
            this.labFilename = new System.Windows.Forms.Label();
            this.txtFilename = new System.Windows.Forms.TextBox();
            this.dlgOpen = new System.Windows.Forms.OpenFileDialog();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.stLab1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.stLab2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnAnalyze = new System.Windows.Forms.Button();
            this.chkAppVisible = new System.Windows.Forms.CheckBox();
            this.chkAddDetalization = new System.Windows.Forms.CheckBox();
            this.panLogger = new System.Windows.Forms.Panel();
            this.labLog = new System.Windows.Forms.Label();
            this.lvLogger = new System.Windows.Forms.ListView();
            this.chTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chEvt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.statusStrip1.SuspendLayout();
            this.panLogger.SuspendLayout();
            this.SuspendLayout();
            // 
            // labFilename
            // 
            this.labFilename.AutoSize = true;
            this.labFilename.Location = new System.Drawing.Point(12, 9);
            this.labFilename.Name = "labFilename";
            this.labFilename.Size = new System.Drawing.Size(78, 13);
            this.labFilename.TabIndex = 0;
            this.labFilename.Text = "Execl Filename";
            // 
            // txtFilename
            // 
            this.txtFilename.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilename.Location = new System.Drawing.Point(13, 26);
            this.txtFilename.Name = "txtFilename";
            this.txtFilename.Size = new System.Drawing.Size(498, 20);
            this.txtFilename.TabIndex = 1;
            // 
            // dlgOpen
            // 
            this.dlgOpen.DefaultExt = "xlsx";
            this.dlgOpen.FileName = "openFileDialog1";
            this.dlgOpen.Filter = "Excel Files|*.xlsx;*.xlsm;*.xls|All Files|*.*";
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(517, 24);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Bro&wse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpen.Location = new System.Drawing.Point(593, 24);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 3;
            this.btnOpen.Text = "&Open";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stLab1,
            this.stLab2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 259);
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
            // btnAnalyze
            // 
            this.btnAnalyze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAnalyze.Location = new System.Drawing.Point(593, 53);
            this.btnAnalyze.Name = "btnAnalyze";
            this.btnAnalyze.Size = new System.Drawing.Size(75, 23);
            this.btnAnalyze.TabIndex = 4;
            this.btnAnalyze.Text = "&Analyze";
            this.btnAnalyze.UseVisualStyleBackColor = true;
            this.btnAnalyze.Click += new System.EventHandler(this.btnAnalyze_Click);
            // 
            // chkAppVisible
            // 
            this.chkAppVisible.AutoSize = true;
            this.chkAppVisible.Location = new System.Drawing.Point(15, 57);
            this.chkAppVisible.Name = "chkAppVisible";
            this.chkAppVisible.Size = new System.Drawing.Size(77, 17);
            this.chkAppVisible.TabIndex = 5;
            this.chkAppVisible.Text = "App &visible";
            this.chkAppVisible.UseVisualStyleBackColor = true;
            this.chkAppVisible.CheckedChanged += new System.EventHandler(this.chkAppVisible_CheckedChanged);
            // 
            // chkAddDetalization
            // 
            this.chkAddDetalization.AutoSize = true;
            this.chkAddDetalization.Location = new System.Drawing.Point(15, 80);
            this.chkAddDetalization.Name = "chkAddDetalization";
            this.chkAddDetalization.Size = new System.Drawing.Size(101, 17);
            this.chkAddDetalization.TabIndex = 6;
            this.chkAddDetalization.Text = "Add &detalization";
            this.chkAddDetalization.UseVisualStyleBackColor = true;
            // 
            // panLogger
            // 
            this.panLogger.Controls.Add(this.lvLogger);
            this.panLogger.Controls.Add(this.labLog);
            this.panLogger.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panLogger.Location = new System.Drawing.Point(0, 125);
            this.panLogger.Name = "panLogger";
            this.panLogger.Size = new System.Drawing.Size(680, 134);
            this.panLogger.TabIndex = 8;
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
            this.lvLogger.Size = new System.Drawing.Size(680, 117);
            this.lvLogger.TabIndex = 1;
            this.lvLogger.UseCompatibleStateImageBehavior = false;
            this.lvLogger.View = System.Windows.Forms.View.Details;
            // 
            // chTime
            // 
            this.chTime.Text = "Time";
            this.chTime.Width = 70;
            // 
            // chEvt
            // 
            this.chEvt.Text = "Event";
            this.chEvt.Width = 580;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 281);
            this.Controls.Add(this.panLogger);
            this.Controls.Add(this.chkAddDetalization);
            this.Controls.Add(this.chkAppVisible);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnAnalyze);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtFilename);
            this.Controls.Add(this.labFilename);
            this.Name = "FormMain";
            this.Text = "OSBB Revision Extractor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormMain_FormClosed);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.Shown += new System.EventHandler(this.FormMain_Shown);
            this.ResizeEnd += new System.EventHandler(this.FormMain_ResizeEnd);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panLogger.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labFilename;
        private System.Windows.Forms.TextBox txtFilename;
        private System.Windows.Forms.OpenFileDialog dlgOpen;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel stLab1;
        private System.Windows.Forms.ToolStripStatusLabel stLab2;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.CheckBox chkAppVisible;
        private System.Windows.Forms.CheckBox chkAddDetalization;
        private System.Windows.Forms.Panel panLogger;
        private System.Windows.Forms.ListView lvLogger;
        private System.Windows.Forms.ColumnHeader chTime;
        private System.Windows.Forms.ColumnHeader chEvt;
        private System.Windows.Forms.Label labLog;
    }
}

