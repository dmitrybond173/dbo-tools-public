using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using XService.Utils;

namespace XService.UI
{
    /// <summary>
    /// Side panel - small togglable panel at right or at left of some window. 
    /// The word "side" means - panel is stick to left or to right side of window.
    /// Panel can be open/displayed. In such case it shows content of panel and user can operate it. But thete is a [hide] button somewhere on that panel.
    /// Panel can be hidden then it shows only a small vertical label with panel caption. By clicking on that label user can open panel back.
    /// </summary>
    public class SidePanelHandler
    {
        /// <summary>Contstruct togglable side-panel</summary>
        /// <param name="pAppSettings">Application settings object to store panel settings in</param>
        /// <param name="pSettingName">Name of string item in Application settings to store panel settings</param>
        /// <param name="pState">Current state of side-panel</param>
        /// <example>
        ///   this.sideToolsPanel = new SidePanelHandler(Settings.Default, "Side_ToolsPanel", false);
        ///   this.sideToolsPanel.Setup(this.labToolsShortcut, this.btnToolsHide, this.panTools, this.labToolsCaption, this.splitTools);
        /// </example>
        public SidePanelHandler(ApplicationSettingsBase pAppSettings, string pSettingName, bool pState)
        {
            this.AppSettings = pAppSettings;
            this.SettingName = pSettingName;
            this.State = pState;

            DeserializeView(null);
        }

        /// <summary>Application settings object to store panel settings in</summary>
        public ApplicationSettingsBase AppSettings { get; protected set; }

        /// <summary>Name of string item in Application settings to store panel settings</summary>
        public string SettingName { get; protected set; }

        /// <summary>Width of side-panel when it is open</summary>
        public int Width = 120;

        /// <summary>Current state of side-panel (ON/OFF)</summary>
        public bool State { get; protected set; }

        /// <summary>Ref to [Hide] button</summary>
        public Button BtnHide;
        /// <summary>Ref to shortcut label. When panel is hidden only this label is displayed. By clicking this label panel can be opened</summary>
        public Label LabOpen;
        /// <summary>Ref to label control which is Caption of panel. It is visible only when panel is open</summary>
        public Label LabCaption;
        /// <summary>Ref to content panel inside side-panel</summary>
        public Panel PanContent;
        /// <summary>Ref to Splitter control which separates opened side-panel from rest of window</summary>
        public Splitter Split;

        /// <summary>Setup side-panel</summary>
        public void Setup(Label pLabOpen, Button pBtnHide, Panel pPanContent, Label pLabCaption, Splitter pSplit)
        {
            this.LabOpen = pLabOpen;
            this.BtnHide = pBtnHide;
            this.PanContent = pPanContent;
            this.LabCaption = pLabCaption;
            this.Split = pSplit;

            this.LabOpen.Paint += this.LabOpen_Paint;
            this.LabOpen.Click += this.LabOpen_Click;
            this.BtnHide.Click += this.BtnHide_Click;

            // initialize default state
            ToggleState(this.State);
        }

        /// <summary>Searilize side-panel state into string, save into AppSettings, also return that string</summary>
        public string SerializeView()
        {
            string s = string.Format("State={0};Width={1}", this.State, this.Width);
            this.AppSettings[this.SettingName] = s;
            this.AppSettings.Save();
            return s;
        }

        /// <summary>Desearilize side-panel state from string</summary>
        public void DeserializeView(string pDefs)
        {
            if (string.IsNullOrEmpty(pDefs))
                pDefs = this.AppSettings[this.SettingName].ToString();
            if (string.IsNullOrEmpty(pDefs)) return;

            Dictionary<string, string> props = CollectionUtils.ParseParametersStrEx(pDefs, true);
            foreach (KeyValuePair<string, string> it in props)
            {
                int n;
                bool q;
                if (StrUtils.IsSameText(it.Key, "State") && StrUtils.GetAsBool(it.Value, out q))
                    this.State = q;
                else if (StrUtils.IsSameText(it.Key, "Width") && StrUtils.GetAsInt(it.Value, out n))
                    this.Width = n;
            }
        }

        /// <summary>Toggle side-panel state</summary>
        public void ToggleState(bool pNewState)
        {
            int savedW = this.Width;
            if (pNewState)
            {
                UiTools.CTRL_Enable(this.LabOpen, false, false);
                UiTools.CTRL_Enable(this.Split, true, true);
                UiTools.CTRL_Enable(this.LabCaption, true, true);
                UiTools.CTRL_Enable(this.BtnHide, true, true);
                //UiTools.CTRL_Enable(this.PanContent, true, true);
                UiTools.ETRL_EnableChilds(this.PanContent, true, true, true, null);

                if (this.PanContent != null)
                {
                    this.PanContent.Width = this.Width;
                    this.PanContent.MinimumSize = new Size(180, 0);
                }
            }
            else
            {
                if (this.PanContent != null)
                {
                    this.Width = this.PanContent.Width;
                    this.PanContent.Tag = this.PanContent.Width;

                    this.PanContent.MinimumSize = new Size(22, 0);
                    this.PanContent.Width = 22;
                }

                UiTools.ETRL_EnableChilds(this.PanContent, false, false, true, null);
                //UiTools.CTRL_Enable(this.PanContent, false, false);
                UiTools.CTRL_Enable(this.Split, false, false);
                UiTools.CTRL_Enable(this.BtnHide, false, false);
                UiTools.CTRL_Enable(this.LabCaption, false, false);
                UiTools.CTRL_Enable(this.LabOpen, true, true);
            }
            bool isChanged = (this.State != pNewState || savedW != this.Width);
            this.State = pNewState;
            if (isChanged)
                SerializeView();
        }

        /// <summary>Auto-handler of [hide] button click</summary>
        public void BtnHide_Click(object sender, EventArgs e)
        {
            ToggleState(false);
        }

        /// <summary>Auto-handler of [open panel] click</summary>
        public void LabOpen_Click(object sender, EventArgs e)
        {
            ToggleState(true);
        }

        /// <summary>Auto-handler of Paint event for tab-label of side-panel. This lable is only displayed when side-panel is hidden</summary>
        public void LabOpen_Paint(object sender, PaintEventArgs e)
        {
            //UiTools.LAB_DrawVerticalString(LabOpen, e, LabOpen.Tag.ToString());
            UiTools.LAB_DrawVerticalString(LabOpen, e, LabOpen.Tag.ToString(), this.vCtx);
        }

        protected UiTools.VetricalLabelCtx vCtx = new UiTools.VetricalLabelCtx();
    }
}
