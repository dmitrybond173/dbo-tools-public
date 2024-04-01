/*
 * Call-back TraceListener by Dmitry Bond.
 * Date May 11, 2006
 */
namespace System.Diagnostics
{
    using System;
    using System.Diagnostics;
    using XService.Utils;

    /// <summary>
    /// Summary description for MyTraceListener.
    /// </summary>
    public class CallbackTraceListener : TraceListener
    {
        public delegate void OnWriteProc(string message, bool WriteLine);

        public static OnWriteProc OnWrite = null;

        public CallbackTraceListener()
            : base()
        {
        }

        public override void Write(string message)
        {
            if (OnWrite != null)
                OnWrite(message, false);
        }        

        public override void WriteLine(string message)
        {
            int identLen = this.IndentLevel * this.IndentSize;
            if (identLen != this.identStr.Length)
            {
                if (identLen <= 0)
                    this.identStr = "";
                else
                    this.identStr = StrUtils.Strng(" ", identLen);
            }
            if (this.identStr.Length > 0)
                message = this.identStr + message;

            if (OnWrite != null)
                OnWrite(message, true);
        }

        protected string identStr = "";
    }
}
