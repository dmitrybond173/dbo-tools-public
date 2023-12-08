/*
 * Log Facts Extractor: visualization plug-in for TCP-GW logs - UI for visualization parameters.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-08-24
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

namespace Plugin.CslmonCallsVisualization
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
        private List<string> srvCls;
        private List<string> selectedSrvCls;

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
            if (this.prms.TryGetValue("SelectedServerClasses", out obj)) this.selectedSrvCls = (List<string>)obj;
            if (this.prms.TryGetValue("ServerClasses", out obj))
            {
                this.srvCls = (List<string>)obj;
                populateSrvClasses();
            }
            lvSrvClsHighlight.Enabled = (this.srvCls != null && this.srvCls.Count > 0);

            PluginUtils.NskTsToUi(StrUtils.NskTimestampOf(this.minTs), dtpMinTsDt, dtpMinTsTm);
            PluginUtils.NskTsToUi(StrUtils.NskTimestampOf(this.maxTs), dtpMaxTsDt, dtpMaxTsTm);

            UiTools.RenderInto(txtLegend,
                "<root>" +
                "By X-axis - CSLMON executors (or CSLMON service itself), 1 column (5 pixels) = 1 executor <br/>" +
                "By Y-axis - time, 1 pixel = 1 second<br/>" +
                "<br/>" +
                "<b color='silver'>SILVER</b> - life-time of executor<br/>" +
                "<b color='green'>GREEN</b> - scope of commited transaction<br/>" +
                "<b color='red'>RED</b> - scope of aborted transactio, red line at end n<br/>" +
                "<b color='red'>RED</b> - scope of aborted transactio, red line at end n<br/>" +
                "</root>"
                );
        }

        private void commit()
        {
            string ts = PluginUtils.UiToNskTs(dtpMinTsDt, dtpMinTsTm);
            this.prms["minTs"] = StrUtils.NskTimestampToDateTime(ts);
            
            ts = PluginUtils.UiToNskTs(dtpMaxTsDt, dtpMaxTsTm);
            this.prms["maxTs"] = StrUtils.NskTimestampToDateTime(ts);

            this.prms["VisualizationType"] = cmbVisualType.Items[cmbVisualType.SelectedIndex].ToString();

            if (lvSrvClsHighlight.Enabled && lvSrvClsHighlight.Items.Count > 0 && this.selectedSrvCls != null)
            {
                foreach (ListViewItem li in lvSrvClsHighlight.Items)
                    if (li.Checked)
                        this.selectedSrvCls.Add(li.Tag.ToString().ToLower());
            }
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

        private void populateSrvClasses()
        {
            lvSrvClsHighlight.BeginUpdate();
            try
            {
                lvSrvClsHighlight.Items.Clear();
                if (this.srvCls == null) return;

                foreach (string srv in this.srvCls)
                {
                    lvSrvClsHighlight.Items.Add(new ListViewItem(srv) { Tag = srv } );
                }
            }
            finally { lvSrvClsHighlight.EndUpdate(); }
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
    }
}
