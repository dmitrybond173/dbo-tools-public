
Just a 'gentleman set' of UI-specific routinues for small applications.

This package includes VS-generated documentaiton, so this readme file contsina only some typically used highlights.

Example of typical code for AboutBox

        private void mmiAbout_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            TypeUtils.CollectVersionInfoAttributes(props, Assembly.GetEntryAssembly());
            props["ApplicationName"] = "ADO.NET Query";
            props["EOL"] = Environment.NewLine;
            props["url"] = "https://dmitrybond.wordpress.com/2012/10/20/three-queries/";
            props["userConfig"] = (string.IsNullOrEmpty(this.userConfigPath) ? "-" : this.userConfigPath);

            Assembly asm = Assembly.GetExecutingAssembly();
            props["HostInfo"] = CommonUtils.HostInfoStamp() + string.Format(" ProcessType:{0};", asm.GetName().ProcessorArchitecture);

            string info = ""
                + "$(ApplicationName).$(EOL)"
                + "Version $(Version) / $(FileVersion)$(EOL)"
                + "Written by Dmitry Bond. (dima_ben@ukr.net)$(EOL)"
                + "$(EOL)"
                + "$(HostInfo)$(EOL)"
                + "$(EOL)"
                + "$(url)$(EOL)"
                + "$(EOL)"
                + "$(userConfig)$(EOL)"
                + "";
            info = StrUtils.ExpandParameters(info, props, true);
            FormAbout.Execute(this, StrUtils.ExpandParameters("About $(ApplicationName)", props, true),
                info, HELP_LICENSE_TEXT);
        }

Example of rendering text into RichText UI control:

    StringBuilder sb = new StringBuilder();
    [...]
    sb.AppendLine(string.Format("<color color=\'navy\'>Statistic generation time</color>: <b>{0}</b> ms, <b>{1}</b><br/>", 
        (DateTime.Now - t1).TotalMilliseconds.ToString("N1"), DateTime.Now));
    sb.AppendLine("</root>");
    UiTools.RenderInto(txtInfo, sb.ToString());

Example of saving UI form location and settings:

    private string serializeExtraUiProps()
    {
        string txt = "";
        txt += string.Format("QueryHeight:{0}%; ", (100.0d * (double)panQuery.Height / this.Height).ToString("N2"));
        txt += string.Format("LogPanelHeight:{0}%; ", (100.0d * (double)lvLogger.Height / this.Height).ToString("N2"));
        txt += string.Format("HoldConnection:{0}; ", (tbHoldConnection.Checked ? "1" : "0"));
        return txt;
    }

    private void deserializeExtraUiProp(string pName, string pValue)
    {
        if (StrUtils.IsSameText(pName, "QueryHeight"))
        {
            try { panQuery.Height = (int)(Convert.ToDouble(pValue.Trim("%".ToCharArray())) / 100.0 * this.Height); }
            catch { }
        }
        if (StrUtils.IsSameText(pName, "LogPanelHeight"))
        {
            try { lvLogger.Parent.Height = (int)(Convert.ToDouble(pValue.Trim("%".ToCharArray())) / 100.0 * this.Height); }
            catch { }
        }            
        if (StrUtils.IsSameText(pName, "HoldConnection"))
        {
            try { tbHoldConnection.Checked = StrUtils.GetAsBool(pValue); }
            catch { }
        }
    }

    private void FormMain_Shown(object sender, EventArgs e)
    {
        // load MainForm UI settings (window position & state)
        if (!string.IsNullOrEmpty(Settings.Default.MainFormView))
            UiTools.DeserializeFormView(Settings.Default.MainFormView, this, this.deserializeExtraUiProp);

        [...]
    }

    private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
    {
        Settings.Default.MainFormView = UiTools.SerializeFormView(this) + serializeExtraUiProps();
        Settings.Default.Save();
        [...]
    }


Example of SidePanel for UI:

    private void FormMain_Load(object sender, EventArgs e)
    {
        [...]
        this.sideToolsPanel = new SidePanelHandler(Settings.Default, "Side_ToolsPanel", false);
        this.sideToolsPanel.Setup(this.labToolsShortcut, this.btnToolsHide, this.panTools, this.labToolsCaption, this.splitTools);
        [...]
    }

    private void mmiToolsToggleToolsPanel_Click(object sender, EventArgs e)
    {
        this.sideToolsPanel.ToggleState(!this.sideToolsPanel.State);
        mmiToolsToggleToolsPanel.Checked = this.sideToolsPanel.State;
    }


Example of simple modal EditValue UI:

    bool isOk = FormEditValue.Execute(this,
        Languages.Translate("Set Workspace Name"),
        Languages.Translate("Type workspace name"),
        ref s, FormEditValue.EEditValueFlags.NonEmpty, null, validateWsName);
    if (isOk)
    {
        this.document.DefaultWorkspace.SetName(s);
        updateAppCaption();
        populateWorkspaces();
    }


Example of simple modal SelectItems UI:

    List<string> list2 = new List<string>();
    FormSelectItems.WINDOW_SIZE = new System.Drawing.Size(520, 320);
    string title = (pRemove ? "Rules to Un-setup" : "Rules to Setup");
    string prompt = (pRemove ? "Select rules to un-setup" : "Select rules to setup");
    bool isOk = FormSelectItems.Execute(
        this, title, prompt, list1, list2, 
        FormSelectItems.ESelectItemFlags.DetailsView | FormSelectItems.ESelectItemFlags.NonEmpty | FormSelectItems.ESelectItemFlags.UsePreferedSize);
    if (!isOk) return;


Example of simple modal ShowText UI:

    FormShowText.UiParams prm = new FormShowText.UiParams() { WindowSize = new Size(900, 450), UseWebView = false };
    FormShowText.Execute(this, string.Format("Please Check Rules {0}Setup Log File", (pRemove ? "Un-" : "")), sb.ToString(), prm);


/Dmitry Bond (dmitry_bond@hotmail.com)/