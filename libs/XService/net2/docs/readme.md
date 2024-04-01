
Just a small 'gentleman set' of typical routinues for small applications.

This package includes VS-generated documentaiton, so this readme file contains only some typically used highlights.

Example of trace configuration in app.config

    <system.diagnostics>
        <switches>
          <add name="TraceLevel" value="4"/>
          <add name="DacService" value="3"/>
          <add name="XService" value="2"/>
          <add name="XService.UI" value="3"/>
        </switches>
        <trace autoflush="true">
            <listeners>
                <add name="defaultLogger" type="System.Diagnostics.AdvancedTraceListener, XService.Net2" 
                     initializeData="%TEMP%\$$(AppName)_.log;LinePrefix=${TID};TimeRouteFilenamePattern=yyyyMMdd;CleanupOlderThan=7days"/>
                <add name="uiLogger" type="System.Diagnostics.CallbackTraceListener, XService.Net2"/>
                <add name="consoleLogger" type="System.Diagnostics.ConsoleTraceListener, XService.Net2"/>
            </listeners>
        </trace>
    </system.diagnostics>

If need to organize a logger panel in UI (ex: with ListView UI control) then it could be like this:

        /// <summary>Write message to Logger UI.</summary>
        private void writeLogger(string pMessage, bool pWriteLine)
        {
            string ts = StrUtils.NskTimestampOf(DateTime.Now).Substring(11, 12);

            // HostAction from SqlApi can make UI hang! So, we have to filter out some types of messages here
            bool isFlood = pMessage.Contains("SqlApiErgs[ ReceivingData");
            if (isFlood) 
                return ;

            lock (this.lvLogger)
            {
                while (lvLogger.Items.Count > BtoAppKernel.MaxUiLoggerItems)
                {
                    lvLogger.Items.RemoveAt(0);
                }

                ListViewItem li = null;
                if (!pWriteLine)
                {
                    if (lvLogger.Items.Count > 0)
                    {
                        li = lvLogger.Items[lvLogger.Items.Count - 1];
                        li.SubItems[0].Text = ts;
                        li.SubItems[1].Text += pMessage;
                    }
                    else
                        pWriteLine = true;
                }
                if (pWriteLine)
                {
                    if (pMessage.IndexOf('\n') >= 0)
                    {
                        string[] lines = pMessage.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                        foreach (string line in lines)
                        {
                            li = new ListViewItem(new string[] { ts, line });
                            lvLogger.Items.Add(li);
                        }
                    }
                    else
                    {
                        li = new ListViewItem(new string[] { ts, pMessage });
                        lvLogger.Items.Add(li);
                    }
                }
                if (li != null)
                    li.EnsureVisible();
            }
        }

        /// <summary>Callback method used to send message to Logger UI from Trace</summary>
        //private IAsyncResult ar_onTraceWrite;
        private void onTraceWrite(string pMessage, bool pWriteLine)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new CallbackTraceListener.OnWriteProc(this.writeLogger), pMessage, pWriteLine);
            }
            else
            {
                this.writeLogger(pMessage, pWriteLine);
            }
        }

        /// <summary>Switch Logger UI panel ON/OFF</summary>
        private void toggleLoggerPanel(bool pNewState)
        {
            if (pNewState)
            {
                spltLogger.Enabled = true; spltLogger.Visible = true;
                panLogger.Enabled = true; panLogger.Visible = true;
                if (Settings.Default.LogPanel == 0) Settings.Default.LogPanel = 130;
                panLogger.Height = Settings.Default.LogPanel;
            }
            else
            {
                Settings.Default.LogPanel = 0;
                spltLogger.Enabled = false; spltLogger.Visible = false;
                panLogger.Enabled = false; panLogger.Visible = false;
            }
            mmiWinLogPanel.Checked = pNewState;
            if (panLogger.Height != Settings.Default.LogPanel)
                Settings.Default.Save();
        }


When need a basic INI-style section in app.config then you can do it like this:

    <configSections>
        <section name="UsefulLinks" type="XService.Configuration.IniParameters, XService.Net2"/>
        [...]
    </configSections>

    [...]

    <UsefulLinks caseSensitive="true">
        ADO.NET = http://en.wikipedia.org/wiki/ADO.NET
        Comparison of ADO and ADO.NET = http://en.wikipedia.org/wiki/Comparison_of_ADO_and_ADO.NET
        Connection Strings = http://ConnectionStrings.com
    </UsefulLinks>

