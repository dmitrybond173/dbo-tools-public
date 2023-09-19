namespace ReestrParser
{
    partial class FormAppUI
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
            this.txtLog = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.stLab1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.stLab2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.btnParse = new System.Windows.Forms.Button();
            this.chkDb = new System.Windows.Forms.CheckBox();
            this.chkExcel = new System.Windows.Forms.CheckBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtFilespecs = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dlgBrowseForDir = new System.Windows.Forms.FolderBrowserDialog();
            this.statusStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.SystemColors.Info;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.txtLog.Location = new System.Drawing.Point(0, 127);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(555, 160);
            this.txtLog.TabIndex = 6;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stLab1,
            this.stLab2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 287);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(555, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // stLab1
            // 
            this.stLab1.AutoSize = false;
            this.stLab1.Name = "stLab1";
            this.stLab1.Size = new System.Drawing.Size(118, 17);
            this.stLab1.Text = "Ready.";
            this.stLab1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // stLab2
            // 
            this.stLab2.AutoSize = false;
            this.stLab2.Name = "stLab2";
            this.stLab2.Size = new System.Drawing.Size(400, 17);
            this.stLab2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.btnParse);
            this.panel1.Controls.Add(this.chkDb);
            this.panel1.Controls.Add(this.chkExcel);
            this.panel1.Controls.Add(this.btnBrowse);
            this.panel1.Controls.Add(this.txtFilespecs);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(555, 127);
            this.panel1.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Вывод данных:";
            // 
            // btnParse
            // 
            this.btnParse.Location = new System.Drawing.Point(441, 59);
            this.btnParse.Name = "btnParse";
            this.btnParse.Size = new System.Drawing.Size(101, 23);
            this.btnParse.TabIndex = 11;
            this.btnParse.Text = "Распарсить";
            this.btnParse.UseVisualStyleBackColor = true;
            this.btnParse.Click += new System.EventHandler(this.btnParse_Click);
            // 
            // chkDb
            // 
            this.chkDb.AutoSize = true;
            this.chkDb.Location = new System.Drawing.Point(30, 99);
            this.chkDb.Name = "chkDb";
            this.chkDb.Size = new System.Drawing.Size(134, 17);
            this.chkDb.TabIndex = 9;
            this.chkDb.Tag = "db";
            this.chkDb.Text = "В базу данных SQLite";
            this.chkDb.UseVisualStyleBackColor = true;
            this.chkDb.CheckedChanged += new System.EventHandler(this.chkOutput_CheckedChanged);
            // 
            // chkExcel
            // 
            this.chkExcel.AutoSize = true;
            this.chkExcel.Location = new System.Drawing.Point(30, 76);
            this.chkExcel.Name = "chkExcel";
            this.chkExcel.Size = new System.Drawing.Size(91, 17);
            this.chkExcel.TabIndex = 10;
            this.chkExcel.Tag = "excel";
            this.chkExcel.Text = "Excel (HTML)";
            this.chkExcel.UseVisualStyleBackColor = true;
            this.chkExcel.CheckedChanged += new System.EventHandler(this.chkOutput_CheckedChanged);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(467, 24);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 8;
            this.btnBrowse.Text = "Обзор...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtFilespecs
            // 
            this.txtFilespecs.Location = new System.Drawing.Point(15, 26);
            this.txtFilespecs.Name = "txtFilespecs";
            this.txtFilespecs.Size = new System.Drawing.Size(446, 20);
            this.txtFilespecs.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(150, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Папка с файлами реестров:";
            // 
            // FormAppUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(555, 309);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.MinimumSize = new System.Drawing.Size(500, 340);
            this.Name = "FormAppUI";
            this.Text = "Parse Bank Reestr Files";
            this.Shown += new System.EventHandler(this.FormAppUI_Shown);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel stLab1;
        private System.Windows.Forms.ToolStripStatusLabel stLab2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnParse;
        private System.Windows.Forms.CheckBox chkDb;
        private System.Windows.Forms.CheckBox chkExcel;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtFilespecs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FolderBrowserDialog dlgBrowseForDir;
    }
}