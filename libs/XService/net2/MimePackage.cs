﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace XService.Utils
{
    /// <summary>
    /// Parser and holder of file content encoded as MIME/base64.
    /// This class can decode MIME/base64 encoded files (which created by TotalCommander or similar tools).
    /// But the difference is - all MIME/base64 code must be enclosed within root XML element! 
    /// XML-encapsulation is required to make it possible to save such file in db
    /// 
    /// Example of MIME header generated by TotalCommander:
    ///   MIME-Version: 1.0
    ///   Content-Type: application/octet-stream; name="QueryLogo_BY.png"
    ///   Content-Transfer-Encoding: base64
    ///   Content-Disposition: attachment; filename="QueryLogo_BY.png"
    /// </summary>
    public class MimePackage
    {
        public const string PREFIX_MimeVersion = "MIME-Version:";
        public const string PREFIX_ContentType = "Content-Type:";
        public const string NAME_Name = "name";
        public const string PREFIX_ContentTransferEncoding = "Content-Transfer-Encoding:";
        public const string PREFIX_ContentDisposition = "Content-Disposition:";
        public const string NAME_Filename = "filename";

        /// <summary>Load 1st MIME-encoded entity from specified file</summary>
        /// <returns>MimePackage object or null when enable to decode</returns>
        public static MimePackage Load(string pFilename)
        {
            FileInfo fi = new FileInfo(pFilename);
            if (fi.Exists)
                return Load(fi);
            return null;
        }

        /// <summary>Load 1st MIME-encoded entity from specified file</summary>
        /// <returns>MimePackage object or null when enable to decode</returns>
        public static MimePackage Load(FileInfo pFile)
        {
            XmlDocument dom = new XmlDocument();
            dom.Load(pFile.FullName);

            string text = StrUtils.AdjustLineBreaks(XmlUtils.LoadText(dom.DocumentElement), "\n");
            string[] lines = text.Split('\n');
            int iLine = 0;

            return Load(lines, ref iLine);
        }

        /// <summary>Load 1st MIME-encoded entity from specified file</summary>
        /// <param name="pLines">Array of lines to scan</param>
        /// <param name="pLineIndex">Ref: line index to start, it will return here line index where it stop</param>
        /// <returns>MimePackage object or null when enable to decode</returns>
        public static MimePackage Load(string[] pLines, ref int pLineIndex)
        {
            MimePackage result = new MimePackage();
            string pn, pv;
            int headerLevel = 0;
            int p, iLine = pLineIndex;
            while (iLine < pLines.Length)
            {
                string line = pLines[iLine];

                // when line is not empty
                if (!string.IsNullOrEmpty(line) && line.Trim(StrUtils.CH_SPACES).Length > 0)
                {
                    line = line.Trim(StrUtils.CH_SPACES);
                    switch (headerLevel)
                    {
                        case 0: // MIME-Version: 1.0
                            if (line.ToLower().StartsWith(PREFIX_MimeVersion.ToLower()))
                            {
                                line = line.Remove(0, PREFIX_MimeVersion.Length).Trim(StrUtils.CH_SPACES);
                                result.Header = line;
                            }
                            headerLevel++;
                            break;

                        case 1:
                            // Content-Type: application/octet-stream; name="QueryByLogo.png"
                            if (line.ToLower().StartsWith(PREFIX_ContentType.ToLower()))
                            {
                                line = line.Remove(0, PREFIX_ContentType.Length).Trim(StrUtils.CH_SPACES);
                                parseSpec(line, out result.ContentType, out pn, out pv);
                                if (pn != null && StrUtils.IsSameText(pn, NAME_Name))
                                    result.Name = pv;
                            }
                            // Content-Transfer-Encoding: base64
                            else if (line.ToLower().StartsWith(PREFIX_ContentTransferEncoding.ToLower()))
                            {
                                line = line.Remove(0, PREFIX_ContentTransferEncoding.Length).Trim(StrUtils.CH_SPACES);
                                result.TransferEncoding = line;
                            }
                            // Content-Disposition: attachment; filename="QueryByLogo.png"
                            else if (line.ToLower().StartsWith(PREFIX_ContentDisposition.ToLower()))
                            {
                                line = line.Remove(0, PREFIX_ContentDisposition.Length).Trim(StrUtils.CH_SPACES);
                                parseSpec(line, out result.Disposition, out pn, out pv);
                                if (pn != null && StrUtils.IsSameText(pn, NAME_Filename))
                                    result.Filename = pv;
                            }

                            // check if header fully loaded
                            if (result.ContentType != null && result.TransferEncoding != null && result.Disposition != null)
                                headerLevel++;
                            break;
                    }
                }
                else // when line is empty
                {
                    // MIME header assumed finished/broken after empty line!
                    if (headerLevel > 0)
                        break;
                }

                iLine++;
            }

            // check if we can continue with data loading...
            if (result.IsValidHeader)
            {
                // calculate total possible length...
                int totalLen = 0;
                for (int i = pLineIndex; i < pLines.Length; i++)
                    totalLen += pLines[i].Length;

                pLineIndex = iLine;

                bool isContentLatch = false;
                StringBuilder sb = new StringBuilder(totalLen);
                while (iLine < pLines.Length)
                {
                    string line = pLines[iLine].Trim(StrUtils.CH_SPACES);

                    // when line is not empty
                    if (!string.IsNullOrEmpty(line))
                    {
                        isContentLatch = true;
                        sb.Append(line);
                    }
                    else
                    {
                        // empty line after loaded data - it a condition to stop
                        if (isContentLatch)
                            break;
                    }

                    iLine++;
                }
                pLineIndex = iLine;

                result.Data = Convert.FromBase64String(sb.ToString());
            }
            else
                result = null;

            return result;
        }

        /// <summary>Load all MIME-encoded entities from specified file</summary>
        /// <returns>MimePackage[] array of decoded entities, when no entities decoded that will be an empty array</returns>
        public static MimePackage[] LoadMultiple(FileInfo pFile)
        {
            XmlDocument dom = new XmlDocument();
            dom.Load(pFile.FullName);

            string text = StrUtils.AdjustLineBreaks(XmlUtils.LoadText(dom.DocumentElement), "\n");
            string[] lines = text.Split('\n');
            int iLine = 0;
            return LoadMultiple(lines, ref iLine);
        }

        /// <summary>Load all MIME-encoded entities from specified file</summary>
        /// <param name="pLines">Array of lines to scan</param>
        /// <param name="pLineIndex">Ref: line index to start, it will return here line index where it stop</param>
        /// <returns>MimePackage[] array of decoded entities, when no entities decoded that will be an empty array</returns>
        public static MimePackage[] LoadMultiple(string[] pLines, ref int pLineIndex)
        {
            int iLine = 0;
            MimePackage pack = null;
            List<MimePackage> list = new List<MimePackage>();
            do
            {
                pack = Load(pLines, ref iLine);
                if (pack != null)
                    list.Add(pack);
            }
            while (pack != null);
            return list.ToArray();
        }

        public override string ToString()
        {
            string id = GetName();

            return String.Format("MimePack[ {0}; {1}; {2}; data={3} ]",
                StrUtils.FixNull(id)
                , StrUtils.FixNull(this.ContentType)
                , StrUtils.FixNull(this.TransferEncoding)
                , (this.Data != null ? string.Format("{0} bytes", this.Data.Length) : "(null)")
                );
        }

        public string Header;            // MIME-Version: 1.0

        public string ContentType;       // Content-Type: application/octet-stream; name="QueryByLogo.png"
        public string Name;

        public string TransferEncoding;  // Content-Transfer-Encoding: base64
        public string Disposition;       // Content-Disposition: attachment; filename="QueryByLogo.png"
        public string Filename;

        public string GetName()
        {
            return (this.Name != null ? this.Name : this.Filename);
        }

        public bool IsValidHeader
        {
            get
            {
                return (this.Header != null
                    && this.ContentType != null
                    && this.TransferEncoding != null
                    && this.Disposition != null
                    && (this.Name != null || this.Filename != null)
                    );
            }
        }

        public bool IsForced
        {
            get { return (this.Disposition != null && StrUtils.IsSameText(this.Disposition, "force")); }
        }

        public byte[] Data;

        /// <summary>Save file data into specified directory, keeping filename specified in MIME header</summary>
        /// <param name="pPath"></param>
        public void SaveToDirectory(string pPath)
        {
            if (this.Data == null)
                throw new XServiceError(string.Format("Data is missing for: {0}", this.ToString()));

            string fn = GetName();
            if (string.IsNullOrEmpty(fn))
                throw new XServiceError(string.Format("Filename is not specified for: {0}", this.ToString()));

            if (!Directory.Exists(pPath))
                Directory.CreateDirectory(pPath);
            if (Directory.Exists(pPath))
            {
                using (FileStream fs = new FileStream(PathUtils.IncludeTrailingSlash(pPath) + fn, FileMode.Create))
                {
                    if (this.Data.Length > 0)
                        fs.Write(this.Data, 0, this.Data.Length);
                    fs.SetLength(this.Data.Length);
                    fs.Flush();
                }
            }
        }

        #region Implementation details

        protected static void parseSpec(string pText, out string pValue, out string pSpecKey, out string pSpecValue)
        {
            pSpecKey = pSpecValue = null;

            int p = pText.IndexOf(";");
            pValue = (p >= 0 ? pText.Substring(0, p) : pText);
            if (p >= 0)
            {
                pText = pText.Remove(0, p + 1).Trim(StrUtils.CH_SPACES);
                p = pText.IndexOf("=");
                if (p >= 0)
                {
                    pSpecKey = pText.Substring(0, p).Trim(StrUtils.CH_SPACES);
                    pSpecValue = pText.Remove(0, p + 1).Trim(StrUtils.CH_SPACES);
                    if (pSpecValue.StartsWith("\"") && pSpecValue.EndsWith("\""))
                        pSpecValue = pSpecValue.Remove(pSpecValue.Length - 1, 1).Remove(0, 1);
                }
            }
        }

        #endregion // Implementation details
    }

}