/* 
 * Reestr Parser app.
 *
 * Main engine for Reestr Parser app.
 * Orchestrate application components into a symphony.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Aug, 2023
 */

#if _UseDB
  using System.Data.SQLite;
#endif
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using XService.Utils;

namespace ReestrParser
{
    public class ParserEngine
    {
        public const string DEFAULT_ConnectionString = "Data Source=db\\reestr.db";

        public static TraceSwitch TrcLvl { get { return ToolSettings.TrcLvl; } }

        public ParserEngine(ToolSettings pSettings)
        {
            this.Settings = pSettings;
            this.ConnectionString = DEFAULT_ConnectionString;
            this.ParsedFiles = new List<ReestrFile>();
            this.Statistic = new StatisticHolder();
            
            loadConfiguration();
        }

        #region Properties

        public ToolSettings Settings { get; protected set; }
        public StatisticHolder Statistic { get; protected set; }
        public string ConnectionString { get; set; }
        public DbConnection CurrentDb { get; protected set; }
        public DbProviderFactory DbFactory { get; set; }
        public List<ReestrFile> ParsedFiles  { get; protected set; }

        #endregion // Properties

        public void OpenDb()
        {
            if (this.CurrentDb != null) return;

            #if _UseDB
            
            Trace.WriteLine(string.Format("Connecting SQLite db [{0}]...", this.ConnectionString));            
            this.CurrentDb = new SQLiteConnection(this.ConnectionString);
            this.CurrentDb.Open();

            this.DbFactory = DbProviderFactories.GetFactory(this.CurrentDb);

            Trace.WriteLine(string.Format(" * Connected (version={0}).", this.CurrentDb.ServerVersion));

            #endif
        }

        public void Parse()
        {
            DateTime t1 = DateTime.Now;

            List<FileInfo> files = new List<FileInfo>();
            foreach (ToolSettings.FileSource fs in this.Settings.SrcFiles)
            {
                fs.ResolveFiles();
                files.AddRange(fs.Files);
            }
            Trace.WriteLine(string.Format("* {0} files to process...", files.Count));

            this.Statistic.Reset();            

            Trace.WriteLine("=== Parallel processing...");
            Parallel.ForEach(files, (fi) => 
            {
                Trace.WriteLine(string.Format("--- processing: {0} ...", fi.FullName));

                ReestrFile f = ReestrFile.Parse(fi);
                if (f != null)
                {
                    lock (this.ParsedFiles)
                        this.ParsedFiles.Add(f);
                    lock (this.Statistic)
                        this.Statistic.AddFile(f);                    
                }
                else
                {
                    this.Statistic.totalFailures++;
                    Trace.WriteLine(string.Format("!FAIL to parse: {0}", fi.FullName));
                }
            });
            Trace.WriteLine(string.Format("+++ Parsing completed. Elapsed time: {0} sec", (DateTime.Now - t1).TotalSeconds.ToString("N1")));
            Trace.WriteLine(string.Format("= Statistic: {0} total files, {1} total items", this.ParsedFiles.Count, this.Statistic.totalItems));
            Trace.WriteLine(string.Format("= Statistic: {0} total good files, {1} total wrong files, {2} total failures",
                this.Statistic.totalGoodFiles, this.Statistic.totalWrongFiles, this.Statistic.totalFailures));
            Trace.WriteLine(string.Format("= Statistic: {0} total sum, {1} total commission, {2} total confirmed",
                this.Statistic.totalSum, this.Statistic.totalCommission, this.Statistic.totalConfirmed));

            //DBG:
            foreach (ReestrFile f in this.ParsedFiles)
            {
                bool isOk = f.Validate();

                //DBG: repeat call in case of failure - just to debug what could be wrong
                if (!isOk)
                    isOk = f.Validate();

                Trace.WriteLine(string.Format(" #file[{0}]: {1}", (isOk ? "ok" : "WRONG!"), f));
                Trace.Indent();
                foreach (ReestrFile.ReestrItem it in f.Items)
                    Trace.WriteLine(string.Format(" #item: {0}", it));
                Trace.Unindent();
            }
        }

        public void GenerateOutput()
        {
            Trace.WriteLine(string.Format("--- {0} output formats to process...", this.Settings.Outputs.Count));

            foreach (string fmt in this.Settings.Outputs)
            {
                GenerateOutput(fmt);
            }
        }

        public void GenerateOutput(string pFormat)
        {
            Trace.WriteLine(string.Format(" --> Generating output of [{0}] ...", pFormat));

            pFormat = pFormat.ToLower().Trim();
            switch (pFormat)
            {
                case "db": outputToDb(); break;
                case "excel": outputToExcel(); break;
            }
        }

        #region Implementation details

        protected string fixFpValue(float pValue)
        {
            return pValue.ToString().Replace(",", ".");
        }

