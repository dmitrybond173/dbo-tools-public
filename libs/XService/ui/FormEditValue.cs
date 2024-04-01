using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace XService.UI.CommonForms
{
    public partial class FormEditValue : Form
    {
        [Flags]
        public enum EEditValueFlags
        {
            None = 0,
            ListOnlySelection = 0x0001,
            Multiline = 0x0002,
            NonEmpty = 0x0004,
            MonoWidthFont = 0x0008,
            Resizable = 0x0010,
            Password = 0x0020,
            BlockAutoCommit = 0x0040,

            DefaultStringValue = NonEmpty,
            DefaultTextValue = NonEmpty | MonoWidthFont | Resizable,
            DefaultFixedListValue = NonEmpty | ListOnlySelection,
        }

        public delegate bool ValidateValueMethod(Form pSender, string pValue);

        public static bool Execute(Form pOwner, string pCaption, string pPrompt, ref string pValue)
        {
            return Execute(pOwner, pCaption, pPrompt, ref pValue, EEditValueFlags.None, null, null);
        }

        public static bool Execute(Form pOwner, string pCaption, string pPrompt, ref string pValue, EEditValueFlags pFlags)
        {
            return Execute(pOwner, pCaption, pPrompt, ref pValue, pFlags, null, null);
        }

        public static bool Execute(Form pOwner, string pCaption, string pPrompt, ref string pValue, EEditValueFlags pFlags, List<string> pValues)
        {
            return Execute(pOwner, pCaption, pPrompt, ref pValue, pFlags, pValues, null);
        }

        public static bool Execute(Form pOwner, string pCaption, string pPrompt, ref string pValue, EEditValueFlags pFlags, List<string> pValues, ValidateValueMethod pValidator)
        {
            using (FormEditValue frm = new FormEditValue())
            {
                //frm.Parent = pOwner;
                frm.flags = pFlags;
                frm.validator = pValidator;
                frm.Display(pCaption, pPrompt, pValue, pFlags, pValues);

                DialogResult dr = frm.ShowDialog(pOwner);
                bool isCommit = (dr == DialogResult.OK);
                if (isCommit)
                {
                    frm.Commit(ref pValue);
                }
                return isCommit;
            }
        }

        public FormEditValue()
        {
            InitializeComponent();
            this.uiCtx = SynchronizationContext.Current;
        }

        private bool isReady = false;
        private bool validationInProgress = false;
        private System.Threading.Timer tmrUiInit = null;
        private int tickStart = -1;
        private EEditValueFlags flags = EEditValueFlags.None;
        private ValidateValueMethod validator = null;
        private SynchronizationContext uiCtx;

        private bool IsMultiline { get { return ((this.flags & EEditValueFlags.Multiline) == EEditValueFlags.Multiline); } }
        private bool IsListOnlySelection { get { return ((this.flags & EEditValueFlags.ListOnlySelection) == EEditValueFlags.ListOnlySelection); } }
        private bool IsNonEmpty { get { return ((this.flags & EEditValueFlags.NonEmpty) == EEditValueFlags.NonEmpty); } }
        private bool IsMonoWidthFont { get { return ((this.flags & EEditValueFlags.MonoWidthFont) == EEditValueFlags.MonoWidthFont); } }
        private bool IsResizable { get { return ((this.flags & EEditValueFlags.Resizable) == EEditValueFlags.Resizable); } }
        private bool IsPassword { get { return ((this.flags & EEditValueFlags.Password) == EEditValueFlags.Password); } }
        private bool IsBlockAutoCommit { get { return ((this.flags & EEditValueFlags.Password) == EEditValueFlags.BlockAutoCommit); } }        

        private Control getValueTextControl()
        {
            if (IsMultiline)
                return this.txtTextLines;
            else
                return this.txtValue;
        }

        private string getValue()
        {
            if (IsMultiline || IsPassword)
                return this.txtTextLines.Text;
            else
            {
                if (IsListOnlySelection)
                {
                    if (this.txtValue.SelectedIndex < 0 || this.txtValue.Items.Count == 0)
                        return null;
                    else
                        return this.txtValue.Items[this.txtValue.SelectedIndex].ToString();
                }
                else
                    return this.txtValue.Text.TrimEnd();
            }
        }

        private void Display(string pCaption, string pPrompt, string pValue, EEditValueFlags pFlags, List<string> pValues)
        {
            this.Text = pCaption;
            this.labPrompt.Text = pPrompt;

            if (IsMultiline || IsPassword)
            {
                if (IsMultiline)
                {
                    this.Height = 280;
                    this.AcceptButton = null;
                }
                
                txtTextLines.Text = pValue;
                txtTextLines.Multiline = IsMultiline;

                if (IsPassword)
                    this.txtTextLines.PasswordChar = '*';
            }
            else
            {
                if (pValues != null)
                {
                    txtValue.Items.AddRange(pValues.ToArray());
                    int idx = pValues.IndexOf(pValue);
                    if (idx >= 0)
                        txtValue.SelectedIndex = idx;
                    if ((this.flags & EEditValueFlags.ListOnlySelection) == EEditValueFlags.ListOnlySelection)
                        txtValue.DropDownStyle = ComboBoxStyle.DropDownList;
                    else
                        txtValue.DropDownStyle = ComboBoxStyle.DropDown;
                }
                else
                {
                    this.txtValue.Text = pValue;
                    txtValue.DropDownStyle = ComboBoxStyle.Simple;
                }
            }

            if (IsResizable)
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;

            if (IsMonoWidthFont)
                getValueTextControl().Font = new Font(FontFamily.GenericMonospace, getValueTextControl().Font.Size);
        }

        private void Commit(ref string pValue)
        {
            pValue = getValue();
        }

        private void delayedUiInit(object pCtx)
        {
            if (pCtx != null)
                Thread.Sleep(550);

            this.isReady = true;
            Thread.Sleep(330);

            this.AcceptButton = btnOk;
            getValueTextControl().Focus();

            try
            {
                System.Threading.Timer tmr = this.tmrUiInit;
                this.tmrUiInit = null;
                if (tmr != null)
                    tmr.Dispose();
            }
            catch { }
        }

        private void tmpInitUi(Object state)
        {
            uiCtx.Post(delayedUiInit, null);
        }

        #region Form Events Handlers

        private bool validatorLatch = false;

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.validationInProgress = true;
            try
            {
                if (this.validator != null)
                {
                    if (!this.validator(this, getValue()))
                    {
                        this.validatorLatch = true;
                        getValueTextControl().Focus();
                        Thread.Sleep(150);
                        return;
                    }
                }
                if (IsNonEmpty)
                {
                    if (string.IsNullOrEmpty(getValue()))
                    {
                        MessageBox.Show("Please enter non empty value.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        getValueTextControl().Focus();
                        Thread.Sleep(150);
                        return;
                    }
                }
            }
            finally { this.validationInProgress = false; }
            this.DialogResult = DialogResult.OK;
        }

        private void FormEditValue_Shown(object sender, EventArgs e)
        {
            txtValue.Visible = !(IsMultiline || IsPassword);
            txtValue.Enabled = txtValue.Visible;

            txtTextLines.Visible = (IsMultiline || IsPassword);
            txtTextLines.Enabled = txtTextLines.Visible;

            if (txtTextLines.Enabled)
                txtTextLines.Focus();
            if (txtValue.Enabled)
                txtValue.Focus();

            this.tickStart = Environment.TickCount;

            if (IsBlockAutoCommit)
            {
                //this.tmrUiInit = new System.Threading.Timer(tmpInitUi, null, 550, Timeout.Infinite);
                uiCtx.Post(delayedUiInit, this);
            }
            else
            {
                this.AcceptButton = btnOk;
                this.isReady = true;
            }
        }

        // Note: this method still does not work properly!
        // By unknown reason and despite all attempts there is still a double reaction on [Enter]/[Esc]!!! :-(
        private void FormEditValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (!this.isReady) { e.Handled = true; return; }
            if ((Environment.TickCount - this.tickStart) < 700) { e.Handled = true; return; }

            if (e.Handled) return;
            if (this.validationInProgress) { e.Handled = true; return; }
            if (this.validatorLatch) { this.validatorLatch = false; e.Handled = true; return; }

            bool hasModifiers = (e.Alt && e.Control && e.Shift);
            if (hasModifiers) return;

            if (!IsMultiline && getValueTextControl().Focused && e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                btnOk_Click(sender, e);
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.DialogResult = DialogResult.Cancel;
            }
        }

        #endregion // Form Events Handlers
    }
}
