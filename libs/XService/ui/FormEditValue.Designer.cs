namespace XService.UI.CommonForms
{
    partial class FormEditValue
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labPrompt = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.ComboBox();
            this.txtTextLines = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(250, 60);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "O&K";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(331, 60);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // labPrompt
            // 
            this.labPrompt.AutoSize = true;
            this.labPrompt.Location = new System.Drawing.Point(8, 9);
            this.labPrompt.Name = "labPrompt";
            this.labPrompt.Size = new System.Drawing.Size(34, 13);
            this.labPrompt.TabIndex = 0;
            this.labPrompt.Text = "&Value";
            // 
            // txtValue
            // 
            this.txtValue.FormattingEnabled = true;
            this.txtValue.Location = new System.Drawing.Point(8, 25);
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(398, 21);
            this.txtValue.TabIndex = 4;
            // 
            // txtTextLines
            // 
            this.txtTextLines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTextLines.Enabled = false;
            this.txtTextLines.Location = new System.Drawing.Point(8, 25);
            this.txtTextLines.Multiline = true;
            this.txtTextLines.Name = "txtTextLines";
            this.txtTextLines.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtTextLines.Size = new System.Drawing.Size(398, 26);
            this.txtTextLines.TabIndex = 5;
            this.txtTextLines.Visible = false;
            this.txtTextLines.WordWrap = false;
            // 
            // FormEditValue
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(414, 92);
            this.Controls.Add(this.txtTextLines);
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.labPrompt);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormEditValue";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Value";
            this.Shown += new System.EventHandler(this.FormEditValue_Shown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FormEditValue_KeyUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labPrompt;
        private System.Windows.Forms.ComboBox txtValue;
        private System.Windows.Forms.TextBox txtTextLines;
    }
}