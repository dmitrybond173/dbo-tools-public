/*
 * Log Facts Extractor: visualization plug-in for CSLMON logs - UI for visualization parameters.
 * 
 * WARNING!
 * In current version this UI is ignored. You can simply click [OK] button...
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2023-12-06
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XService.Utils;
using XService.UI;

namespace Plugin.CslmonClientsAndSessions
{
    public partial class FormCslmonVisualizationParams : Form
    {
        public static bool Execute(Form pOwner, Dictionary<string, object> pParams)
        {
            using (FormCslmonVisualizationParams frm = new FormCslmonVisualizationParams())
            {
                frm.prms = pParams;
                frm.display();
                bool isOk = (frm.ShowDialog() == DialogResult.OK);
                if (isOk)
                {
                    frm.commit();
                }
                return isOk;
            }
        }

        public FormCslmonVisualizationParams()
        {
            InitializeComponent();
        }

        private Dictionary<string, object> prms;
        private LogFactsExtractorPlugin plugin = null;
        private DateTime minTs = DateTime.MinValue;
        private DateTime maxTs = DateTime.MinValue;

        private void display()
        { 
            object obj;
            if (this.prms.TryGetValue("Plugin", out obj))
            {
                this.plugin = (LogFactsExtractorPlugin)obj;
                string s;
                if (this.plugin.GetInfo().TryGetValue("VisualizationTypes", out s)) 
                    populateVisualizationTypes(s);
            }
            if (this.prms.TryGetValue("minTs", out obj)) this.minTs = (DateTime)obj;
            if (this.prms.TryGetValue("maxTs", out obj)) this.maxTs = (DateTime)obj;

            PluginUtils.NskTsToUi(StrUtils.NskTimestampOf(this.minTs), dtpMinTsDt, dtpMinTsTm);
            PluginUtils.NskTsToUi(StrUtils.NskTimestampOf(this.maxTs), dtpMaxTsDt, dtpMaxTsTm);

            if (this.prms.TryGetValue("sortByInitTime", out obj)) chkSortByInitTime.Checked = (bool)obj;

            UiTools.RenderInto(txtLegend,
                "<root>" +
                "On drawing it shows:<br/>" +
                "* CSL clients (by thin lines)<br/>" +
                "* Executor sessions (by thick lines on a thin line of CSL client)<br/>" +
                "<br/>" +
                "Thick lines of different colors on Executor session line:<br/>" +
                "<b color='navy'>NAVY</b> - CSL client line<br/>" +
                "<b color='teal'>TEAL</b> - executor session allocated to CSL client<br/>" +
                "<b color='silver'>SILVER</b> - wait-time for executor session allocation<br/>" +
                "<b color='magenta'>MAGENTA</b> - initialization time of this concrete executor<br/>" +
                "</root>"
                );
        }

        private void commit()
        {
            string ts = PluginUtils.UiToNskTs(dtpMinTsDt, dtpMinTsTm);
            this.prms["minTs"] = StrUtils.NskTimestampToDateTime(ts);
            
            ts = PluginUtils.UiToNskTs(dtpMaxTsDt, dtpMaxTsTm);
            this.prms["maxTs"] = StrUtils.NskTimestampToDateTime(ts);

            this.prms["sortByInitTime"] = chkSortByInitTime.Checked;

            this.prms["VisualizationType"] = cmbVisualType.Items[cmbVisualType.SelectedIndex].ToString();
        }

        private void populateVisualizationTypes(string pTypes)
        {
            int iSel = -1;
            cmbVisualType.BeginUpdate();
            try
            {
                cmbVisualType.Items.Clear();
                if (string.IsNullOrEmpty(pTypes)) 
                {
                    cmbVisualType.Enabled = false;
                    return ;
                }

                object obj;
                string selectedType = null;
                if (this.prms.TryGetValue("VisualizationType", out obj)) 
                    selectedType = obj.ToString();


                string[] types = pTypes.Split(',');
                foreach (string t in types)
                {
                    cmbVisualType.Items.Add(t);
                    if (selectedType.CompareTo(t) == 0)
                        iSel = cmbVisualType.Items.Count - 1;
                }
            }
            finally 
            { 
                cmbVisualType.EndUpdate();
                if (iSel >= 0)
                    cmbVisualType.SelectedIndex = iSel;
                else if (cmbVisualType.Items.Count > 0)
                    cmbVisualType.SelectedIndex = 0;
            }
        }

        private bool validate()
        {
            DateTime ts1 = StrUtils.NskTimestampToDateTime(PluginUtils.UiToNskTs(dtpMinTsDt, dtpMinTsTm));
            DateTime ts2 = StrUtils.NskTimestampToDateTime(PluginUtils.UiToNskTs(dtpMaxTsDt, dtpMaxTsTm));
            string errMsg = "";
            if (this.minTs > ts1)
            {
                errMsg += (Environment.NewLine + "Min time cannot be less than olest LogTime value in log file!");
                PluginUtils.NskTsToUi(StrUtils.NskTimestampOf(this.minTs), dtpMinTsDt, dtpMinTsTm);
            }
            if (this.maxTs < ts2)
            {
                errMsg += (Environment.NewLine + "Max time cannot be higher than newest LogTime value in log file!");
                PluginUtils.NskTsToUi(StrUtils.NskTimestampOf(this.maxTs), dtpMaxTsDt, dtpMaxTsTm);
            }
            bool isOk = string.IsNullOrEmpty(errMsg);
            if (!isOk)
            {
                MessageBox.Show("Validation problems:" + errMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return isOk;
        }

        #region Form Event Handlers

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!validate()) return;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void FormCslmonVisualizationParams_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\x1B')
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        #endregion // Form Event Handlers
    }
}
