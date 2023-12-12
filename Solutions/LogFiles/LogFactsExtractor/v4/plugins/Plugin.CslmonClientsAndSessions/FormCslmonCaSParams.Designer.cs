namespace Plugin.CslmonClientsAndSessions
{
    partial class FormCslmonVisualizationParams
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
            this.dtpMinTsTm = new System.Windows.Forms.DateTimePicker();
            this.dtpMinTsDt = new System.Windows.Forms.DateTimePicker();
            this.labFromTs = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dtpMaxTsDt = new System.Windows.Forms.DateTimePicker();
            this.dtpMaxTsTm = new System.Windows.Forms.DateTimePicker();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtLegend = new System.Windows.Forms.RichTextBox();
            this.labLegend = new System.Windows.Forms.Label();
            this.labSrvClsHighlight = new System.Windows.Forms.Label();
            this.lvSrvClsHighlight = new System.Windows.Forms.ListView();
            this.labVisualType = new System.Windows.Forms.Label();
            this.cmbVisualType = new System.Windows.Forms.ComboBox();
            this.chkSortByInitTime = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // dtpMinTsTm
            // 
            this.dtpMinTsTm.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpMinTsTm.Location = new System.Drawing.Point(131, 24);
            this.dtpMinTsTm.Name = "dtpMinTsTm";
            this.dtpMinTsTm.ShowUpDown = true;
            this.dtpMinTsTm.Size = new System.Drawing.Size(80, 20);
            this.dtpMinTsTm.TabIndex = 2;
            // 
            // dtpMinTsDt
            // 
            this.dtpMinTsDt.CustomFormat = "";
            this.dtpMinTsDt.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpMinTsDt.Location = new System.Drawing.Point(12, 24);
            this.dtpMinTsDt.Name = "dtpMinTsDt";
            this.dtpMinTsDt.Size = new System.Drawing.Size(113, 20);
            this.dtpMinTsDt.TabIndex = 1;
            // 
            // labFromTs
            // 
            this.labFromTs.AutoSize = true;
            this.labFromTs.Location = new System.Drawing.Point(9, 9);
            this.labFromTs.Name = "labFromTs";
            this.labFromTs.Size = new System.Drawing.Size(84, 13);
            this.labFromTs.TabIndex = 0;
            this.labFromTs.Text = "Timestamp &from:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Timestamp &to:";
            // 
            // dtpMaxTsDt
            // 
            this.dtpMaxTsDt.CustomFormat = "";
            this.dtpMaxTsDt.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpMaxTsDt.Location = new System.Drawing.Point(12, 65);
            this.dtpMaxTsDt.Name = "dtpMaxTsDt";
            this.dtpMaxTsDt.Size = new System.Drawing.Size(113, 20);
            this.dtpMaxTsDt.TabIndex = 6;
            // 
            // dtpMaxTsTm
            // 
            this.dtpMaxTsTm.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpMaxTsTm.Location = new System.Drawing.Point(131, 65);
            this.dtpMaxTsTm.Name = "dtpMaxTsTm";
            this.dtpMaxTsTm.ShowUpDown = true;
            this.dtpMaxTsTm.Size = new System.Drawing.Size(80, 20);
            this.dtpMaxTsTm.TabIndex = 7;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(203, 388);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 12;
            this.btnOk.Text = "O&K";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(281, 388);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtLegend
            // 
            this.txtLegend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLegend.BackColor = System.Drawing.SystemColors.Info;
            this.txtLegend.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLegend.Location = new System.Drawing.Point(10, 257);
            this.txtLegend.Name = "txtLegend";
            this.txtLegend.ReadOnly = true;
            this.txtLegend.Size = new System.Drawing.Size(344, 126);
            this.txtLegend.TabIndex = 11;
            this.txtLegend.TabStop = false;
            this.txtLegend.Text = "";
            // 
            // labLegend
            // 
            this.labLegend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labLegend.AutoSize = true;
            this.labLegend.Location = new System.Drawing.Point(10, 241);
            this.labLegend.Name = "labLegend";
            this.labLegend.Size = new System.Drawing.Size(46, 13);
            this.labLegend.TabIndex = 10;
            this.labLegend.Text = "&Legend:";
            // 
            // labSrvClsHighlight
            // 
            this.labSrvClsHighlight.AutoSize = true;
            this.labSrvClsHighlight.Enabled = false;
            this.labSrvClsHighlight.Location = new System.Drawing.Point(9, 94);
            this.labSrvClsHighlight.Name = "labSrvClsHighlight";
            this.labSrvClsHighlight.Size = new System.Drawing.Size(82, 13);
            this.labSrvClsHighlight.TabIndex = 8;
            this.labSrvClsHighlight.Text = "Filter definitions:";
            // 
            // lvSrvClsHighlight
            // 
            this.lvSrvClsHighlight.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvSrvClsHighlight.CheckBoxes = true;
            this.lvSrvClsHighlight.HideSelection = false;
            this.lvSrvClsHighlight.Location = new System.Drawing.Point(12, 110);
            this.lvSrvClsHighlight.Name = "lvSrvClsHighlight";
            this.lvSrvClsHighlight.Size = new System.Drawing.Size(343, 126);
            this.lvSrvClsHighlight.TabIndex = 9;
            this.lvSrvClsHighlight.UseCompatibleStateImageBehavior = false;
            this.lvSrvClsHighlight.View = System.Windows.Forms.View.List;
            // 
            // labVisualType
            // 
            this.labVisualType.AutoSize = true;
            this.labVisualType.Location = new System.Drawing.Point(218, 9);
            this.labVisualType.Name = "labVisualType";
            this.labVisualType.Size = new System.Drawing.Size(91, 13);
            this.labVisualType.TabIndex = 3;
            this.labVisualType.Text = "&Visualization type:";
            // 
            // cmbVisualType
            // 
            this.cmbVisualType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbVisualType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbVisualType.FormattingEnabled = true;
            this.cmbVisualType.Location = new System.Drawing.Point(221, 24);
            this.cmbVisualType.Name = "cmbVisualType";
            this.cmbVisualType.Size = new System.Drawing.Size(133, 21);
            this.cmbVisualType.TabIndex = 4;
            // 
            // chkSortByInitTime
            // 
            this.chkSortByInitTime.AutoSize = true;
            this.chkSortByInitTime.Location = new System.Drawing.Point(221, 65);
            this.chkSortByInitTime.Name = "chkSortByInitTime";
            this.chkSortByInitTime.Size = new System.Drawing.Size(99, 17);
            this.chkSortByInitTime.TabIndex = 13;
            this.chkSortByInitTime.Text = "Sort by InitTime";
            this.chkSortByInitTime.UseVisualStyleBackColor = true;
            // 
            // FormCslmonVisualizationParams
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(364, 420);
            this.Controls.Add(this.chkSortByInitTime);
            this.Controls.Add(this.cmbVisualType);
            this.Controls.Add(this.lvSrvClsHighlight);
            this.Controls.Add(this.txtLegend);
            this.Controls.Add(this.labLegend);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.dtpMaxTsTm);
            this.Controls.Add(this.dtpMinTsTm);
            this.Controls.Add(this.dtpMaxTsDt);
            this.Controls.Add(this.labSrvClsHighlight);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dtpMinTsDt);
            this.Controls.Add(this.labVisualType);
            this.Controls.Add(this.labFromTs);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormCslmonVisualizationParams";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "CSLMON Logs Visualization";
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FormCslmonVisualizationParams_KeyPress);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtpMinTsTm;
        private System.Windows.Forms.DateTimePicker dtpMinTsDt;
        private System.Windows.Forms.Label labFromTs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpMaxTsDt;
        private System.Windows.Forms.DateTimePicker dtpMaxTsTm;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.RichTextBox txtLegend;
        private System.Windows.Forms.Label labLegend;
        private System.Windows.Forms.Label labSrvClsHighlight;
        private System.Windows.Forms.ListView lvSrvClsHighlight;
        private System.Windows.Forms.Label labVisualType;
        private System.Windows.Forms.ComboBox cmbVisualType;
        private System.Windows.Forms.CheckBox chkSortByInitTime;
    }
}