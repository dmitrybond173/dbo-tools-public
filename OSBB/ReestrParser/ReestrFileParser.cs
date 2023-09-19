/* 
 * Reestr Parser app.
 *
 * Parser halper class.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Aug, 2023
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using XService.Utils;
using BTO.Compatibility.Borland.Delphi5;

namespace ReestrParser
{
    public class ReestrFileParser : AdvStringReader
    {
        public static TraceSwitch TrcLvl { get { return ToolSettings.TrcLvl; } }

        public ReestrFileParser(string pText)
            : base(pText)
        {
        }

        #region Properties

        public bool IsSpace
        {
            get 
            {
                char ch = (char)this.Lookup();
                return (_SPACES.IndexOf(ch) >= 0);
            }
        }

        public bool IsSplit
        {
            get
            {
                char ch = (char)this.Lookup();
                char ch1 = (char)this.Lookup(1);
                return ( ch == '-' && ch1 == '-' );
            }
        }

        public bool IsEOL
        {
            get
            {
                char ch = (char)this.Lookup();
                char ch1 = (char)this.Lookup(1);
                return ( (ch == '\r' && ch1 == '\n' ) || ch == '\n' );
            }
        }

        #endregion // Properties

        public void SkipSpaces()
        {
            while (!this.EOF && this.IsSpace)
                this.Read();
        }

        public void SkipLine()
        {
            while (!this.EOF && !this.IsEOL)
                this.Read();
            if (!this.EOF)
                ExpectEOL();
        }

        public void ExpectEOL()
        {
            if (!this.IsEOL)
                ThrowError("EOL expected");

            SkipWhile(_EOL);
        }

        public void ExpectSplitLine()
        {
            if (!this.IsSplit)
                ThrowError("splitter expected");

            SkipSpaces();
            SkipWhile("-");
            SkipSpaces();

            if (!(this.IsEOL || this.EOF))
                ThrowError("EOL or EOF expected");
            if (!this.EOF)
                ExpectEOL();
        }

        public void SkipToSplitLine()
        {
            while (!this.IsSplit)
                SkipLine();
        }

        public int ExpectMarker(string[] pMarkers)
        {
            bool result = false;
            int idx = 0;
            foreach (string mrk in pMarkers)
            {
                string txt = GetNChars(mrk.Length);
                result = StrUtils.IsSameText(txt, mrk);
                if (result)
                    break;
                
                Unread(mrk.Length);
                idx++;
            }
            if (!result)
                ThrowError(string.Format("expected text: [{0}]", StrUtils.Join(pMarkers, "] or [")));

            SkipSpaces();
            return idx;
        }

        public void ExpectMarker(string pMarker)
        {
            string txt = GetNChars(pMarker.Length);
            bool result = StrUtils.IsSameText(txt, pMarker);
            if (!result)
            {
                Unread(pMarker.Length);
                ThrowError(string.Format("expected text: {0}", pMarker));
            }

            SkipSpaces();
        }

        public string ExpectValueFormat(Regex pRexp, int pLength)
        { 
            string txt = GetNChars(pLength);
            if (!pRexp.IsMatch(txt))
            {
                Unread(pLength);
                ThrowError(string.Format("expect formated value: {0}", pRexp));
            }
            return txt;
        }

        public bool HasMarker(string pMarker)
        { 
            string txt = GetNChars(pMarker.Length);
            bool result = StrUtils.IsSameText(txt, pMarker);
            Unread(pMarker.Length);
            return result;
        }

        public string ExtractToMarker(string[] pMarkers)
        {
            int pos = this.index;
            bool hasMrk = false;
            string actualMrk = null;
            while (!this.EOF && !this.IsEOL)
            {
                hasMrk = false;
                foreach (string mrk in pMarkers)
                {
                    if ((hasMrk = HasMarker(mrk)))
                    {
                        actualMrk = mrk;
                        break;
                    }
                }
                if (hasMrk)
                    break;
                char ch = (char)read();
            }
            string txt = this.data.Substring(pos, this.index - pos);
            if (hasMrk)
                ExpectMarker(actualMrk);
            return txt;
        }

        public string ExtractToMarker(string pMarker)
        {
            int pos = this.index;
            bool hasMrk = false;
            while (!this.EOF && !this.IsEOL)
            {
                if ((hasMrk = HasMarker(pMarker)))
                    break;
                char ch = (char)read();
            }
            string txt = this.data.Substring(pos, this.index - pos);
            if (hasMrk)
                ExpectMarker(pMarker);
            return txt;
        }

        public string GetFixedLengthValue(int pLength)
        {
            //return GetNChars(pLength);

            int pos = this.index;
            int N = pLength;
            while (!this.EOF && N > 0)
            {
                char ch = (char)read();
                if (this.IsEOL)
                    ThrowError("unexpect EOL inside value");
                N--;
            }
            return this.data.Substring(pos, this.index - pos);
        }

        #region Implementation details

        private static string _SPACES = "\t "; // we have to exclude \r\n from spaced in this case!
        private static string _EOL = "\r\n"; 

        #endregion // Implementation details
    }
}