When need a free-structure XML in your app.config then you can do it like this:

    <configSections>
      <section name="Services" type="XService.Configuration.ConfigXml, XService.Net2" />
      [...]
    </configSections>
    
    [...]
    
    <Services>
    
      <!-- Section to place parameters which are common for all services -->
      <CommonParameters>
        DisplayName = BTO SF Srvc ($(Model)/$(Plant):$(RemoteControlPort)/$(id))
        BtoEnvironment = btoDevVm2012:20203, ABX/P001
        ServiceStartType = Automatic
        ServiceAccount = LocalSystem
        ServiceUser = $(MachineName)\dmitry
      </CommonParameters>
    
      <Service name="BtoSf-ABX">
        id = ABX
        ConfigFilename = $(HomePath)env\$(id)\conf_Sf.xml
        BtoEnvironment = -, ABX/P001
        RemoteControlPort = 8050
      </Service>
    
      <Service name="BtoSf-HBDT">
        id = HBDT
        ConfigFilename = $(HomePath)env\$(id)\conf_Sf.xml
        BtoEnvironment = -, ABX/P001
        RemoteControlPort = 8051
      </Service>
    
    </Services>

Example of ScriptGenerator usage:

    XmlElement node = (XmlElement)ConfigurationManager.GetSection("ScriptTemplates");
    ScriptGenerator scripts = new ScriptGenerator(node);
    scripts.OnGetMacroValue += this.getScpPrmValue;

    string scriptTemplateName = "Export";
    string id = scriptTemplateName;
    scripts.Start(id);
    scripts.AddHeader();
    for (int i = 0; i < tables.Count; i++)
    {
        this.scpItemNo++;
        this.scpCurrentTable = tables[i];
        scripts.AddItem(tables[i], null);
    }
    scripts.AddFooter();
    string txt = scripts.Finish();

    string ts = StrUtils.CompactNskTimestampOf(DateTime.Now).Substring(0, 19);
    string scriptFn = Environment.ExpandEnvironmentVariables(
        string.Format("%TEMP%\\testScriptGen-{0}.txt", ts));
    using (StreamWriter sw = File.CreateText(scriptFn))
    {
        sw.WriteLine(txt);
    }

And here is example of <ScriptTemplates/> section in app.config:

    <!--  
      Another key piece of configuration - templates of scripts.
      These templates used to generate actual data export script.
      Each script is build as {Header} + N*{Item} + {Footer}.
    -->
    <ScriptTemplates>
    
      <Template name="ConnectDB">
        <Header>
          <![CDATA[
        @echo off
        set BTO_DbName=$(DbName)
        set BTO_DbUser=$(DbUser)
        set BTO_DbPwd=$(DbPassword)
        db2 connect to %BTO_DbName% user %BTO_DbUser% using %BTO_DbPwd%
        echo Connected at %DATE%, %TIME%
      ]]>
        </Header>
      </Template>
    
      <Template name="Export">
        <Header><![CDATA[
          @echo off
      
          set name=$(PackageName)
          set Format=IXF
          set condition=
          set accessSpec=for read only
          
          call "$(BtoHome)\bind\connectDb.bat"
      
          set path=$(AppHome);%~dp0;%path%
          cd %~dp0
          md %name%
          cd %name%
                
          DbTabAdoNet.exe -nc -dbii -d=%BTO_DbName% -u=%BTO_DbUser% -p=%BTO_DbPwd% dbtab.h 
      
          set db2cmd=db2
        
        ]]></Header>
    
        <Item><![CDATA[
          echo.
          set TBL=$(ItemName)
          set condition=$(ItemCondition)
          echo ----- [%TBL%] ---------------------------------------------
          title "$(ItemNo) of $(ItemsCount): Exporting [%name% -> %TBL% of %condition%]"
          %db2cmd% export to %TBL%.%Format% of %Format% select * from %TBL% %condition% %accessSpec%
        
        ]]></Item>
    
        <Footer><![CDATA[      
          cd ..
    
          set cmd=start /min /low rar m -r -s -mdG %name%_ %name%\*.*
          echo %cmd%
          %cmd%
        
        ]]></Footer>
      </Template>
    
    </ScriptTemplates>



/Dmitry Bond (dmitry_bond@hotmail.com)/