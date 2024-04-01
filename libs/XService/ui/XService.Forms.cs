/*
 * Simple utlities to facilitate development using Windows.Forms.
 * Written by Dmitry Bond. at May 6, 2007
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.Text;

namespace XService.Utils
{

    public class CustomListViewSorter : IComparer
    {
        private int column = -1;
        // picture of column types
        private string col_types_pic = null;
        // index of image to indicate ascedency sorting in column
        private int asc_image_index = 0;

        public delegate void DisplayUnsortedMethod(ListView pListView);
        public DisplayUnsortedMethod DisplayUnsorted;

        public CustomListViewSorter(ListView pListView, int pColumn, SortOrder pOrder, string pColTypesPicture, int pAscImgIdx)
        {
            this.Order = pOrder;
            this.ListViewCtrl = pListView;
            this.col_types_pic = pColTypesPicture.ToUpper();
            this.asc_image_index = pAscImgIdx;
            this.TriState = false;
            Set(pColumn, pOrder);
        }

        /// <summary>Can be set when value should be extracted from column, before specified delimiter</summary>
        public string ExtraDelimiter = null;

        public void Set(int pColumn, SortOrder pOrder)
        {
            this.Column = pColumn;
            this.Order = pOrder;
        }

        public ListView ListViewCtrl { get; protected set; }

        public int Column
        {
            get { return this.column; }
            set { this.column = value; this.Order = SortOrder.Ascending; }
        }

        public SortOrder Order { get; set; }

        public bool TriState { get; set; }

        public SortOrder Next()
        {
            switch (this.Order)
            {
                case SortOrder.None: this.Order = SortOrder.Ascending; break;
                case SortOrder.Ascending: this.Order = SortOrder.Descending; break;
                case SortOrder.Descending: this.Order = (this.TriState ? SortOrder.None : SortOrder.Ascending); break;
            }
            return this.Order;
        }

        public void UpdateListView()
        {
            if (!this.ListViewCtrl.ListViewItemSorter.Equals(this))
                this.ListViewCtrl.ListViewItemSorter = this;

            if (this.ListViewCtrl.Sorting != this.Order)
                this.ListViewCtrl.Sorting = this.Order;

            ColumnHeader ch = this.ListViewCtrl.Columns[this.Column];
            switch (this.Order)
            {
                case SortOrder.None: ch.ImageIndex = -1; ch.TextAlign = ch.TextAlign; break;
                case SortOrder.Ascending: ch.ImageIndex = this.asc_image_index; break;
                case SortOrder.Descending: ch.ImageIndex = this.asc_image_index+1; break;
            }

            if (this.Order == SortOrder.None)
            {
                if (this.DisplayUnsorted != null)
                {
                    this.DisplayUnsorted(this.ListViewCtrl);
                    this.ListViewCtrl.ListViewItemSorter = this;
                }
            }
            else
                this.ListViewCtrl.Sort();

            this.ListViewCtrl.Refresh();
        }

        public int Compare(object x, object y)
        {
            if (this.Order == SortOrder.None)
                return 0;

            int col_index = this.column;

            string v1 = ((ListViewItem)x).SubItems[col_index].Text;
            string v2 = ((ListViewItem)y).SubItems[col_index].Text;

            if (!string.IsNullOrEmpty(this.ExtraDelimiter) && (v1.IndexOf(ExtraDelimiter) >= 0 || v1.IndexOf(ExtraDelimiter) >= 0))
            {
                string s1 = StrUtils.GetToPattern(v1, this.ExtraDelimiter);
                string s2 = StrUtils.GetToPattern(v2, this.ExtraDelimiter);
                v1 = (s1 == null ? v1 : s1);
                v2 = (s2 == null ? v2 : s2);
            }

            char col_type = 'S';
            if (!string.IsNullOrEmpty(this.col_types_pic) && this.col_types_pic.Length > col_index)
            {
                col_type = this.col_types_pic[col_index];
            }

            int result = 0;
            switch (col_type)
            {
                case 'I':
                    v1 = v1.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "");
                    v2 = v2.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "");
                    int i1, i2;
                    if (!StrUtils.GetAsInt(v1.Trim(), out i1)) i1 = 0;
                    if (!StrUtils.GetAsInt(v2.Trim(), out i2)) i2 = 0;
                    if (i1 < i2)
                        result = -1;
                    else if (i1 > i2)
                        result = 1;
                    break;

                case 'N':
                    double n1, n2;
                    if (!StrUtils.GetAsDouble(v1.Trim(), out n1)) n1 = 0;
                    if (!StrUtils.GetAsDouble(v2.Trim(), out n2)) n2 = 0;
                    if (n1 < n2)
                        result = -1;
                    else if (n1 > n2)
                        result = 1;
                    break;

                default:
                    result = String.Compare(v1, v2, true);
                    break;
            }

            if (this.Order == SortOrder.Descending)
                result = result * -1;

            return result;
        }

        /// <summary>This is the main handler of column click. You only need to call this method</summary>
        /// <example>
        /// private void lvThus_ColumnClick(object sender, ColumnClickEventArgs e)
        /// {
        ///     if (this.lvComparer == null)
        ///     {
        ///         this.lvComparer = new CustomListViewSorter(lvThus, 0, SortOrder.None, "siis", 0);
        ///         lvThus.ListViewItemSorter = this.lvComparer;
        ///     }
        ///     CustomListViewSorter.HandleColumnClick((CustomListViewSorter)this.lvThus.ListViewItemSorter, e.Column);
        /// }
        /// </example>
        /// <param name="pSorter"></param>
        /// <param name="pColumn"></param>
        public static void HandleColumnClick(CustomListViewSorter pSorter, int pColumn)
        {
            ListView list = pSorter.ListViewCtrl;

            list.BeginUpdate();
            try
            {
                for (int i = 0; i < list.Columns.Count; i++)
                {
                    if (list.Columns[i].ImageIndex >= 0)
                    {
                        list.Columns[i].ImageIndex = -1;
                        HorizontalAlignment ha = list.Columns[i].TextAlign;
                        list.Columns[i].TextAlign = ha;
                    }
                }

                if (pColumn == pSorter.Column)
                    pSorter.Next();
                else
                    pSorter.Column = pColumn;

                pSorter.UpdateListView();
            }
            finally { list.EndUpdate(); }
        }

        /*
        public bool ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ColumnHeader columnHdr = this.list_view.Columns[e.Column];
            bool isSameColumn = (this.Column == e.Column);

            if (!isSameColumn)
            {
                ColumnHeader prevColumnHdr = this.list_view.Columns[this.Column];
                prevColumnHdr.ImageIndex = -1;
                //prevColumnHdr.TextAlign = HorizontalAlignment.;
                this.list_view.Sorting = SortOrder.None;
            }

            this.Column = e.Column;
            // Note: have to re-set ListViewItemSorter property to keep it working correctly! :-\
            this.list_view.ListViewItemSorter = this;

            if (this.list_view.Sorting == SortOrder.None)
            {
                //chName.ImageKey = "sort-asc";
                columnHdr.ImageIndex = 0;
                this.list_view.Sorting = SortOrder.Ascending;
                this.list_view.Sort();
            }
            else if (this.list_view.Sorting == SortOrder.Ascending)
            {
                //chName.ImageKey = "sort-desc";
                columnHdr.ImageIndex = 1;
                this.list_view.Sorting = SortOrder.Descending;
                this.list_view.Sort();
            }
            else
            {
                // Note: there is a bug in .NET - http://social.msdn.microsoft.com/Forums/en/winformsdesigner/thread/4cf0b454-2dd8-46bb-87a9-5290a2b7a7e3
                // So, the only way to reset column image is to use "chName.TextAlign = chName.TextAlign" after ImageIndex set to -1.
                //chName.ImageKey = "empty";
                columnHdr.ImageIndex = -1;
                //columnHdr.TextAlign = chName.TextAlign;
                this.list_view.Sorting = SortOrder.None;
                //displayCsParameters();
            }

            return (this.list_view.Sorting != SortOrder.None);
        }
        */

    }

}
