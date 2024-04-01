/*
 * Console TraceListener by Dmitry Bond.
 * Date August 24, 2005
 */

namespace System.Diagnostics
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Web;
    using XService.Utils;

    /// <summary>
    /// Summary description for MyTraceListener.
    /// </summary>
    public class ConsoleTraceListener : TextWriterTraceListener
    {
        public ConsoleTraceListener()
            : base()
        {
        }

        public ConsoleTraceListener(string AFilename)
            : base()
        {
            this.filename = AFilename;
            parseParameters();
        }

        ~ConsoleTraceListener()
        {
        }

        #region Interface

        public override void Write(string message)
        {
            if (this.useStdErr)
                Console.Error.Write(message);
            else
                Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            if (this.useStdErr)
                Console.Error.WriteLine(message);
            else
                Console.WriteLine(message);
        }

        #endregion // Interface

        private void parseParameters()
        { 
            Dictionary<string, string> props = new Dictionary<string, string>();
            StrUtils.ParseLop(this.filename, props, true);
            foreach (KeyValuePair<string, string> prop in props)
            {
                if (StrUtils.IsSameText(prop.Key, "StdErr") || StrUtils.IsSameText(prop.Key, "UseStdErr"))
                {
                    this.useStdErr = true;
                }
            }
        }

        private string filename;
        private bool useStdErr = false;
    }
}
