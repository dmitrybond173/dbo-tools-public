/*
 * Some utlitities to facilitate debugging of applications.
 * Written by Dmitry Bond. at June 14, 2006
 */

using System;
using System.Diagnostics;

namespace XService.Utils
{
    /// <summary>
    /// Summary description for DbgHelper.
    /// </summary>
    public sealed class DbgHelper
    {
        public static bool SplitItemsByNewLine = false;

        public enum ELoggerLineState
        {
            Information,
            Success,
            Warning,
            Error
        }

        public delegate void OnLoggerProc(ELoggerLineState ALineState, string AMsg);

        public delegate void LogOutputMethod(TraceLevel pLogLevel, string AMsg);
    }
}
