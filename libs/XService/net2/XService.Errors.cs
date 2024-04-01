/*
 * Base classes for exceptions.
 * Written by Dmitry Bond. at June 14, 2006
 */

using System;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;

namespace XService.Utils
{
    /// <summary>
    /// ToolError
    /// To present all tool errors which are not classified as any other error class
    /// </summary>
    public class ToolError : Exception
    {
        public ToolError(string message)
            : base(message)
        {
        }

        public ToolError(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }


    /// <summary>
    /// ToolConfigError
    /// To present errors which appears at configuration loading stage
    /// </summary>
    public class ToolConfigError : ToolError
    {
        public ToolConfigError(string message)
            : base(message)
        {
        }

        public ToolConfigError(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }


    /// <summary>
    /// ObjectError
    /// To present errors which is related to certain object
    /// </summary>
    public class ObjectError : ToolError
    {
        public ObjectError(string pMessage, object pObject)
            : base(pMessage)
        {
            ErrorObject = pObject;
        }

        public ObjectError(string message, object pObject, Exception innerException)
            : base(message, innerException)
        {
            ErrorObject = pObject;
        }

        public object ErrorObject;
    }


    public class LicenseKeyError : ToolConfigError
    {
        public LicenseKeyError(string message)
            : base(message)
        {
        }

        public LicenseKeyError(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }


    /// <summary>
    /// StrUtils - utilities to manipulate Exceptions.
    /// </summary>
    public sealed class ErrorUtils
    {
        /// <summary>Prepare a string which contains info of specified exception and full chain of all internal exceptions</summary>
        public static Exception UnrollException(Exception exc)
        {
            while (exc.InnerException != null)
                exc = exc.InnerException;
            return exc;
        }

        /// <summary>Prepare a string which contains info of specified exception and full chain of all internal exceptions</summary>
        public static string UnrollExceptionMsg(Exception exc, bool includeTypeInfo)
        {
            string sMsg = (includeTypeInfo ? (exc.GetType().ToString() + ": ") : "") + exc.Message; 
            while (exc.InnerException != null)
            {
                exc = exc.InnerException;
                sMsg += (" / " 
                    + (includeTypeInfo ? (exc.GetType().ToString() + ": ") : "") 
                    + exc.Message);                
            }
            return sMsg;
        }

        /// <summary>Return a string which describing specified exception (exception type name + exception message)</summary>
        public static string FormatErrorMsg(Exception exc)
        {
            return String.Format(
                "ERR({0}): {1}{2}", exc.GetType().ToString(), exc.Message,
                (exc.InnerException != null ? " (+internal err!)" : "")
                );
        }

        /// <summary>Return formated string describing stack trace for specified exception</summary>
        public static string FormatStackTrace(Exception exc)
        {
            if (exc.StackTrace == null)
                return "-n/a-";
            return exc.StackTrace.Replace("\n", "\n\t");
        }

        /// <summary>Write exception info into trace log</summary>
        public static void LogException(Exception exc, string pMethodId, TraceSwitch TrcLvl)
        {
            LogException(exc, pMethodId, TrcLvl.TraceError);
        }

        /// <summary>Write exception info into trace log</summary>
        public static void LogException(Exception exc, string pMethodId, bool pIsVisible)
        {
            if (!pIsVisible) return;

            string info = "";
            if (exc is SocketException)
            {
                SocketException se = (SocketException)exc;
                info = string.Format("winErr:{0}, sockErr:{1}/{2}, osErr:{3}",
                    se.ErrorCode, se.SocketErrorCode, (int)se.SocketErrorCode, se.NativeErrorCode);
                if (se.Data != null && se.Data.Count > 0)
                {
                    info += string.Format(", {0} items=(", se.Data.Count);
                    foreach (DictionaryEntry de in se.Data)
                    {
                        info += string.Format("{0}=[{1}],", de.Key, de.Value);
                    }
                    info += ")";
                }
            }

            Trace.WriteLine(string.Format("{0}.ERR({1}): {2} [{3}]\nat{4}", pMethodId, exc.GetType(), exc.Message, info, exc.StackTrace));
            int errLvl = 0;
            while (exc.InnerException != null)
            {
                errLvl++;
                exc = exc.InnerException;
                Trace.WriteLineIf(pIsVisible, pIsVisible ? string.Format(
                    "{0}[#{1}].ERR({2}): {3}\nat{4}", pMethodId, errLvl, exc.GetType(), exc.Message, exc.StackTrace) : "");
            }
        }

    }

}
