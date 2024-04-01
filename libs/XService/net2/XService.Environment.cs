/*
 * Some utlitities for application environment handling.
 * Written by Dmitry Bond. at June 15, 2012
 */

using System;
using System.Configuration;
using System.Text;

namespace XService.Utils
{
    public class EnvironmentUtils
    {
        /// <summary>
        /// Returns true if application is running on Windows platform
        /// </summary>
        public static bool IsWindowsPlatform
        {
            get
            {
                string s = ConfigurationManager.AppSettings["Platform"];
                if (!string.IsNullOrEmpty(s))
                {
                    bool isWindows = (
                        string.Compare(s, "Windows", true) == 0
                        || string.Compare(s, "WinNT", true) == 0
                        || string.Compare(s, "Win32", true) == 0
                        || string.Compare(s, "Win32s", true) == 0
                        );
                    return isWindows;
                }
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT: return true;
                    case PlatformID.Win32S: return true;
                    case PlatformID.Win32Windows: return true;
                    default:
                        return false;
                }
            }
        }

    }
}