        protected void dbCleanup(DbCommand cmd)
        {
            Trace.WriteLine("-- Cleanup db...");

            cmd.CommandText = "DELETE FROM ReestrItem";
            int ni = cmd.ExecuteNonQuery();

            cmd.CommandText = "DELETE FROM ReestrHeader";
            int nh = cmd.ExecuteNonQuery();

            Trace.WriteLine(string.Format(" * cleanup SqlCodes: ReestrHeader={0}; ReestrItem={1}", nh, ni));
        }

        protected void dbInsertHeader(ReestrFile f, DbCommand cmd)
        {
            cmd.CommandText = string.Format(
                "INSERT INTO ReestrHeader ( " +
                "  id, filename, lang, date, sndBank, sndCode, sndAccount, rcvBank, rcvCode, rcvAccount, reestrName, " +
                "  reestrDate, po, poDate, totalAccNo, totalItems, totalCommission, totalSum, totalConfirmed )" +
                "VALUES ( " +
                "  {0}, \"{1}\", \"{2}\", \"{3}\", \"{4}\", \"{5}\", \"{6}\", \"{7}\", \"{8}\", \"{9}\", " +
                "  \"{10}\", \"{11}\", \"{12}\", \"{13}\", \"{14}\", {15}, {16}, {17}, {18} )" +
                "",
                f.db_id, f.Filename, f.db_lang, f.db_date, f.db_sndBank, f.db_sndCode, f.db_sndAccount, f.db_rcvBank, f.db_rcvCode, f.db_rcvAccount, f.db_reestrName, 
                f.db_reestrDate, f.db_po, f.db_poDate, f.db_totalAccNo, f.db_totalItems,
                fixFpValue(f.db_totalCommission), fixFpValue(f.db_totalSum), fixFpValue(f.db_totalConfirmed)
                );
            Trace.WriteLine(string.Format("+ Insert header: {0}", cmd.CommandText));
            int n = cmd.ExecuteNonQuery();
            Trace.WriteLine(string.Format(" * SqlCode: {0}", n));
        }

        protected void dbInsertItem(ReestrFile.ReestrItem it, DbCommand cmd)
        {
            cmd.CommandText = string.Format(
                "INSERT INTO ReestrItem ( " +
                "  reestrId, idx, docNo, operDay, name, account, address, kvRef, counters, payinterval, amount, commission )" +
                "VALUES ( " +
                "  {0}, {1}, \"{2}\", \"{3}\", \"{4}\", \"{5}\", \"{6}\", \"{7}\", \"{8}\", \"{9}\", {10}, {11} )" +
                "",
                it.Owner.db_id, it.db_idx, it.db_docNo, it.db_operDay, it.db_name.ToUpper(),
                it.db_account, it.db_address, it.db_kvRef, it.db_counters, it.db_payinterval, 
                fixFpValue(it.db_amount), fixFpValue(it.db_commission) 
                );
            Trace.WriteLine(string.Format("+ Insert item: {0}", cmd.CommandText));
            int n = cmd.ExecuteNonQuery();
            Trace.WriteLine(string.Format(" * SqlCode: {0}", n));
        }

        protected void outputToDb()
        {
            DateTime t1 = DateTime.Now;
            this.OpenDb();

            using (DbCommand cmd = this.CurrentDb.CreateCommand())
            {
                if (this.Settings.CleanupDb)
                    dbCleanup(cmd);
                else
                    Trace.WriteLine("! Skip db-cleanup.");

                foreach (ReestrFile f in this.ParsedFiles)
                {
                    dbInsertHeader(f, cmd);
                    Trace.Indent();
                    foreach (ReestrFile.ReestrItem it in f.Items)
                    {
                        dbInsertItem(it, cmd);
                    }
                    Trace.Unindent();
                }
            }
            Trace.WriteLine(string.Format("+++ DB-output completed. Elapsed time: {0} sec", (DateTime.Now - t1).TotalSeconds.ToString("N1")));
        }

