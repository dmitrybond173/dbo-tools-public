namespace XService.UI.CommonForms
{
    partial class FormAbout
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAbout));
			this.imgLogo = new System.Windows.Forms.PictureBox();
			this.txtInfo = new System.Windows.Forms.TextBox();
			this.btnLicense = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.imgLogo)).BeginInit();
			this.SuspendLayout();
			// 
			// imgLogo
			// 
			this.imgLogo.Image = ((System.Drawing.Image)(resources.GetObject("imgLogo.Image")));
			this.imgLogo.Location = new System.Drawing.Point(112, 10);
			this.imgLogo.Name = "imgLogo";
			this.imgLogo.Size = new System.Drawing.Size(130, 63);
			this.imgLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.imgLogo.TabIndex = 0;
			this.imgLogo.TabStop = false;
			// 
			// txtInfo
			// 
			this.txtInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtInfo.BackColor = System.Drawing.SystemColors.Control;
			this.txtInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtInfo.Location = new System.Drawing.Point(12, 79);
			this.txtInfo.Multiline = true;
			this.txtInfo.Name = "txtInfo";
			this.txtInfo.ReadOnly = true;
			this.txtInfo.Size = new System.Drawing.Size(330, 172);
			this.txtInfo.TabIndex = 1;
			this.txtInfo.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.txtInfo.WordWrap = false;
			// 
			// btnLicense
			// 
			this.btnLicense.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.btnLicense.Location = new System.Drawing.Point(178, 262);
			this.btnLicense.Name = "btnLicense";
			this.btnLicense.Size = new System.Drawing.Size(130, 23);
			this.btnLicense.TabIndex = 0;
			this.btnLicense.Text = "License";
			this.btnLicense.UseVisualStyleBackColor = true;
			this.btnLicense.Click += new System.EventHandler(this.btnLicense_Click);
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(47, 262);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(130, 23);
			this.btnOK.TabIndex = 0;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			// 
			// FormAbout
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(354, 292);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnLicense);
			this.Controls.Add(this.txtInfo);
			this.Controls.Add(this.imgLogo);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormAbout";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About";
			((System.ComponentModel.ISupportInitialize)(this.imgLogo)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox imgLogo;
        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.Button btnLicense;
		private System.Windows.Forms.Button btnOK;
    }
}