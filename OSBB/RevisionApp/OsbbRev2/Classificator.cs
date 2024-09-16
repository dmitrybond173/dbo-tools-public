/* 
 * App for OSBB Revision.
 *
 * Classificator.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Feb, 2024
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml;
using XService.Utils;
using static OsbbRev2.Classificator;
using static XService.Utils.SyncUtils.LockInfo;
using Excel = Microsoft.Office.Interop.Excel;

namespace OsbbRev2
{
    public class Classificator
    {
        public Classificator()
        {
            this.Categories = new List<CategoryDescriptor>();
            this.Data = new Dictionary<CategoryDescriptor, List<DataItem> >();
            this.Items = new List<DataItem>();
            this.DefaultCategory = null;
            this.Flags = EFlags.None;
            this.MaxRows = 0;

            loadConfiguration();
        }

        [Flags]
        public enum EFlags
        { 
            None = 0,
            AddDetalisation = 0x0001,
        }

        public delegate void UpdateStatusBarMethod(string pMsg);
        public UpdateStatusBarMethod UpdateStatusBar;

        public List<CategoryDescriptor> Categories { get; protected set; }
        public Dictionary<CategoryDescriptor, List<DataItem>> Data { get; protected set; }
        public CategoryDescriptor DefaultCategory { get; protected set; }
        public List<DataItem> Items { get; protected set; }
        public EFlags Flags { get; set; }
        public int MaxRows { get; set; }
        public FilterDescriptor Filter { get; protected set; }

        public bool HasFlag(EFlags pFlag)
        { 
            return ((this.Flags & pFlag) == pFlag);
        }

        public bool IsAcceptedByFilter(DataItem pItem)
        {
            if (this.Filter == null) return true;

            foreach (string it in this.Filter.Accounts)
            {
                if (it == pItem.AccountNo.ToString())
                    return true;
            }
            return false;
        }

        public void Analyze(DataItem pItem, Excel.Range pRow, int pRowIndex)
        {
            if (!IsAcceptedByFilter(pItem))
            {
                Trace.WriteLine(string.Format("FILTERED-OUT: row# {0}", pRowIndex));
                return;
            }

            tmpList.Clear();
            int cnt = Match(pItem, tmpList);
            Trace.WriteLine(string.Format("? cls[{0} categories]: {1} ", cnt, pItem));
            if (cnt == 0)
            {
                Trace.WriteLine(string.Format(" = row# {0} - use default-category", pRowIndex));
                tmpList.Add(this.DefaultCategory);
            }

            pItem.Categories.AddRange(tmpList);

            this.Items.Add(pItem);

            foreach (CategoryDescriptor cd in tmpList)
            {
                DataItem item = new DataItem(pItem) { Category = cd };

                List<DataItem> trgList;
                if (!Data.TryGetValue(cd, out trgList))
                {
                    trgList = new List<DataItem>();
                    this.Data[cd] = trgList;
                }
                trgList.Add(item);
            }
        }

        public void FinalizeAnalysis()
        {
            // check if we have any items were not added to any of categories...
            foreach (DataItem item in this.Items)
            {
                //if ()
            }
        }

        public void FlushData(Excel.Worksheet pSheet)
        {
            Trace.WriteLine(string.Format("--- Flush statistic: {0} categories...", this.Data.Count));

            int iRow = 1;
            int iCol = 1;

            int totalCount = 0;
            foreach (KeyValuePair<CategoryDescriptor, List<DataItem>> kvp in this.Data)
                totalCount += kvp.Value.Count;

            pSheet.Cells[iRow, iCol].Value = string.Format( 
                "{0} data items in {1} categories. {2}", totalCount, this.Data.Count, StrUtils.NskTimestampOf(DateTime.Now));
            iRow += 2;

            pSheet.Cells[iRow, iCol].Value = "Total";            
            pSheet.Cells[iRow, iCol + 1].Value = "Total In";
            pSheet.Cells[iRow, iCol + 2].Value = "Total Out";

            pSheet.Cells[iRow, iCol + 3].Value = "Items";
            pSheet.Cells[iRow, iCol + 4].Value = "Category";
            iRow += 2;

            int idx = 0;
            string categoryName = "";
            List<DataItem> defList = null;
            foreach (KeyValuePair<CategoryDescriptor, List<DataItem>> kvp in this.Data)
            {
                idx++;
                CategoryDescriptor cd = kvp.Key;
                List<DataItem> list = kvp.Value;

                categoryName = string.Format("#{0} - {1}", idx, cd.Caption);
                setStatus("+ category: " + string.Format("#{0} of {1} - {2}", idx, this.Data.Count, cd.Caption));
                Trace.WriteLine(string.Format(" -- Category[{0}]: {1} items...", categoryName, kvp.Value.Count));

                if (this.DefaultCategory == cd)
                {
                    defList = list; 
                    continue;
                }

                flushCategory(ref iRow, iCol, cd, list, pSheet, categoryName);

                if (HasFlag(EFlags.AddDetalisation))
                    PostDataItems(idx, iCol + 6, ref iRow, list, pSheet, categoryName);

                iRow += 1;
            }

            if (defList != null)
            {
                flushCategory(ref iRow, iCol, this.DefaultCategory, defList, pSheet, categoryName);

                if (HasFlag(EFlags.AddDetalisation))
                    PostDataItems(idx, iCol + 6, ref iRow, defList, pSheet, categoryName);
            }

            flushCategory(ref iRow, iCol, null, null, pSheet, "---end---");
        }

        private static string[] colNames = new string[] { 
            "row#", "refs.", "Acc#", "Time", "Money", "Descr", "CounterParty", "Categories..." 
        };

        public void PostDataItems(int pCategoryIdx, int iCol, ref int iRow, List<DataItem> pList, Excel.Worksheet pSheet, string pCategory)
        {
            Trace.WriteLine(string.Format("  ++ PostDataItems( R{0}/C{1}; {2} items; category={3} ... )", iRow, iCol, pList.Count, pCategory));

            PostCaptions(pSheet, iRow, iCol, colNames);

            iRow++;
            int idx = -1;
            Excel.Range cr;
            foreach (DataItem it in pList)
            {
                idx++;
                if (idx > 0 && (idx % 100) == 0)
                    Trace.WriteLine(string.Format("  = item# [{0}] -> {1}...", pCategoryIdx, idx));

                pSheet.Cells[iRow, iCol + 0].Value = "#" + it.RowIndex.ToString("0###");
                
                pSheet.Cells[iRow, iCol + 1].Value = it.Categories.Count;
                SetCellFormat(pSheet.Cells[iRow, iCol + 1], ECellFormat.Number);
                //cr = pSheet.Cells[iRow, iCol + 1];
                //cr.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                pSheet.Cells[iRow, iCol + 2].Value = "\'" + it.AccountNo.ToString("0###");

                pSheet.Cells[iRow, iCol + 3].Value = it.Time; //it.CopyCell(DataItem.iTime, pSheet.Cells, iCol + 3); //pSheet.Cells[iRow, iCol + 3].Value = it.Time;
                SetCellFormat(pSheet.Cells[iRow, iCol + 3], ECellFormat.Timestamp);
                //cr = pSheet.Cells[iRow, iCol + 3];
                //cr.NumberFormat = "YYYY-MM-DD,hh:mm";

                pSheet.Cells[iRow, iCol + 4].Value = it.MoneyOriginalValue;
                SetCellFormat(pSheet.Cells[iRow, iCol + 4], ECellFormat.Currency);
                //cr = pSheet.Cells[iRow, iCol + 4];
                //cr.NumberFormat = "# ##0,00;[Red]-# ##0,00";
                //cr.NumberFormat = "$* #,##0.00";
                //cr.NumberFormat = "* #,##0.00 [$]";                

                //pSheet.Cells[iRow, iCol + 4].Value = it.MoneyValue;
                //pSheet.Cells[iRow, iCol + 4].Value = it.Money;
                //it.CopyCell(DataItem.iMoney, pSheet.Cells, iCol + 4); //pSheet.Cells[iRow, iCol + 4].Value = it.Money;

                pSheet.Cells[iRow, iCol + 5].Value = it.Description;
                pSheet.Cells[iRow, iCol + 6].Value = it.CounterParty;
                pSheet.Cells[iRow, iCol + 7].Value = it.CategoriesList;

                iRow++;

                Thread.Sleep(1);
            }

            Trace.WriteLine(string.Format("   = PostDataItems: done. R{0}/C{1}", iRow, iCol));
        }

        public enum ECellFormat { Timestamp, Currency, Number }

        public static void SetCellFormat(Excel.Range pCells, ECellFormat pFormat)
        {
            string fmt = "@";
            switch (pFormat)
            {
                case ECellFormat.Timestamp: fmt = "YYYY-MM-DD,hh:mm"; break;
                case ECellFormat.Currency: fmt = "* #,##0.00 [$]"; break;
                case ECellFormat.Number: 
                    pCells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    return ;
                default: return;
            }
            pCells.NumberFormat = fmt;
        }

        public static void SetCellFormat(Excel.Range pCells, string pFormat)
        {
            pCells.NumberFormat = pFormat;
        }

        public int Match(DataItem pItem, List<CategoryDescriptor> pTargetList)
        {
            int savedCnt = pTargetList.Count;
            foreach (CategoryDescriptor c in this.Categories) 
            {
                if (c.Patterns.Count == 0) continue; // here we need to handle only categories with patterns!
                if (c.IsMatch(pItem))
                {
                    pTargetList.Add(c);
                }
            }
            return pTargetList.Count - savedCnt;
        }

        #region Implementation details

        private void loadConfiguration()
        {
            XmlElement cfgNode = (XmlElement)ConfigurationManager.GetSection("Categories");
            if (cfgNode == null)
                throw new Exception("Config sectopn <Categories> is not found!");

            foreach (XmlNode attr in cfgNode.Attributes) 
            {
                if (attr.Name.ToLower().StartsWith("idx_"))
                {
                    int idx;
                    if (!StrUtils.GetAsInt(attr.Value.Trim(), out idx)) continue;

                    string id = attr.Name.Remove(0, 4).Trim();
                    if (StrUtils.IsSameText(id, "AccountNo")) DataItem.iAccountNo = idx;
                    else if (StrUtils.IsSameText(id, "Date")) DataItem.iDate = idx;
                    else if (StrUtils.IsSameText(id, "Time")) DataItem.iTime = idx;
                    else if (StrUtils.IsSameText(id, "Money")) DataItem.iMoney = idx;
                    else if (StrUtils.IsSameText(id, "Description")) DataItem.iDescription = idx;
                    else if (StrUtils.IsSameText(id, "CouterParty")) DataItem.iCouterParty = idx;
                }
            }

            foreach (XmlNode node in cfgNode) 
            {
                if (node.NodeType != XmlNodeType.Element) continue;

                if (StrUtils.IsSameText(node.Name, "Filter"))
                {
                    FilterDescriptor f = FilterDescriptor.Load((XmlElement)node);
                    if (f != null)
                        this.Filter = f;
                }
                else if (StrUtils.IsSameText(node.Name, "Category"))
                {
                    CategoryDescriptor c = CategoryDescriptor.Load((XmlElement)node);
                    if (c != null)
                    {
                        this.Categories.Add(c);
                        if (c.Patterns.Count == 0 && this.DefaultCategory == null)
                            this.DefaultCategory = c;
                    }
                }
            }
        }

        private void setStatus(string msg)
        {
            if (this.UpdateStatusBar != null)
                this.UpdateStatusBar(msg);
        }

        private void flushCategory(ref int iRow, int iCol, CategoryDescriptor cd, List<DataItem> list, Excel.Worksheet pSheet, string pCategory)
        {
            Trace.WriteLine(string.Format("  ++ flushCategory( R{0}/C{1}; {2} items; category={3} ... )", 
                iRow, iCol, (list != null ? list.Count : -1), (pCategory != null ? pCategory : "---") ));

            iRow += 1;

            float sum = 0;
            float totalIn = 0;
            float totalOut = 0;
            if (list != null)
            {
                int idx = -1;
                foreach (DataItem item in list)
                {
                    idx++;
                    if (idx > 0 && (idx % 100) == 0)
                        Trace.WriteLine(string.Format("  = item# {0}...", idx));

                    sum += item.Money;
                    if (item.Money > 0) totalIn += item.Money;
                    if (item.Money < 0) totalOut += item.Money;
                }
            }

            pSheet.Cells[iRow, iCol].Value = sum;
            SetCellFormat(pSheet.Cells[iRow, iCol], ECellFormat.Currency);

            pSheet.Cells[iRow, iCol + 1].Value = totalIn;
            SetCellFormat(pSheet.Cells[iRow, iCol + 1], ECellFormat.Currency);

            pSheet.Cells[iRow, iCol + 2].Value = totalOut;
            SetCellFormat(pSheet.Cells[iRow, iCol + 2], ECellFormat.Currency);

            pSheet.Cells[iRow, iCol + 3].Value = (list != null ? list.Count : -1);
            SetCellFormat(pSheet.Cells[iRow, iCol + 3], ECellFormat.Number);

            pSheet.Cells[iRow, iCol + 4].Value = (cd != null ? cd.Caption : "---");

            iRow += 2;

            Trace.WriteLine(string.Format("   = flushCategory: done. total=[in:{0}; out:{1}]", totalIn, totalOut));
        }

        private List<CategoryDescriptor> tmpList = new List<CategoryDescriptor>();

        #endregion // Implementation details

        public static void PostCaptions(Excel.Worksheet pSheet, int iRow, int iCol, string[] pCapptions)
        {
            int idx = -1;
            foreach (string cap in pCapptions)
            {
                idx++;
                pSheet.Cells[iRow, iCol + idx].Value = cap;
            }
        }

        public class FilterDescriptor
        {
            public static FilterDescriptor Load(XmlElement pDomNode)
            {
                FilterDescriptor result = new FilterDescriptor();
                
                XmlNode attr = AppUtils.RequiredAttr("accounts", pDomNode);
                if (attr != null)
                {
                    result.Accounts.AddRange(attr.Value.Replace(";", ",").Trim().Split(','));
                }

                return result;
            }

            public FilterDescriptor()
            {
                this.Accounts = new List<string>();
            }

            public List<string> Accounts { get; protected set; }
        }
    }
}