        protected void outputToExcel()
        {
            DateTime t1 = DateTime.Now;

            StringBuilder sb = new StringBuilder(this.ParsedFiles.Count * 0x400);
            sb.Append(
                "<!doctype html>\n" +
                "<html>\n\n" + 
                "<head>\n" + 
                "<title>Parsed reestr files</title>\n" + 
                "</head>\n\n" + 
                "<body>\n" + 
                "");

            sb.Append(
                "<table border=\"1\" cellspacing=\"0\" cellpadding=\"0\"><thead><tr>\n" +
                "<!-- 00 --> <th>file</th> \n" +
                "<!-- 01 --> <th>date</th> \n" +
                "<!-- 02 --> <th>sender</th> \n" +
                "<!-- 03 --> <th>receiver</th> \n" +
                "<!-- 04 --> <th>registry name</th> \n" +
                "<!-- 05 --> <th>registry date</th>\n" +
                "<!-- 06 --> <th>PO#</th>\n" +
                "<!-- 07 --> <th>PO date</th>\n" +
                "" +
                "<!-- 08 --> <th>#</th>\n" +
                "<!-- 09 --> <th>doc#</th>\n" +
                "<!-- 10 --> <th>day</th>\n" +
                "<!-- 11 --> <th>name</th>\n" +
                "<!-- 12 --> <th>amount</th>\n" +
                "</tr></thead>\n" +
                "<tbody>\n" +
                "");

            string[] columns = new string[13];
            string[] prevValues = new string[13];

            int i;
            foreach (ReestrFile f in this.ParsedFiles)
            {
                columns[0] = f.Filename;
                columns[1] = f.db_date;
                columns[2] = string.Format("{0}/{1}/{2}", f.db_sndBank, f.db_sndCode, f.db_sndAccount);
                columns[3] = string.Format("{0}/{1}/{2}", f.db_rcvBank, f.db_rcvCode, f.db_rcvAccount);
                columns[4] = f.db_reestrName;
                columns[5] = f.db_reestrDate;
                columns[6] = f.db_po;
                columns[7] = f.db_poDate;

                for (i = 8; i < columns.Length; i++) columns[i] = "";

                appendHtmlTableRow(sb, columns);

                foreach (ReestrFile.ReestrItem it in f.Items)
                {
                    columns[8] = it.db_idx.ToString();
                    columns[9] = it.db_docNo;
                    columns[10] = it.db_operDay;
                    columns[11] = it.db_name;
                    columns[12] = it.db_amount.ToString("N2");

                    appendHtmlTableRow(sb, columns);
                }
            }

            // put some statistic into last row...
            sb.AppendFormat(
                "<tr>\n" +
                " <td>{0}</td> <td>total files</td> " + 
                " <td>{1}</td> <td>total items</td>\n" +
                "</tr><tr>\n" +
                " <td>{0}</td> <td>total sum</td> " + 
                " <td>{1}</td> <td>total commission</td> " + 
                " <td>{2}</td> <td>total confirmed</td> \n",
                "</tr>\n" +
                "",
                this.ParsedFiles.Count, this.Statistic.totalItems,
                this.Statistic.totalSum, this.Statistic.totalCommission, this.Statistic.totalConfirmed);

            sb.Append(
                "</tbody>\n" +
                "</table>\n" +
                "");

            sb.Append(
                "</body>\n" +
                "</html>\n" +
                "");

            string fn = "output.xlsx";
            using (StreamWriter sw = File.CreateText(fn))
            {
                sw.WriteLine(sb.ToString());
            }
            Trace.WriteLine(string.Format(" ++ Saving file: {0}", fn));

            Trace.WriteLine(string.Format("+++ Excel(html)-output completed. Elapsed time: {0} sec", (DateTime.Now - t1).TotalSeconds.ToString("N1")));
        }

        protected void appendHtmlTableRow(StringBuilder sb, string[] columns)
        {
            sb.AppendFormat(
                "<tr>\n" +
                " <td>{0}</td> " +
                " <td>{1}</td> " +
                " <td>{2}</td> " +
                " <td>{3}</td> " +
                " <td>{4}</td> " +
                " <td>{5}</td> " +
                " <td>{6}</td>\n" +
                " <td>{7}</td>\n" +
                "" +
                " <td>{8}</td>\n" +
                " <td>{9}</td>\n" +
                " <td>{10}</td>\n" +
                " <td>{11}</td>\n" +
                " <td>{12}</td>\n" +
                "</tr>\n" +
                "",
                columns[0], columns[1], columns[2], columns[3], columns[4],
                columns[5], columns[6], columns[7], columns[8], columns[9],
                columns[10], columns[11], columns[12]
                );
        }

        protected void loadConfiguration()
        {

        }
                
        #endregion // Implementation details

        public class StatisticHolder
        {
            public int totalGoodFiles = 0, totalFailures = 0, totalWrongFiles = 0;
            public int totalItems = 0;
            public float totalSum = 0, totalCommission = 0, totalConfirmed = 0;

            public void Reset()
            { 
                this.totalGoodFiles = 0;
                this.totalWrongFiles = 0;
                this.totalFailures = 0;
                this.totalItems = 0;
                this.totalSum = 0;
                this.totalCommission = 0;
                this.totalConfirmed = 0;
            }

            public void AddFile(ReestrFile f)
            {
                totalItems += f.Items.Count;
                totalSum += f.db_totalSum;
                totalCommission += f.db_totalCommission;
                totalConfirmed += f.db_totalConfirmed;

                if (f.Validate())
                    this.totalGoodFiles++;
                else
                    this.totalWrongFiles++;
            }

        }
    }
}
