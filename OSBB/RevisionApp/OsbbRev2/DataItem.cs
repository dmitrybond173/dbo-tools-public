using System;
using Excel = Microsoft.Office.Interop.Excel;

namespace OsbbRev2
{
    public class DataItem
    {
        public static int iAccountNo = 1;
        public static int iDate = 3;
        public static int iTime = 4;
        public static int iMoney = 5;
        public static int iDescription = 7;
        public static int iCouterParty = 9;

        public static DataItem Load(Excel.Range pRow, int pRowIndex)
        {
            DataItem result = new DataItem() { Row = pRow, RowIndex = pRowIndex };

            int n;
            string s = pRow.Cells[iAccountNo].Value;
            if (Int32.TryParse(s, out n))
            {
                result.AccountNo = n;
                result.Money = (float)pRow.Cells[iMoney].Value;
            }
            else
                return null;

            return result;
        }

        public DataItem() { }

        public DataItem(DataItem pSrc) 
        {
            this.Row = pSrc.Row;
            this.RowIndex = pSrc.RowIndex;

            this.AccountNo = pSrc.AccountNo;
            this._time = pSrc._time;
            this._description = pSrc._description;
            this._counterParty = pSrc._counterParty;
            this.Money = pSrc.Money;

            this.Category = pSrc.Category;
        }

        public override string ToString()
        {
            return String.Format("DataItem[row#{0}; acc#{1}; {2}; {3} uah; {4}; {5}]",
                this.RowIndex,
                this.AccountNo, 
                this.Time,
                this.Money,
                this.Description,
                this.CounterParty
                );
        }

        public int AccountNo { get; set; } = 0;

        private DateTime _time = DateTime.MinValue;
        public DateTime Time 
        {
            get
            {
                if (_time == DateTime.MinValue)
                { 
                    DateTime dt = this.Row.Cells[iDate].Value;
                    var x = this.Row.Cells[iTime].Value;
                    DateTime tm = AppUtils.ParseExcelDate(x.ToString());
                    _time = AppUtils.Compose(dt, tm);
                }
                return _time;
            }
        }

        private string _description = null;
        public string Description 
        { 
            get 
            {
                if (_description == null)
                {
                    _description = this.Row.Cells[iDescription].Value;
                }
                return _description;
            } 
        }

        private string _counterParty = null;
        public string CounterParty 
        {
            get
            {
                if (_counterParty == null)
                {
                    _counterParty = this.Row.Cells[iCouterParty].Value;
                }
                return _counterParty;
            }
        }

        public float Money { get; set; }

        public int RowIndex { get; set; }

        public Excel.Range Row { get; set; }

        public CategoryDescriptor Category { get; set; }
    }
}
