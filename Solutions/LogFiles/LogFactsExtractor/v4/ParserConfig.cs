/*
 * Log Facts Extractor: confguration objects.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-06-20
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using XService.Data;
using XService.Utils;

namespace LogFactExtractor4
{

    /// <summary>Root point of configuration holder</summary>
    public class LogFactsExtractorConfig
    {
        public LogFactsExtractorConfig()
        {
            this.LogTypes = new List<LogTypeConfig>();
            this.IOQueueLength = 0;
            this.WorkersCount = 4;
            this.ParsersPoolChannels = 4;
        }

        public override string ToString()
        {
            return String.Format("Cfg: wrk={0}, {1} log types", WorkersCount, LogTypes.Count);
        }

        #region Serializable

        /// <summary>Number of worker threads</summary>
        [XmlAttribute]
        public int WorkersCount { get; set; }

        /// <summary>Number of threads to handle in I/O queue. When 0 - then use dierct db I/O from</summary>
        [XmlAttribute]
        public int IOQueueLength { get; set; }

        [XmlAttribute]
        public int ParsersPoolChannels { get; set; }        

        /// <summary>Custom SQL commands to initialize data inserter db connection</summary>
        public string DbInserterInitialization { get; set; }

        /// <summary>Custom SQL commands to initialize data reader db connection</summary>
        public string DbReaderInitialization { get; set; }

        /// <summary>SQL statement to create db table for log files list</summary>
        public string CreateTablesStatement { get; set; }

        /// <summary>SQL statement to register log file in db</summary>
        public string LogFileInsertStatement { get; set; }

        /// <summary>SQL statement to register log file parse session in db</summary>
        public string LogParseInsertStatement { get; set; }

        /// <summary>Collection of log types supported by log facts extractor</summary>
        public List<LogTypeConfig> LogTypes { get; set; }

        #endregion // Serializable

        #region Run-time

        //[XmlIgnoreAttribute]
        //public LogFactsExtractorEngine Owner;

        [XmlIgnoreAttribute]
        public List<string> DbInserterInitializationCommands
        {
            get
            {
                if (this.dbInserterCommands == null)
                {
                    this.dbInserterCommands = new List<string>();
                    AppUtils.TextToCommands(this.DbInserterInitialization, this.dbInserterCommands);
                }
                return this.dbInserterCommands;
            }
        }

        [XmlIgnoreAttribute]
        public List<string> DbReaderInitializationCommands
        {
            get
            {
                if (this.dbReaderCommands == null)
                {
                    this.dbReaderCommands = new List<string>();
                    AppUtils.TextToCommands(this.DbReaderInitialization, this.dbReaderCommands);
                }
                return this.dbReaderCommands;
            }
        }

        /// <summary>Search LogType with specified name. Returns LogType object or null when not found</summary>
        public LogTypeConfig FindLogType(string pLogType)
        {
            foreach (LogTypeConfig lt in this.LogTypes)
            {
                if (StrUtils.IsSameText(lt.Name, pLogType))
                    return lt;
            }
            return null;
        }

        #endregion // Run-time

        /// <summary>Initialization after deserialization</summary>
        internal virtual void AfterDeserialization()
        {
            foreach (LogTypeConfig lt in this.LogTypes)
            {
                lt.Owner = this;
                foreach (ExtractorPattern ep in lt.Patterns)
                {
                    ep.Owner = lt;
                }
            }

            DbUtils.DbInserterInitializationScript = this.DbInserterInitialization;
            DbUtils.DbReaderInitializationScript = this.DbReaderInitialization;
        }

        private List<string> dbInserterCommands = null;
        private List<string> dbReaderCommands = null;
    }


    /// <summary>Config: holder of log-type information</summary>
    public class LogTypeConfig
    {
        public enum ELogTypeScope { File, Directory };

        public LogTypeConfig()
        {
            this.Patterns = new List<ExtractorPattern>();
            this.TableConfig = new DataTable();
            //this.Lines = new List<string>();
        }

        public override string ToString()
        {
            return String.Format("LogType[{0}]: {1} patterns, tm={2}", Name, Patterns.Count, LogTimeFormat);
        }

        #region Serializable

        /// <summary>Name of log type (will be used as fact-id)</summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>Description of log type (will be used as fact-id)</summary>
        [XmlAttribute]
        public string Description { get; set; }

        /// <summary>Regex patter of log filename</summary>
        [XmlAttribute]
        public string LogFilenamePattern { get; set; }

        /// <summary>Name of database table which will be used to store extract log facts</summary>
        [XmlAttribute]
        public string TableName { get; set; }

        /// <summary>Name log-time format</summary>
        [XmlAttribute]
        public string LogTimeFormat { get; set; }

        /// <summary>SQL statement to create db table for log facts</summary>
        public string CreateTableStatement { get; set; }

        /// <summary>SQL statement to insert log fact into db</summary>
        public string FactInsertStatement { get; set; }

        /// <summary>Collection of fact extraction patterns</summary>
        public List<ExtractorPattern> Patterns { get; set; }

        /// <summary>Name of database table which will be used to store extract log facts</summary>
        [XmlAttribute]
        public ELogTypeScope Scope { get; set; }

        #endregion // Serializable

        #region Run-time

        [XmlIgnoreAttribute]
        public LogFactsExtractorConfig Owner;

        [XmlIgnoreAttribute]
        public DataTable TableConfig { get; set; }

        [XmlIgnoreAttribute]
        public Regex LogFilenamePatternRexp
        {
            get
            {
                lock (this)
                {
                    if (!string.IsNullOrEmpty(this.LogFilenamePattern))
                    {
                        if (this.logFilenamePatternRexp == null)
                            this.logFilenamePatternRexp = new Regex(this.LogFilenamePattern);
                        return logFilenamePatternRexp;
                    }
                    return this.logFilenamePatternRexp;
                }
            }
        }

        [XmlIgnoreAttribute]
        public LogTimestampParserBase LogTimestampParser
        {
            get
            {
                lock (this)
                {
                    if (this.logTimestampParser == null)
                    {
                        if (string.IsNullOrEmpty(this.LogTimeFormat))
                            throw new Exception(string.Format("LogType[{0}]: log timestamp format is not defined!", this.Name));

                        string id = this.LogTimeFormat;
                        string param = null;
                        if (id.IndexOf(':') >= 0)
                        {
                            param = StrUtils.GetAfterPattern(id, ":");
                            id = StrUtils.GetToPattern(id, ":");
                        }
                        if (!AppUtils.LogTimestampParsers.TryGetValue(id.ToLower(), out this.logTimestampParser))
                            throw new Exception(string.Format("Log timestamp parser [{0}] is not found!", this.LogTimeFormat));
                        this.logTimestampParser.SetParameter(param);
                    }
                    return this.logTimestampParser;
                }
            }
        }

        public int GetTotalErrors()
        {
            int result = 0;
            foreach (ExtractorPattern ep in this.Patterns)
                result += ep.ErrorsCount;
            return result;
        }

        public int GetTotalFacts()
        {
            int result = 0;
            foreach (ExtractorPattern ep in this.Patterns)
                result += ep.FactsCount;
            return result;
        }

        public void ResetCounters()
        {
            foreach (ExtractorPattern ep in this.Patterns)
            {
                ep.FactsCount = 0;
                ep.ErrorsCount = 0;
            }
        }

        public void LoadSummary(DataTable pTable, DbConnection pSrcDb)
        {
            pTable.Clear();

            //DbProviderFactory dbFactory;
            using (DbConnection db = pSrcDb) // this.Owner.Owner.GetLocalDbConnection(EDbConnectionType.Reader, this, out dbFactory))
            {
                using (DbCommand cmd = db.CreateCommand())
                {
                    /*
                    cmd.CommandText = string.Format("" +
                        "SELECT L.logId, L.filename, L.created registered, P.parseId, P.filesize, P.fileModifyTime, P.created lastParsed, count(*) factsCount " +
                        "  FROM LogFile L " +
                        "  LEFT JOIN LogParse P ON P.logId = L.logId AND P.parseId = (SELECT max(parseId) FROM LogParse WHERE logId = L.logId) " +
                        "  LEFT JOIN {0} F ON F.logId = L.logId " +
                        " WHERE Upper(L.logtype) = Upper('{1}')" +
                        " GROUP BY L.logId " +
                        " ORDER BY L.logId, P.parseId " +
                        "", this.TableName, this.Name);

                    using (DbDataAdapter da = dbFactory.CreateDataAdapter())
                    {
                        da.SelectCommand = cmd;
                        da.Fill(pTable);
                    }
                    Trace.WriteLine(string.Format(" * LogType[{0}].LoadSummary: R:{1} x C:{2} resultset loaded", this.Name, pTable.Rows.Count, pTable.Columns.Count));
                    */

                    /* DBG: 
                    List<DataTable> list = new List<DataTable>();
                    list.Add(pTable);
                    DacService.SerializeTables(list, Environment.ExpandEnvironmentVariables("%temp%\\logType_summary.xml"));
                    */
                }
            }
        }

        public void EnsureFactsTable(DbConnection db)
        {
            string stmt = this.CreateTableStatement;
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(tableName)", this.TableName);
            DbUtils.ExecuteCommands(db, stmt);

            DbUtils.EnsureTableConfigLoaded(db, this.TableConfig, this.TableName, "logId = -1");
        }

        #endregion // Run-time

        private Regex logFilenamePatternRexp;
        private LogTimestampParserBase logTimestampParser;
    }


    /// <summary>Config: holder of fact extraction pattern</summary>
    public class ExtractorPattern
    {
        public ExtractorPattern()
        {
            this.Tosser = new List<string>();
        }

        public override string ToString()
        {
            return String.Format("Pattern[{0}]: {1} tossDefs, rexp={2}", Name, Tosser.Count, RexpText);
        }

        [XmlIgnoreAttribute]
        public LogTypeConfig Owner;

        #region Serializable

        /// <summary>Name of fact extraction pattern (will be used as fact-id)</summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>Text pattern to be used as quick-search factor (when found then try to match Regex)</summary>
        [XmlAttribute]
        public string Text { get; set; }

        /// <summary>Regular expression describing how to extract facts from log line</summary>
        [XmlAttribute(AttributeName = "Rexp")]
        public string RexpText { get; set; }

        //public string TosserDesinitions { get; set; }

        public List<string> Tosser { get; set; }

        #endregion // Serializable

        /// <summary>Regular expression describing how to extract facts from log line</summary>
        [XmlIgnoreAttribute]
        public Regex Rexp
        {
            get
            {
                lock (this)
                {
                    if (this.rexp == null && !string.IsNullOrEmpty(this.RexpText))
                    {
                        this.rexp = StrUtils.ExtractRegexp(this.RexpText);
                    }
                    return this.rexp;
                }
            }
        }

        [XmlIgnoreAttribute]
        public Dictionary<string, string> TosserDefs
        {
            get
            {
                if (this.tosserDefs == null)
                {
                    this.tosserDefs = new Dictionary<string, string>();
                    if (this.Tosser != null && this.Tosser.Count > 0)
                    {
                        foreach (string s in this.Tosser)
                        {
                            string sn = StrUtils.GetToPattern(s.Trim(), "=").Trim();
                            string sv = StrUtils.GetAfterPattern(s.Trim(), "=").Trim();
                            this.tosserDefs[sn.ToLower()] = sv;
                        }
                    }
                }
                return this.tosserDefs;
            }
        }

        [XmlIgnoreAttribute]
        public int FactsCount = 0;

        [XmlIgnoreAttribute]
        public int ErrorsCount = 0;

        private Regex rexp = null;
        private Dictionary<string, string> tosserDefs;
    }

}
