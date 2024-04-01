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
using System.Threading;
using System.Web;
using System.Xml;
using XService.Utils;
using Excel = Microsoft.Office.Interop.Excel;

namespace OsbbRev2
{
    public class Classificator
    {
        public Classificator()
        {
            this.Categories = new List<CategoryDescriptor>();
            this.Data = new Dictionary<CategoryDescriptor, List<DataItem> >();
            this.DefaultCategory = null;
            this.Flags = EFlags.None;

            loadConfiguration();
        }

        [Flags]
        public enum EFlags
        { 
            None = 0,
            AddDetalisation = 0x0001,
        }

        public List<CategoryDescriptor> Categories { get; protected set; }
        public Dictionary<CategoryDescriptor, List<DataItem>> Data { get; protected set; }
        public CategoryDescriptor DefaultCategory { get; protected set; }
        public EFlags Flags { get; set; }

        public bool HasFlag(EFlags pFlag)
        { 
            return ((this.Flags & pFlag) == pFlag);
        }

        public void Analyze(DataItem pItem, Excel.Range pRow, int pRowIndex)
        {
            tmpList.Clear();
            int cnt = Match(pItem, tmpList);
            Trace.WriteLine(string.Format("? cls[{0} categories]: {1} ", cnt, pItem));
            if (cnt == 0)
                tmpList.Add(this.DefaultCategory);

            pItem.Categories.AddRange(tmpList);

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
                Trace.WriteLine(string.Format(" -- Category[{0}]: {1} items...", categoryName, kvp.Value.Count));

                if (this.DefaultCategory == cd)
                {
                    defList = list; 
                    continue;
                }

                flushCategory(ref iRow, iCol, cd, list, pSheet, categoryName);

                if (HasFlag(EFlags.AddDetalisation))
                    PostDataItems(iCol + 6, ref iRow, list, pSheet, categoryName);

                iRow += 1;
            }

            if (defList != null)
            {
                flushCategory(ref iRow, iCol, this.DefaultCategory, defList, pSheet, categoryName);

                if (HasFlag(EFlags.AddDetalisation))
                    PostDataItems(iCol + 6, ref iRow, defList, pSheet, categoryName);
            }
        }

        public void PostDataItems(int iCol, ref int iRow, List<DataItem> pList, Excel.Worksheet pSheet, string pCategory)
        {
            Trace.WriteLine(string.Format("  ++ PostDataItems( R{0}/C{1}; {2} items; category={3} ... )", iRow, iCol, pList.Count, pCategory));

            pSheet.Cells[iRow, iCol + 0].Value = "row#";
            pSheet.Cells[iRow, iCol + 0].Value = "refs.";
            pSheet.Cells[iRow, iCol + 1].Value = "Acc#";
            pSheet.Cells[iRow, iCol + 2].Value = "Time";
            pSheet.Cells[iRow, iCol + 3].Value = "Money";
            pSheet.Cells[iRow, iCol + 4].Value = "Descr";
            pSheet.Cells[iRow, iCol + 5].Value = "CounterParty";
            iRow++;
            int idx = -1;
            foreach (DataItem it in pList)
            {
                idx++;
                if (idx > 0 && (idx % 100) == 0)
                    Trace.WriteLine(string.Format("  = item# {0}...", idx));

                pSheet.Cells[iRow, iCol + 0].Value = "#" + it.RowIndex.ToString("0###");
                pSheet.Cells[iRow, iCol + 1].Value = "\'" + it.AccountNo.ToString("0###");
                pSheet.Cells[iRow, iCol + 2].Value = "\'" + it.Categories.Count.ToString();
                pSheet.Cells[iRow, iCol + 3].Value = it.Time;
                pSheet.Cells[iRow, iCol + 4].Value = it.Money;
                pSheet.Cells[iRow, iCol + 5].Value = it.Description;
                pSheet.Cells[iRow, iCol + 6].Value = it.CounterParty;
                iRow++;

                Thread.Sleep(1);
            }

            Trace.WriteLine(string.Format("   = PostDataItems: done. R{0}/C{1}", iRow, iCol));
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

                CategoryDescriptor c = CategoryDescriptor.Load((XmlElement)node);
                if (c != null)
                {
                    this.Categories.Add(c);
                    if (c.Patterns.Count == 0 && this.DefaultCategory == null)
                        this.DefaultCategory = c;
                }
            }
        }

        private void flushCategory(ref int iRow, int iCol, CategoryDescriptor cd, List<DataItem> list, Excel.Worksheet pSheet, string pCategory)
        {
            Trace.WriteLine(string.Format("  ++ flushCategory( R{0}/C{1}; {2} items; category={3} ... )", iRow, iCol, list.Count, pCategory));

            iRow += 1;

            float sum = 0;
            float totalIn = 0;
            float totalOut = 0;
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

            pSheet.Cells[iRow, iCol].Value = sum;
            pSheet.Cells[iRow, iCol + 1].Value = totalIn;
            pSheet.Cells[iRow, iCol + 2].Value = totalOut;

            pSheet.Cells[iRow, iCol + 3].Value = list.Count;
            pSheet.Cells[iRow, iCol + 4].Value = cd.Caption;

            iRow += 2;

            Trace.WriteLine(string.Format("   = flushCategory: done. total=[in:{0}; out:{1}]", totalIn, totalOut));
        }

        private List<CategoryDescriptor> tmpList = new List<CategoryDescriptor>();

        #endregion // Implementation details

    }
}
