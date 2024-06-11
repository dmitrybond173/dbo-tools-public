/* 
 * Borland Delphi 5 Compatibility Toolkit.
 * Written by Dmitry Bond. at Aug, 2009
 */

using System;
using System.Collections.Generic;
using XService.Utils;

[assembly: CustomVersionInfo("RCS:D5ParserServices.cs", "$Date: 2021/05/31 10:09:41Z $ $RCSfile: D5ParserServices.cs $ $State: Exp $ $Author: dbondare $ $Revision: 1.3 $ $Locker: $ $Name: $")]

namespace XService.Parsers
{
    /// <summary>
    /// ParserError
    /// </summary>
    public class ParserError : Exception
    {
        public ParserError(string message)
            : base(message)
        {
        }
    }


    /// <summary>
    /// AdvStringReader - base class for text parsers
    /// </summary>
    public class AdvStringReader
    {
        public AdvStringReader(string AData)
        {
            this.data = AData;
            if (this.data == null)
                this.data = string.Empty;
        }

        public override string ToString()
        {
            return String.Format("{0}[pos={1}; sz={2}; bof={3}; eof={4}]: [{5}]",
                this.GetType().Name, this.Position, this.Size, this.BOF, this.EOF, PreviewStr(100));
        }

        #region Interface

        public bool BOF
        {
            get { return (this.index == 0); }
        }

        public bool EOF
        {
            get { return (this.index >= this.data.Length); }
        }

        public int Position
        {
            get { return this.index; }
        }

        public int Size
        {
            get { return this.data.Length; }
        }

        public bool Is(char pCh)
        {
            return (pCh == (char)this.Lookup());
        }

        public void Close()
        {
            this.data = string.Empty;
            this.index = 0;
            this.marker = -1;
        }

        public void Reset()
        {
            this.index = 0;
            this.marker = -1;
        }

        public void SetMarker()
        {
            this.marker = this.index;
        }

        public void BackToMarker()
        {
            if (this.marker >= 0)
            {
                this.index = this.marker;
                this.marker = -1;
            }
            ThrowError("no marker set");
        }

        public void SetMarker(char pId)
        {
            this.markers[pId] = this.index;
        }

        public void BackToMarker(char pId)
        {
            int mrk;
            if (this.markers.TryGetValue(pId, out mrk))
            {
                this.index = mrk;
                this.markers.Remove(pId);
                return;
            }
            ThrowError(string.Format("no \'{0}\' marker set", pId));
        }

        public string GetMarked()
        {
            if (this.marker < 0)
                ThrowError("no marker set");

            int pos = this.marker;
            this.marker = -1;
            if (pos > this.index) return null;
            return this.data.Substring(pos, this.index - pos);
        }

        public string GetMarked(char pId)
        {
            int pos;
            if (!this.markers.TryGetValue(pId, out pos))
                ThrowError(string.Format("no \'{0}\' marker set", pId));

            this.markers.Remove(pId);
            if (pos > this.index) return null;

            return this.data.Substring(pos, this.index - pos);
        }

        public int Lookup()
        {
            if (this.index < this.data.Length)
                return this.data[this.index];
            return -1;
        }

        public int Lookup(int N)
        {
            if (this.index + N < this.data.Length)
                return this.data[this.index + N];
            return -1;
        }

        public string PreviewStr(int pDepth)
        {
            string result = "";
            int idx = 0;
            char ch;
            while (idx < pDepth && (ch = (char)Lookup(idx)) >= 0)
            {
                result += ch;
                idx++;
            }
            return result;
        }

        public int Read()
        {
            if (this.EOF)
                ThrowError("attempt to read after the end of expression");
            return this.data[this.index++];
        }

        public int Skip(int N)
        {
            int ch = -1;
            while (!this.EOF && N > 0)
            {
                ch = read();
                N--;
            }
            return ch;
        }

        public string ReadToEnd()
        {
            string text = "";
            while (!this.EOF)
            {
                text += (char)Read();
            }
            return text;
        }

        public string Read(int N)
        {
            int saved_N = N;
            int pos = this.index;
            while (!this.EOF && N > 0)
            {
                read();
                N--;
            }
            if (N == 0)
                return this.data.Substring(pos, this.index - pos);

            ThrowError(String.Format(
                "read error - no {0} chars in expression from pos {1}",
                saved_N, pos));
            return string.Empty; // this code is to disable compiler warning
        }

        public void Unread()
        {
            if (this.index > 0)
                this.index--;
        }

        public void Unread(int N)
        {
            while (!this.BOF && N > 0)
            {
                read();
                N--;
            }
        }

        public string GetNChars(int N)
        {
            int pos = this.index;
            while (!this.EOF && N > 0)
            {
                read();
                N--;
            }
            return this.data.Substring(pos, this.index - pos);
        }

        public int SkipWhile(string chars)
        {
            int saved_i = this.index;
            while (!this.EOF && chars.IndexOf(this.data[this.index]) >= 0)
                this.index++;
            return (this.index - saved_i);
        }

        public int SkipUntil(string chars)
        {
            int saved_i = this.index;
            while (!this.EOF && chars.IndexOf(this.data[this.index]) < 0)
                this.index++;
            return (this.index - saved_i);
        }

        protected virtual void ThrowError(string pMessage)
        {
            throw new ParserError(pMessage + string.Format(" at pos {0}: {1}", this.Position, PreviewStr(40)));
        }

        #endregion // Interface

        #region Implementation

        private int read()
        {
            return this.data[this.index++];
        }

        protected string data = string.Empty;
        protected int index = 0;
        protected int marker = -1;
        protected Dictionary<char, int> markers = new Dictionary<char, int>();

        #endregion // Implementation

    }
}
