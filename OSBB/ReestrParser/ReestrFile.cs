/* 
 * Reestr Parser app.
 *
 * Holder and parser of data of particular bank reestr file.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Aug, 2023
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using XService.Utils;
using BTO.Compatibility.Borland.Delphi5;

namespace ReestrParser
{
    public class ReestrFile
    {
        public static TraceSwitch TrcLvl { get { return ToolSettings.TrcLvl; } }

        public const string DEFAULT_PoDate_Null = "00.00.00";
        public const float MIN_AllowableFloatDelta = 0.005f; // 0.5 копеек

        public static int ID = 0;

        public static ReestrFile Parse(FileInfo pFile)
        {
            ReestrFile result = new ReestrFile(pFile.FullName);
            result.Parse();
            return result;
        }

        public ReestrFile(string pFilename)
        {
            ID++;
            this.db_id = ID;
            this.Filename = pFilename;
            this.Items = new List<ReestrItem>();
        }

        public override string ToString()
        {
            return String.Format(
                "ReestrFile[{0}; db:[{1}; ({2}/{3}/{4}); ({5}/{6}/{7}); {8}/{9}; {10}/{11}]]: {12} or {13} items; total:{14}/{15}/{16}", 
                Path.GetFileName(this.Filename),
                this.db_date,
                this.db_sndBank, this.db_sndCode, this.db_sndAccount,
                this.db_rcvBank, this.db_rcvCode, this.db_rcvAccount,
                this.db_reestrName, this.db_reestrDate,
                this.db_po, this.db_poDate,
                this.Items.Count, this.db_totalItems,
                this.db_totalSum.ToString("N2"), this.db_totalCommission.ToString("N2"), this.db_totalConfirmed.ToString("N2")
                );
        }

        #region Properties

        public ReestrFileParser Parser { get; protected set; }

        public string Filename { get; protected set; }
        public List<ReestrItem> Items { get; protected set; }

        public int db_id;
        public string db_lang = ""; 
        public string db_date; 
        public string db_sndBank;
        public string db_sndCode;
        public string db_sndAccount;
        public string db_rcvBank;
        public string db_rcvCode;
        public string db_rcvAccount;
        public string db_reestrName;
        public string db_reestrDate;
        public string db_po;
        public string db_poDate;
        public string db_totalAccNo;
        public int db_totalItems;
        public float db_totalCommission;
        public float db_totalSum;
        public float db_totalConfirmed;

        #endregion // Properties

        public bool Parse()
        {
            return performParse();
        }

        public bool Validate()
        {
            bool isOk = (this.db_totalItems == this.Items.Count);
            if (isOk)
            {
                float actualSum = 0.0f;
                float actualCommission = 0.0f;
                foreach (ReestrFile.ReestrItem it in this.Items)
                {
                    actualSum += it.db_amount;
                    actualCommission += it.db_commission;
                }

                // WARNING: this works incorrectly in Intel CPUs!
                // Because of very small diff in FP-numbers
                //isOk = (this.db_totalSum == actualSum)
                //    && (this.db_totalCommission == actualCommission);

                // Note: due to mistales in Intel FP-processing we have to use 'delta' to validate numbers
                float delta_Sum = Math.Abs(this.db_totalSum - actualSum);
                float delta_Commission = Math.Abs(this.db_totalCommission - actualCommission);
                isOk = (delta_Sum < MIN_AllowableFloatDelta && delta_Commission < MIN_AllowableFloatDelta);
            }
            return isOk;
        }

        #region Implementation details

        protected bool performParse()
        {
            string txt = "";
            using (StreamReader sr = new StreamReader(this.Filename))
                txt = sr.ReadToEnd();

            ReestrFileParser prs = new ReestrFileParser(txt);
            this.Parser = prs;

            string s;
            //double x;

            prs.ExpectSplitLine();

            int langIdx = prs.ExpectMarker(new string[] { "Дата створення звіту:", "Дата создания отчета:" });
            switch (langIdx)
            {
                case 0: this.db_lang = "UA"; break;
                case 1: this.db_lang = "RU"; break;
                default: this.db_lang = "??"; break;
            }
            this.db_date = prs.ExpectValueFormat(rexp_Timestamp, 17).TrimEnd(); // 08.01.22 05:25:43
            prs.SkipSpaces();
            prs.ExpectEOL();

            // row sender
            prs.ExpectMarker(new string[] { "Банк платн.:", "Банк плат.:" });
            prs.SkipSpaces();
            this.db_sndBank = prs.GetFixedLengthValue(39).TrimEnd();
            prs.SkipSpaces();

            prs.ExpectMarker(new string[] { "Код платн.:", "Код плат.:" });
            prs.SkipSpaces();
            this.db_sndCode = prs.GetFixedLengthValue(14).TrimEnd();
            prs.SkipSpaces();

            prs.ExpectMarker(new string[] { "Р/р платн.:", "Р/с плат.:" });
            prs.SkipSpaces();
            this.db_sndAccount = prs.GetFixedLengthValue(40).TrimEnd();
            prs.SkipSpaces();

            prs.ExpectEOL();

            // row receiver
            prs.ExpectMarker(new string[] { "Банк одерж.:", "Банк пол.:" });
            prs.SkipSpaces();
            this.db_rcvBank = prs.GetFixedLengthValue(39).TrimEnd();
            prs.SkipSpaces();

            prs.ExpectMarker(new string[] { "Код одерж.:", "Код получ.:" });
            prs.SkipSpaces();
            this.db_rcvCode = prs.GetFixedLengthValue(14).TrimEnd();
            prs.SkipSpaces();

            prs.ExpectMarker(new string[] { "Р/р підпр.:", "Р/с пред.:" });
            prs.SkipSpaces();
            this.db_rcvAccount = prs.GetFixedLengthValue(40).TrimEnd();
            prs.SkipSpaces();

            prs.ExpectEOL();

            prs.ExpectMarker(new string[] { "ПЛАТІЖНИЙ РЕЄСТР(платежі громадян) по:", "ПЛАТЕЖНЫЙ РЕЕСТР(платежи граждан) по:" });
            prs.SkipSpaces();
            this.db_reestrName = prs.GetFixedLengthValue(80).TrimEnd();
            prs.SkipSpaces();

            prs.ExpectEOL();

            prs.ExpectMarker(new string[] { "Платіжне доручення №", "Платежное поручение №" });
            prs.SkipSpaces();

            this.db_po = prs.ExtractToMarker(new string[] { "від ", "от " }).TrimEnd();
            prs.SkipSpaces();

            try
            {
                this.db_poDate = prs.ExpectValueFormat(rexp_FullDate, 10).TrimEnd(); // 07.01.2022
            }
            catch (ParserError exc)
            {
                Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format(
                    "!WARN: {0}", ErrorUtils.FormatErrorMsg(exc)) : "");
                this.db_poDate = DEFAULT_PoDate_Null;
            }

            prs.SkipSpaces();

            prs.ExpectEOL();

            // skip table header
            prs.ExpectSplitLine();
            prs.SetMarker('H');
            prs.SkipLine();
            string hdr = prs.GetMarked('H');
            prs.SkipToSplitLine();

            // table rows
            prs.ExpectSplitLine();
            prs.SetMarker('D');
            prs.SkipToSplitLine();
            string data = prs.GetMarked('D');

            // parse table 
            parseTableData(hdr, data);

            prs.ExpectSplitLine();

            prs.ExpectMarker(new string[] { "Разом по р/р №", "Итого по р/с №" });
            prs.SkipSpaces();

            this.db_totalAccNo = prs.ExtractToMarker(new string[] { "Кільк. платежів - ", "Кол-во платежей - " }).Trim();
            prs.SkipSpaces();
            s = prs.ExtractToMarker(" ");
            if (!StrUtils.GetAsInt(s, out this.db_totalItems))
                prs.ThrowError(string.Format("invalid numeric value: {0}", s));
            prs.SkipSpaces();
            prs.ExpectEOL();

            prs.ExpectMarker(new string[] { "Сума прийнята:", "Сумма принятая:" });
            prs.SkipSpaces();
            s = prs.ExtractToMarker(" ");
            this.db_totalSum = ParseFloatValue(s, prs);
            prs.SkipSpaces();
            prs.ExpectEOL();

            prs.ExpectMarker(new string[] { "Комісія банку:", "Комиссия банка:" });
            prs.SkipSpaces();
            s = prs.ExtractToMarker(" ");
            this.db_totalCommission = ParseFloatValue(s, prs);
            prs.SkipSpaces();
            prs.ExpectEOL();

            prs.ExpectMarker(new string[] { "К перечислению:", "До перерахування:" });
            prs.SkipSpaces();
            s = prs.ExtractToMarker(" ");
            this.db_totalConfirmed = ParseFloatValue(s, prs);
            prs.SkipSpaces();
            //prs.ExpectEOL();

            return true;
        }

        private void parseTableData(string pHeader, string pData)
        {
            string[] hdrItems = pHeader.Split('|');
            List<int> columnWidth = new List<int>();
            List<int> columnPos = new List<int>();
            foreach (string it in hdrItems)
            {
                columnWidth.Add(it.Length);
            }
            int p = 0;
            foreach (int x in columnWidth)
            {
                columnPos.Add(p);
                p += x + 1;
            }

            string[] columnValue = new string[columnWidth.Count];
            string[] lines = StrUtils.AdjustLineBreaks(pData, "\n").Split('\n');
            foreach (string ln in lines)
            {
                string line = ln;
                if (line.Trim(StrUtils.CH_SPACES).Length == 0) continue;

                string _1stCol = line.Substring(columnPos[0], columnWidth[0]).Trim();

                bool isHas1stCol = !string.IsNullOrEmpty(_1stCol);
                if (isHas1stCol && hasColumnValues(columnValue))
                    flushColumnValues(columnValue);

                for (int iCol=0; iCol<columnPos.Count; iCol++)
                {
                    int cnt = columnWidth[iCol] + (iCol == 0 ? 0 : 1);
                    if (columnPos[iCol] + cnt > line.Length)
                        cnt = line.Length - columnPos[iCol];
                    string s = line.Substring(columnPos[iCol], cnt);
                    columnValue[iCol] += s;
                }
            }
            if (hasColumnValues(columnValue))
                flushColumnValues(columnValue);
        }

        protected bool hasColumnValues(string[] pColumnValue)
        {
            foreach (string it in pColumnValue)
                if (!string.IsNullOrEmpty(it))
                    return true;
            return false;
        }

        protected void resetColumnValues(string[] pColumnValue)
        {
            for (int iCol = 0; iCol < pColumnValue.Length; iCol++)
                pColumnValue[iCol] = "";
        }

        protected void packColumnValues(string[] pColumnValue)
        {
            for (int iCol = 0; iCol < pColumnValue.Length; iCol++)
            {
                string s = pColumnValue[iCol];
                s = s.Trim();
                while (s.IndexOf("  ") >= 0)
                    s = s.Replace("  ", " ");
                pColumnValue[iCol] = s;
            }
        }

        protected void flushColumnValues(string[] pColumnValue)
        {
            packColumnValues(pColumnValue);

            ReestrItem item = ReestrItem.Load(this, pColumnValue);
            if (item != null)
                this.Items.Add(item);

            resetColumnValues(pColumnValue);
        }

        protected void loadConfiguration()
        {
        }

        private Regex rexp_Timestamp = new Regex(@"(\d{2}).(\d{2}).(\d{2})\s+(\d{2})\:(\d{2})\:(\d{2})");
        private Regex rexp_FullDate = new Regex(@"(\d{2}).(\d{2}).(\d{4})");

        #endregion // Implementation details

        public static float ParseFloatValue(string s, ReestrFileParser prs)
        {
            //var z = CultureInfo.CurrentCulture.NumberFormat;
            bool hasDot = s.Contains(".");
            bool hasComma = s.Contains(",");
            string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (sep == "." && (hasComma && !hasDot))
                s = s.Replace(",", sep);
            if (sep == "," && (!hasComma && hasDot))
                s = s.Replace(".", sep);

            double x;
            if (!StrUtils.GetAsDouble(s, out x))
                prs.ThrowError(string.Format("invalid numeric value: {0}", s));
            return (float)x;
        }

        public class ReestrItem
        {
            public static ReestrItem Load(ReestrFile pOwner, string[] pColumnValues)
            {
                ReestrItem result = new ReestrItem(pOwner);
                
                result.db_idx = pOwner.Items.Count + 1;
                result.db_docNo = pColumnValues[0];
                result.db_operDay = pColumnValues[1];
                result.db_name = pColumnValues[2];
                result.db_account = pColumnValues[3];
                result.db_address = pColumnValues[4];
                result.db_counters = pColumnValues[5];
                result.db_payinterval = pColumnValues[6];

                result.db_amount = (float)ReestrFile.ParseFloatValue(pColumnValues[7], pOwner.Parser);
                result.db_commission = (float)ReestrFile.ParseFloatValue(pColumnValues[8], pOwner.Parser);

                int p1 = result.db_address.ToLower().IndexOf("кв.");
                int p2 = result.db_address.ToLower().IndexOf(",", p1 + 1);
                if (p1 >= 0)
                {
                    // _123456789_123456789
                    // 12, кв. 134, ццц
                    if (p2 >= 0)
                        result.db_kvRef = result.db_address.Substring(p1, p2 - p1);
                    else
                        result.db_kvRef = result.db_address.Substring(p1);
                    result.db_kvRef = result.db_kvRef.Replace(" ", " ").Replace(". ", ".");
                }
                else
                    result.db_kvRef = "-";

                return result;
            }

            public ReestrItem(ReestrFile pOwner)
            {
                this.Owner = pOwner;
            }

            public override string ToString()
            {
                return String.Format("ReestrItem[#{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10}]",
                    this.db_idx,
                    this.db_docNo,
                    this.db_operDay,
                    this.db_name,
                    this.db_account,
                    this.db_address,
                    this.db_kvRef,
                    this.db_counters,
                    this.db_payinterval,
                    this.db_amount.ToString("N2"),
                    this.db_commission.ToString("N2")
                    );
            }

            public ReestrFile Owner { get; protected set; }
            public int db_idx;
            public string db_docNo;
            public string db_operDay;
            public string db_name;
            public string db_account;
            public string db_address;
            public string db_kvRef;
            public string db_counters;
            public string db_payinterval;
            public float db_amount;
            public float db_commission;

        }
    }
}
