using System;
using System.Collections.Generic;
using System.Text;

namespace XService.Utils
{
    public sealed class CommonUtils
    {
        public static string Bytes2Hex(byte[] arr)
        {
            StringBuilder sb = new StringBuilder(4 + arr.Length * 2);
            foreach (byte b in arr)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }


    /// <summary>
    /// StrUtils - utilities to manipulate strings.
    /// </summary>
    public sealed class StrUtils
    {
        public static bool GetAsBool(string s)
        {
            s = s.Trim().ToLower();
            return (s == "yes" || s == "true" || s == "1");
        }

        public static string Right(string s, int n)
        {
            return s.Substring(s.Length-n, n);
        }

        public static string IncludeTrailing(string str, char ch)
        {
            return IncludeTrailing(str, "" + ch);
        }

        public static string IncludeTrailing(string str, string pattern)
        {
            if (!str.EndsWith(pattern))
                return str + pattern;
            return str;
        }

        public static string ExcludeTrailing(string str, char ch)
        {
            return ExcludeTrailing(str, "" + ch);
        }

        public static string ExcludeTrailing(string str, string pattern)
        {
            if (str.EndsWith(pattern))
                return str.Remove(str.Length - pattern.Length, pattern.Length);
            return str;
        }

    } /* StrUtils */
}
