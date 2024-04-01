using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace XService.UI.CommonForms
{
    // Note: still not implemented!
    public partial class FormSelectItems : Form
    {
        [Flags]
        public enum ESelectItemFlags
        {
            /// <summary>No flags</summary>
            None = 0,
            
            /// <summary>Can select only one item</summary>
            SingleItem = 0x0001,

            /// <summary>Use non-resizable window</summary>
            DialogBox = 0x0002,

            /// <summary>Should be non-empty selection</summary>
            NonEmpty = 0x0004,

            /// <summary>Switch into details view (with columns)</summary>
            DetailsView = 0x0008,

            /// <summary>Switch into details view (with columns)</summary>
            UsePreferedSize = 0x0010,
        }

        public static Size WINDOW_SIZE = Size.Empty;

        public static bool Execute(Form pOwner, string pCaption, string pPrompt, List<string> pItems, List<string> pSelected, ESelectItemFlags pFlags)
        {
            using (FormSelectItems frm = new FormSelectItems())
            {
                frm.display(pCaption, pPrompt, pItems, pSelected, pFlags);

                DialogResult dr = frm.ShowDialog(pOwner);
                bool isCommit = (dr == DialogResult.OK);
                if (isCommit)
                {
                    frm.commit(pSelected);
                }
                return isCommit;
            }
        }

        public FormSelectItems()
        {
            InitializeComponent();
        }

        private List<string> items;
        private List<string> selected;
        private ESelectItemFlags flags = ESelectItemFlags.None;

        private bool hasFlag(ESelectItemFlags pFlag)
        {
            return ((this.flags & pFlag) == pFlag);
        }

        private void display(string pCaption, string pPrompt, List<string> pItems, List<string> pSelected, ESelectItemFlags pFlags)
        {
            this.Text = pCaption;
            this.labPrompt.Text = pPrompt;
            this.items = pItems;
            this.selected = pSelected;
            this.flags = pFlags;

            if (hasFlag(ESelectItemFlags.DialogBox))
            {
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MinimizeBox = false;
                this.MaximizeBox = false;
            }

            if (hasFlag(ESelectItemFlags.SingleItem))
                this.lvItems.CheckBoxes = false;
            else
                this.lvItems.CheckBoxes = true;

            if (hasFlag(ESelectItemFlags.DetailsView))
                this.lvItems.View = View.Details;

            lvItems.BeginUpdate();
            try
            {
                lvItems.Items.Clear();
                int iSelItem = -1;
                int colsCount = 0;
                foreach (string it in this.items)
                {
                    ListViewItem li = null;
                    string[] cols = it.Split('\t');
                    if (cols.Length > 1)
                    {
                        li = new ListViewItem(cols);
                        if (colsCount < cols.Length)
                            colsCount = cols.Length;
                    }
                    else
                        li = new ListViewItem(it);
                    li.Tag = it;
                    if (this.selected != null)
                    {
                        int idx = this.selected.IndexOf(li.Text);
                        if (idx < 0)
                            idx = this.selected.IndexOf(li.SubItems[0].Text);
                        if (hasFlag(ESelectItemFlags.SingleItem))
                        {
                            if (iSelItem < 0)
                                iSelItem = lvItems.Items.Count - 1;
                        }
                        else
                            li.Checked = (idx >= 0);
                    }
                    lvItems.Items.Add(li);
                }
                if (colsCount > 1)
                { 
                    ColumnHeader ch = lvItems.Columns.Add("Description");
                    ch.Width = 260;
                }
            }
            finally { lvItems.EndUpdate(); }
        }

        private void commit(List<string> pSelected)
        {
            this.selected.Clear();
            if (hasFlag(ESelectItemFlags.SingleItem))
            {
                this.selected.Add(lvItems.SelectedItems[0].Tag.ToString());
            }
            else
            {
                this.selected.Clear();
                foreach (ListViewItem li in lvItems.CheckedItems)
                    this.selected.Add(li.Tag.ToString());
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            int selCount = (hasFlag(ESelectItemFlags.SingleItem) ? lvItems.SelectedItems.Count : lvItems.CheckedItems.Count);
            if (hasFlag(ESelectItemFlags.NonEmpty) && selCount < 1)
            {
                MessageBox.Show("Need to select an item in list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        private void lvItems_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                btnOk_Click(btnOk, e);
        }

        private void FormSelectItems_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\x1B')
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void FormSelectItems_Shown(object sender, EventArgs e)
        {
            if (hasFlag(ESelectItemFlags.UsePreferedSize))
            {
                if (WINDOW_SIZE.Width != 0 && WINDOW_SIZE.Height != 0)
                {
                    this.Width = WINDOW_SIZE.Width;
                    this.Height = WINDOW_SIZE.Height;
                }
            }
        }
    }
}
