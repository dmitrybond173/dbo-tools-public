/* 
 * Reestr Parser app.
 *
 * Entry point for app.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Aug, 2023
 */

//using System.Data.SQLite;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime;
using System.Xml;
using System.Windows.Forms;
using XService.Utils;

namespace ReestrParser
{
    public class Program
    {
        public static TraceSwitch TrcLvl { get { return ToolSettings.TrcLvl; } }

        [STAThread]
        public static int Main(string[] args)
        {
            //Console.WriteLine("#chk.pnt.001");
            // WARNING!
            // To make Trace.Write*() calls working - you need to ensure TRACE #define name is specified for whole C# project!

            ToolSettings.Arguments = args;
            //CallbackTraceListener.OnWrite += onTraceWrite;

            Program tool = new Program();
            return tool.Run(args);
        }

        Program()
        {
            ToolSettings.DumpTraceListeners();
        }


        public int Run(string[] args)
        {
            this.settings.DisplayStartHeader();

            //Console.WriteLine("#chk.pnt.002");
            try
            {
                if (!this.settings.ParseCmdLine(args))
                    return 1;

                //Console.WriteLine("#chk.pnt.003");
                this.engine = new ParserEngine(this.settings);

                if ((this.settings.Pause & ToolSettings.EPause.Begin) == ToolSettings.EPause.Begin)
                { 
                    Trace.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }

                //Console.WriteLine("#chk.pnt.004");
                if (this.settings.Outputs.Contains("db"))
                {
                    Trace.WriteLine("Validate db connectivity...");
                    this.engine.OpenDb();
                }

                if (this.settings.UiMode)
                    runUiMode();
                else
                    runConsoleMode();

                //Console.WriteLine("#chk.pnt.006-ok");

                if ((this.settings.Pause & ToolSettings.EPause.End) == ToolSettings.EPause.End)
                {
                    Trace.WriteLine("Press any key to finish...");
                    Console.ReadKey();
                }
            }
            catch (Exception exc)
            {
                //Console.WriteLine("#chk.pnt.ERR");

                Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format(
                    "!ERR: {0}\nat {1}", ErrorUtils.FormatErrorMsg(exc), ErrorUtils.FormatStackTrace(exc)) : "");
                if (exc.InnerException != null)
                    Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format(
                        "  !ERR.internal: {0}", ErrorUtils.UnrollException(exc)) : "");

                if ((this.settings.Pause & ToolSettings.EPause.Error) == ToolSettings.EPause.Error)
                {
                    Trace.WriteLine("Press any key to abort...");
                    Console.ReadKey();
                }
            }

            return 0;
        }

        /* private static void onTraceWrite(string pMessage, bool pWriteLine)
        {
            if (pWriteLine)
                Console.WriteLine(pMessage);
            else
                Console.Write(pMessage);
        } */

        private void runUiMode()
        {
            FormAppUI frm = new FormAppUI();
            frm.Settings = this.settings;
            //frm.SetEngineRef(this);
            Application.Run(frm);
        }

        private void runConsoleMode()
        {
            this.engine.Parse();
            this.engine.GenerateOutput();
        }

        protected ToolSettings settings = new ToolSettings();

        protected ParserEngine engine;
    }
}
