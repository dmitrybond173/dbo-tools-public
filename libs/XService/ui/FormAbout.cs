using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace XService.UI.CommonForms
{
    public partial class FormAbout : Form
    {
        public static bool Execute(Form pOwner, string pCaption, string pInfo, string pLicenseText)
        {
            using (FormAbout frm = new FormAbout())
            {
				frm.Display(pCaption, pInfo, pLicenseText);
                DialogResult dr = frm.ShowDialog(pOwner);
                bool isCommit = (dr == DialogResult.OK);
                return isCommit;
            }
        }

        public FormAbout()
        {
            InitializeComponent();
        }

		private void Display(string pCaption, string pInfo, string pLicenseText)
        {
			this.licenseText = pLicenseText;
			this.Text = pCaption;
            txtInfo.Text = pInfo;

			if (string.IsNullOrEmpty(pLicenseText))
			{
				btnLicense.Enabled = false;
				btnLicense.Visible = false;
				btnOK.Left = (this.Width - btnOK.Width) / 2;
			}
        }

		private void btnLicense_Click(object sender, EventArgs e)
		{
			FormShowText.Execute(this, "License", this.licenseText, new Size(500, 460));
		}

		private string licenseText;
    }
}
