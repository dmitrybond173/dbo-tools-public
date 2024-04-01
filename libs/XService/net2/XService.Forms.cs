/*
 * Simple utlities to facilitate development using Windows.Forms.
 * Written by Dmitry Bond. at May 6, 2007
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

namespace XService.Utils
{

    public class CustomListViewSorter : IComparer
    {
        // index of column to sort by
        private int column = 0;
        // inverse sorting
        private SortOrder order = SortOrder.None;
        // ref to sorted ListView
        private ListView list_view = null;
        // picture of column types
        private string col_types_pic = null;
        // index of image to indicate ascedency sorting in column
        private int asc_image_index = 0;

        public CustomListViewSorter(ListView pListView, int pColumn, SortOrder pOrder, string pColTypesPicture, int pAscImgIdx)
        {
            this.list_view = pListView;
            this.col_types_pic = pColTypesPicture.ToUpper();
            this.asc_image_index = pAscImgIdx;
            Set(pColumn, pOrder);
        }

        public void Set(int column, SortOrder pOrder)
        {
            this.column = column;
            this.order = pOrder;
        }

        public ListView ListViewCtrl { get { return this.list_view; } }

        public int Column
        {
            get { return this.column; }
            set { this.column = value; this.order = SortOrder.Ascending; }
        }

        public SortOrder Order
        {
            get { return this.order; }
            set { this.order = value; }
        }

        public SortOrder Next()
        {
            switch (this.order)
            {
                case SortOrder.None: this.order = SortOrder.Ascending; break;
                case SortOrder.Ascending: this.order = SortOrder.Descending; break;
                case SortOrder.Descending: this.order = SortOrder.None; break;
            }
            return this.order;
        }

        public void UpdateListView()
        {
            this.list_view.Columns[this.Column].ImageIndex = (this.order == SortOrder.Descending
                ? this.asc_image_index + 1
                : (this.order == SortOrder.None ? -1 : this.asc_image_index));

            this.list_view.Sorting = SortOrder.None;
            this.list_view.ListViewItemSorter = this;
            this.list_view.Sorting = (this.order == SortOrder.None)
                ? SortOrder.Ascending
                : this.Order;
            this.list_view.Sort();

            this.list_view.Refresh();
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

        public int Compare(object x, object y)
        {
            // SortOrder.None means - display items in original order - sort by last (hidden) column
            int col_index = (this.order == SortOrder.None
                ? this.ListViewCtrl.Columns.Count - 1
                : this.column);

            string v1 = ((ListViewItem)x).SubItems[col_index].Text;
            string v2 = ((ListViewItem)y).SubItems[col_index].Text;

            char col_type = 'S';
            if (!string.IsNullOrEmpty(this.col_types_pic) && this.col_types_pic.Length > col_index)
            {
                col_type = this.col_types_pic[col_index];
            }

            int result = 0;
            switch (col_type)
            {
                case 'I':
                    int i1 = Convert.ToInt32(v1);
                    int i2 = Convert.ToInt32(v2);
                    if (i1 < i2)
                        result = -1;
                    else if (i1 > i2)
                        result = 1;
                    break;

                case 'N':
                    double n1 = Convert.ToDouble(v1);
                    double n2 = Convert.ToDouble(v2);
                    if (n1 < n2)
                        result = -1;
                    else if (n1 > n2)
                        result = 1;
                    break;

                default:
                    result = String.Compare(v1, v2, true);
                    break;
            }

            if (this.order == SortOrder.Descending)
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

            for (int i = 0; i < list.Columns.Count; i++)
                list.Columns[i].ImageIndex = -1;

            if (pColumn == pSorter.Column)
                pSorter.Next();
            else
                pSorter.Column = pColumn;

            pSorter.UpdateListView();
        }
    }

}
