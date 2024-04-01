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
    public partial class FormShowText : Form
    {
        /// <summary>
        /// Holder of UI parameters for FormShowText 
        /// </summary>
        public class UiParams
        {
            /// <summary>Form specified as Parent/Owner for FormShowText</summary>
            public Form Owner = null;

            /// <summary>Current instance of FormShowText (set automatically in Display() method, also reset automatically on form closure)</summary>
            public FormShowText Instance;

            /// <summary>Caption of FormShowText window</summary>
            public string Caption = null;

            /// <summary>Text to display</summary>
            public string Text = null;

            /// <summary>Use custom size of FormShowText form</summary>
            public Size WindowSize = Size.Empty;

            /// <summary>Use text wrapping</summary>
            public bool WrapText = false;

            /// <summary>Hide buttons any at all</summary>
            public bool HideButtons = false;

            /// <summary>Use standard Windows buttons (used by MessageBox)</summary>
            public bool UseStdButtons = false;

            /// <summary>Standard Windows buttons (used by MessageBox). But this is used only when UseStdButtons=true</summary>
            public MessageBoxButtons Buttons = MessageBoxButtons.OK;

            /// <summary>Use monowidth font to display text</summary>
            public bool MonoWidthFont = false;

            /// <summary>Use web-view instead of text view. When UseWebView=true then Text should contain URL to display</summary>
            public bool UseWebView = false;

            /// <summary>Synchronization event</summary>
            public EventWaitHandle Wait = null;

            public delegate void WebViewNavigationMethod(object sender, WebBrowserNavigatingEventArgs e1, WebBrowserNavigatedEventArgs e2);

            /// <summary>WebView navigation. When called with e1 != null then it is OnNavigating() call, when e2 != null then it is OnNavigated() call</summary>
            public WebViewNavigationMethod WebViewNavigation;

            /// <summary>Copy parameters from specified source</summary>
            /// <param name="pParams">Source UI params to copy all from</param>
            public void Assign(UiParams pParams)
            {
                this.Owner = pParams.Owner;
                this.Caption = pParams.Caption;
                this.Text = pParams.Text;
                this.WindowSize = pParams.WindowSize;
                this.Wait = pParams.Wait;
                this.WrapText = pParams.WrapText;
                this.UseStdButtons = pParams.UseStdButtons;
                this.Buttons = pParams.Buttons;
                this.HideButtons = pParams.HideButtons;
                this.MonoWidthFont = pParams.MonoWidthFont;
                this.UseWebView = pParams.UseWebView;
                this.WebViewNavigation = pParams.WebViewNavigation;
            }
        }

        protected delegate DialogResult GenericExecuteMethod(UiParams pUiParams);
        //protected delegate bool ExecuteMethod1(Form pOwner, string pCaption, string pText, Size pWindowSize);

        /// <summary>Display FormShowText UI with specified parameters</summary>
        /// <param name="pOwner">Owner form</param>
        /// <param name="pCaption">Caption of FormShowText window</param>
        /// <param name="pText">Text to show</param>
        /// <param name="pWindowSize">Custom window size (or Size.Empty to use default size)</param>
        /// <returns>Returns true</returns>
        public static bool Execute(Form pOwner, string pCaption, string pText, Size pWindowSize)
        {
            DialogResult dr = DialogResult.Cancel;
            UiParams prms = new UiParams() { Owner = pOwner, Caption = pCaption, Text = pText, WindowSize = pWindowSize };
            bool needInovoke = (pOwner != null && pOwner.InvokeRequired);
            if (needInovoke)
            {
                //isCommit = (bool)pOwner.Invoke(new ExecuteMethod1(Execute), pOwner, pCaption, pText, pWindowSize);
                dr = (DialogResult)pOwner.Invoke(new GenericExecuteMethod(ExecuteEx), prms);
            }
            else
            {
                dr = ExecuteEx(prms);
            }
            return (dr == DialogResult.OK);
        }

        //protected delegate DialogResult ExecuteMethod2(Form pOwner, string pCaption, string pText, Size pWindowSize, MessageBoxButtons pButtons, EventWaitHandle pWait);

        /// <summary>Display FormShowText UI with specified parameters</summary>
        /// <param name="pOwner">Owner form</param>
        /// <param name="pCaption">Caption of FormShowText window</param>
        /// <param name="pText">Text to show</param>
        /// <param name="pWindowSize">Custom window size (or Size.Empty to use default size)</param>
        /// <param name="pButtons">Windows buttons (MessaageBox style) to use instead of default [Close] button</param>
        /// <param name="pWait"></param>
        /// <returns>Returns true</returns>
        public static DialogResult Execute(Form pOwner, string pCaption, string pText, Size pWindowSize, MessageBoxButtons pButtons, EventWaitHandle pWait)
        {
            DialogResult dr = DialogResult.Cancel;
            UiParams prms = new UiParams() { Owner = pOwner, Caption = pCaption, Text = pText, WindowSize = pWindowSize, Buttons = pButtons, UseStdButtons = true, Wait = pWait };
            bool needInovoke = (pOwner != null && pOwner.InvokeRequired);
            if (needInovoke)
            {
                dr = (DialogResult)pOwner.Invoke(new GenericExecuteMethod(ExecuteEx), prms);
            }
            else
            {
                dr = ExecuteEx(prms);
            }
            return dr;
        }

        //protected delegate DialogResult ExecuteMethod3(Form pOwner, string pCaption, string pText, UiParams pUiParams);

        /// <summary>Display FormShowText UI with specified parameters</summary>
        /// <param name="pOwner">Owner form</param>
        /// <param name="pCaption">Caption of FormShowText window</param>
        /// <param name="pText">Text to show</param>
        /// <param name="pUiParams">Custom UI parameters</param>
        /// <returns>Returns true</returns>
        public static DialogResult Execute(Form pOwner, string pCaption, string pText, UiParams pUiParams)
        {
            DialogResult dr = DialogResult.Cancel;
            UiParams prms = new UiParams();
            prms.Assign(pUiParams);
            prms.Owner = pOwner;
            prms.Caption = pCaption;
            prms.Text = pText;            
            bool needInovoke = (pOwner != null && pOwner.InvokeRequired);
            if (needInovoke)
            {
                dr = (DialogResult)pOwner.Invoke(new GenericExecuteMethod(ExecuteEx), prms);
            }
            else
            {
                dr = ExecuteEx(prms);
            }
            return dr;
        }

        /// <summary>Display FormShowText UI with specified parameters</summary>
        /// <param name="pUiParams">Custom UI parameters. Note: in this method this instance of UiParams is used explicitly!</param>
        /// <returns>Returns true</returns>
        public static DialogResult ExecuteEx(UiParams pUiParams)
        {
            DialogResult dr = DialogResult.Cancel;
            if (pUiParams == null) return dr;
            bool needInovoke = (pUiParams != null && pUiParams.Owner.InvokeRequired);
            if (needInovoke)
            {
                dr = (DialogResult)pUiParams.Owner.Invoke(new GenericExecuteMethod(ExecuteEx), pUiParams);
            }
            else
            {
                using (FormShowText frm = new FormShowText())
                {
                    // Note: it is very important to use UiParams instance explictly in this method!
                    //frm.uiParams.Assign(pUiParams);
                    frm.uiParams = pUiParams;

                    if (frm.uiParams.WindowSize != Size.Empty)
                    {
                        frm.Width = frm.uiParams.WindowSize.Width;
                        frm.Height = frm.uiParams.WindowSize.Height;
                    }

                    frm.Display(frm.uiParams.Caption, frm.uiParams.Text);
                    dr = frm.ShowDialog(frm.uiParams.Owner);

                    if (frm.uiParams.Wait != null)
                        frm.uiParams.Wait.Set();
                }
            }
            return dr;
        }

        public FormShowText()
        {
            InitializeComponent();
        }

        public UiParams Params { get { return this.uiParams; } }

        #region Implementation details

        private UiParams uiParams = new UiParams();
        private System.Windows.Forms.WebBrowser webBrowser1;

        private void initWebView()
        {
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.webBrowser1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowser1.Location = new System.Drawing.Point(6, 6);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            //this.webBrowser1.Size = new System.Drawing.Size(338, 239);
            this.webBrowser1.Size = new System.Drawing.Size(this.ClientSize.Width - 12, this.ClientSize.Height - 40);
            this.webBrowser1.TabIndex = 2;
            this.webBrowser1.Visible = false;
            this.webBrowser1.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.webBrowser1_Navigated);
            this.webBrowser1.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.webBrowser1_Navigating);
            //
            this.Controls.Add(this.webBrowser1);
            //
            this.ResumeLayout(false);
            this.PerformLayout();
        }

		private void Display(string pCaption, string pText)
        {
            this.uiParams.Instance = this;

            txtInfo.WordWrap = this.uiParams.WrapText;

            if (this.uiParams.MonoWidthFont)
                txtInfo.Font = new Font(FontFamily.GenericMonospace, 9);

            this.Text = pCaption;
            if (this.uiParams.UseWebView)
            {
                initWebView();

                txtInfo.Enabled = false;
                txtInfo.Visible = false;
                webBrowser1.Visible = true;
                webBrowser1.Navigate(pText);
            }
            else
                txtInfo.Text = pText;

            if (!this.uiParams.HideButtons)
            {
                if (this.uiParams.UseStdButtons)
                {
                    btnClose.Enabled = false;
                    btnClose.Visible = false;
                    createStdButtons();
                }
            }
            else
            {
                btnClose.Enabled = false;
                btnClose.Visible = false;
                if (this.uiParams.UseWebView)
                    webBrowser1.Height += 26;
                else
                    txtInfo.Height += 26;
            }
        }

        private void createStdButtons()
        {
            DialogResult[] buttonNames = null;
            switch (this.uiParams.Buttons)
            {
                case MessageBoxButtons.OK: buttonNames = new DialogResult[] { DialogResult.OK }; break;
                case MessageBoxButtons.OKCancel: buttonNames = new DialogResult[] { DialogResult.OK, DialogResult.Cancel }; break;
                case MessageBoxButtons.RetryCancel: buttonNames = new DialogResult[] { DialogResult.Retry, DialogResult.Cancel }; break;
                case MessageBoxButtons.YesNo: buttonNames = new DialogResult[] { DialogResult.Yes, DialogResult.No }; break;
                case MessageBoxButtons.YesNoCancel: buttonNames = new DialogResult[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel }; break;
                case MessageBoxButtons.AbortRetryIgnore: buttonNames = new DialogResult[] { DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore }; break;
            }
            Size btnSz = new Size(75, 23);
            Point pt = new Point((this.Width - (btnSz.Width * buttonNames.Length + 3)) / 2, this.Height - btnSz.Height - (SystemInformation.CaptionHeight + 20));
            int iBtn = 0;
            foreach (DialogResult dr in buttonNames)
            {
                iBtn++;
                Button btn = new Button();
                btn.DialogResult = dr;
                btn.Location = new Point(pt.X, pt.Y);
                btn.Name = "btn" + iBtn.ToString();
                btn.Size = new Size(btnSz.Width, btnSz.Height);
                btn.Text = dr.ToString();
                this.Controls.Add(btn);

                pt.X += btnSz.Width + 4; 
            }
        }

        private void FormShowText_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.uiParams.Instance = null;
        }

        private void FormShowText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\x1B')
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (this.uiParams.WebViewNavigation != null)
                this.uiParams.WebViewNavigation(sender, e, null);
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (this.uiParams.WebViewNavigation != null)
                this.uiParams.WebViewNavigation(sender, null, e);
        }

        #endregion // Implementation details

    }
}
