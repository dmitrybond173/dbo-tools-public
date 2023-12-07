/*
 * Log Facts Extractor: app utils.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-05-25
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using XService.Data;
using XService.Utils;

namespace LogFactExtractor4
{
    /// <summary>
    /// Collection of app-specific utilities
    /// </summary>
    public class AppUtils
    {
        private static object syncRoot = new object();
        public static object SyncRoot { get { return syncRoot; } }

        public static DateTime ParseLogTime(string s)
        {
            // _123456789_123456789
            // 20190517,000007.43 
            int y = Convert.ToInt32(s.Substring(0, 4));
            int m = Convert.ToInt32(s.Substring(4, 2));
            int d = Convert.ToInt32(s.Substring(6, 2));

            int h = Convert.ToInt32(s.Substring(9, 2));
            int n = Convert.ToInt32(s.Substring(11, 2));
            int sec = Convert.ToInt32(s.Substring(13, 2));
            int frac = Convert.ToInt32(s.Substring(16, 2)) * 10;

            return new DateTime(y, m, d, h, n, sec, frac);
        }

        public static float ParseFloat(string s)
        {
            float result = -1;
            double x;
            if (StrUtils.GetAsDouble(s, out x))
            {
                if (x > 0)
                    x *= 1.0;
                result = (float)Math.Truncate(x * 100.0);
                result /= 100.0f;
            }
            return result;
        }

        public static double ParseDouble(string s)
        {
            double result = -1;
            if (StrUtils.GetAsDouble(s, out result))
            {
                if (result > 0)
                    result *= 1.0;
                result = Math.Truncate(result * 100.0);
                result /= 100.0;
            }
            return result;
        }

        public static int TimeToMs(DateTime pTime)
        {
            return (pTime.Hour * 3600 + pTime.Minute * 60 + pTime.Second) * 1000 + pTime.Millisecond;
        }

        public static int TimeToSec(DateTime pTime)
        {
            return pTime.Hour * 3600 + pTime.Minute * 60 + pTime.Second;
        }

        /// <summary>Validates if specified time (only time, without date) fit specified range</summary>
        public static bool IsTimeInRange(DateTime pTime, DateTime pFrom, DateTime pTo)
        {
            int t = TimeToMs(pTime);
            int tfrom = TimeToMs(pFrom);
            int tto = TimeToMs(pTo);
            return (tfrom <= t && t <= tto);
        }

        public static DateTime AdjustTimeByBoundOfMinutes(DateTime pTs, int pMinutesBound)
        {
            int mv = TimeToSec(pTs);
            int newMv = BitUtils.AlignTo(mv, pMinutesBound * 60);
            if (newMv > mv)
                newMv -= pMinutesBound * 60;
            mv = newMv;
            return new DateTime(pTs.Year, pTs.Month, pTs.Day, mv / 3600, (mv % 3600) / 60, 0);
        }

        public static DateTime ReplaceDate(DateTime pTime, DateTime pDate)
        {
            return new DateTime(pDate.Year, pDate.Month, pDate.Day, pTime.Hour, pTime.Minute, pTime.Second, pTime.Millisecond);
        }

        public static int TextToCommands(string pText, List<string> pTargetList)
        {
            int savedCount = pTargetList.Count;
            string[] lines = pText.Split('\n');
            foreach (string ln in lines)
            {
                string cmd = ln.Trim(StrUtils.CH_SPACES);
                if (!string.IsNullOrEmpty(cmd))
                    pTargetList.Add(cmd);
            }
            return (pTargetList.Count - savedCount);
        }

        public static int TextToStatements(string pText, List<string> pTargetList, char pDelimiter)
        {
            int savedCount = pTargetList.Count;
            string[] lines = pText.Split(pDelimiter);
            foreach (string ln in lines)
            {
                string cmd = ln.Trim(StrUtils.CH_SPACES);
                if (!string.IsNullOrEmpty(cmd))
                    pTargetList.Add(cmd);
            }
            return (pTargetList.Count - savedCount);
        }

        public static string ReplaceMacroValue(string pText, string pPattern, string pValue)
        { 
            int idx = 0;
            do
            {
                idx = pText.ToLower().IndexOf(pPattern.ToLower(), idx);
                if (idx >= 0)
                {
                    pText = pText.Remove(idx, pPattern.Length);
                    pText = pText.Insert(idx, pValue);
                }
            }
            while (idx >= 0);
            return pText;
        }

        public static string QuoteStr(string pText)
        {
            return "\'" + pText + "\'";
        }

        public static Type FindType(Type[] pTypes, string pTypeName)
        {
            foreach (Type t in pTypes)
            {
                if (t.Name.CompareTo(pTypeName) == 0)
                {
                    return t;
                }
            }
            return null;
        }

        public static Dictionary<string, LogTimestampParserBase> LogTimestampParsers
        {
            get
            {
                lock (SyncRoot)
                {
                    if (logTimestampParsers == null)
                    {
                        logTimestampParsers = new Dictionary<string, LogTimestampParserBase>();
                        logTimestampParsers["currentts"] = new CurrentTsLogTimestamp();
                        logTimestampParsers["bymask"] = new ByMaskLogTimestamp();
                        logTimestampParsers["yyyymmdd,hhnnss.ff"] = new CompactNetTimestamp();
                        logTimestampParsers["compactts"] = new CompactNetTimestamp();
                        logTimestampParsers["cslmonlogtimestamp"] = new CslmonLogTimestamp();
                    }
                    return logTimestampParsers;
                }
            }
        }

        private static Dictionary<string, LogTimestampParserBase> logTimestampParsers;
    }


    public class TaskContext
    {
        public LogTypeConfig LogType;
        public int TaskNo;
        public int LineFrom, LineTo;
        public List<string> Lines;
        public int ProgressStep;
        public Task Worker;
    }


    public class SqlTaskContext
    {
        public SqlTaskContext()
        {
            lock (typeof(SqlTaskContext))
            {
                WUID++;
                this.ID = WUID;
            }
        }

        public static int WUID = 0;

        public int ID;
        public TaskContext Task;
        public string Caption;
        public string Statement;
    }

    /// <summary>Base class for all log timestamps parsers</summary>
    public abstract class LogTimestampParserBase
    {
        public virtual void SetParameter(string pValue) { }
        public abstract DateTime Parse(string pText);
        public abstract DateTime ParseEx(string pText, Dictionary<string, string> pAttributes);
    }


    /// <summary>
    /// Simulation of timestamp parsing - just return current timestamp for every call
    /// </summary>
    public class CurrentTsLogTimestamp : LogTimestampParserBase
    {
        public override DateTime Parse(string pText)
        {
            return DateTime.Now;
        }

        public override DateTime ParseEx(string pText, Dictionary<string, string> pAttributes) { return Parse(pText); }
    }


    /// <summary>
    /// Mask to recognize timestamp should be specified after "byMask:" prefix in config
    /// </summary>
    public class ByMaskLogTimestamp : LogTimestampParserBase
    {
        public string Mask = null;

        public override void SetParameter(string pValue) 
        {
            this.Mask = pValue;
        }

        public override DateTime Parse(string pText)
        {
            if (this.Mask == null)
                throw new Exception(string.Format("Mask is not set for logtime parser({0})", this.GetType()));

            Dictionary<string, string> fields = new Dictionary<string, string>();
            int idx = 0;
            foreach (char ch in this.Mask)
            {
                string id = "" + ch;
                if (idx >= pText.Length) break;
                char chValue = pText[idx];
                string v;
                if (fields.TryGetValue(id, out v))
                    v += chValue;
                else
                    v = "" + chValue;
                fields[id] = v;
                idx++;
            }

            int y=1900, mo=1, d=1, h=0, mi=0, s=0, f=0;
            foreach (KeyValuePair<string, string> it in fields)
            {
                if (it.Key == "y") y = Convert.ToInt32(it.Value);
                else if (it.Key == "m") mo = Convert.ToInt32(it.Value);
                else if (it.Key == "d") d = Convert.ToInt32(it.Value);
                else if (it.Key == "h") h = Convert.ToInt32(it.Value);
                else if (it.Key == "n") mi = Convert.ToInt32(it.Value);
                else if (it.Key == "s") s = Convert.ToInt32(it.Value);
                else if (it.Key == "f") f = Convert.ToInt32(it.Value);
            }            

            return new DateTime(y, mo, d, h, mi, s, f);
        }

        public override DateTime ParseEx(string pText, Dictionary<string, string> pAttributes) { return Parse(pText); }        
    }



    /// <summary>Parser of compact .NET timestamp</summary>
    public class CompactNetTimestamp : LogTimestampParserBase
    {
        public override DateTime Parse(string pText)
        {
            try
            {
                // _123456789_1234567
                // 20190531,172355,44
                int y, m, d, h, n, s, f;
                y = Convert.ToInt16(pText.Substring(0, 4));
                m = Convert.ToInt16(pText.Substring(4, 2));
                d = Convert.ToInt16(pText.Substring(6, 2));
                h = Convert.ToInt16(pText.Substring(9, 2));
                n = Convert.ToInt16(pText.Substring(11, 2));
                s = Convert.ToInt16(pText.Substring(13, 2));
                f = Convert.ToInt16(pText.Substring(16, 2));
                return new DateTime(y, m, d, h, n, s, f * 10);
            }
            catch (Exception exc)
            {
                throw new Exception(string.Format("Fail to parse logtime ({0}): {1}", pText, ErrorUtils.FormatErrorMsg(exc)));
            }
        }

        public override DateTime ParseEx(string pText, Dictionary<string, string> pAttributes) { return Parse(pText); }
    }


    /// <summary>
    /// Parser of CSLMON log timestamps.
    /// In such case date part of timestamp is taken from log filename, which is expected to be passed in pAttributes for ParseEx() method
    /// </summary>
    public class CslmonLogTimestamp : LogTimestampParserBase
    {
        public override DateTime Parse(string pText)
        {
            throw new NotSupportedException("CslmonLogTimestampParser - log file name is required");
        }

        public override DateTime ParseEx(string pText, Dictionary<string, string> pAttributes)
        {
        	lock (pAttributes)
            try
            {
                string logFileName;
                if (!pAttributes.TryGetValue("logfilename", out logFileName))
                    return DateTime.MinValue;

                string logName = Path.GetFileNameWithoutExtension(logFileName).ToLower();
                bool isExecutor = logName.StartsWith("ex");

                // _123456789_123456789_123456789
                // ex20190517-234216-20140.log
                // mo20190517.log

                int pid = 0;
                int y, m, d, h = 0, n = 0, s = 0, f = 0;
                y = Convert.ToInt16(logName.Substring(2, 4));
                m = Convert.ToInt16(logName.Substring(6, 2));
                d = Convert.ToInt16(logName.Substring(8, 2));
                DateTime logDate = new DateTime(y, m, d);
                pAttributes["logdate"] = StrUtils.NskTimestampOf(logDate);

                DateTime ts = logDate;
                if (isExecutor)
                {
                    h = Convert.ToInt16(logName.Substring(11, 2));
                    n = Convert.ToInt16(logName.Substring(13, 2));
                    s = Convert.ToInt16(logName.Substring(15, 2));
                    ts = new DateTime(y, m, d, h, n, s, 0);
                    pAttributes["appstart"] = StrUtils.NskTimestampOf(ts);

                    pid = Convert.ToInt32(logName.Substring(18));
                    pAttributes["pid"] = pid.ToString();
                }

                // _123456789_12
                // 00:00:00.032
                h = Convert.ToInt16(pText.Substring(0, 2));
                n = Convert.ToInt16(pText.Substring(3, 2));
                s = Convert.ToInt16(pText.Substring(6, 2));
                f = Convert.ToInt16(pText.Substring(9, 3));
                ts = new DateTime(y, m, d, h, n, s, f);

                return ts;
            }
            catch (Exception exc)
            {
                throw new Exception(string.Format("Fail to parse logtime ({0}): {1}", pText, ErrorUtils.FormatErrorMsg(exc)));
            }
        }
    }


    public enum EDbConnectionType
    { 
        Default,
        Inserter,
        Reader,
    }


    /// <summary>
    /// 
    /// </summary>
    public class DbUtils
    {
        public const string DEFAULT_SQLite_ConnectionString = "Data Source=LogFacts.sqlite;Version=3;";

        public static TraceSwitchEx TrcLvl = new TraceSwitchEx("DataLayer", "DataLayer");

        public static string LocalDbFilename = "LogFacts.sqlite";
        public static DbProviderFactory LocalDbFactory = null;

        public static string DbInserterInitializationScript = null;
        public static string DbReaderInitializationScript = null;

        public static void GetCustomInitialization(EDbConnectionType pConnectionType, ref List<string> extraInit)
        {
            if (extraInit == null)
                extraInit = new List<string>();
            extraInit.Clear();
            string txt = null;
            switch (pConnectionType)
            {
                case EDbConnectionType.Default:
                case EDbConnectionType.Inserter:
                    txt = DbInserterInitializationScript;
                    break;
                case EDbConnectionType.Reader:
                    txt = DbReaderInitializationScript;
                    break;
            }
            if (txt != null)
            {
                AppUtils.TextToCommands(txt, extraInit);
            }
        }

        public static DbConnection CreateLocalDbConnection(EDbConnectionType pConnectionType, LogTypeConfig pLogType)
        {
            DbConnection db;
            try
            {
                Trace.WriteLine("Trying standard ADO.NET way of creating connection...");
                DacService.ConnectionStringName = "LocalDB";
                db = DacService.GetConnection();
                LocalDbFactory = DacService.GetDbFactory();
                Trace.WriteLine(string.Format("= Successfully connected db: {0} / {1}.", db.GetType(), LocalDbFactory.GetType()));
                return db;
            }
            catch (Exception exc)
            {
                Trace.WriteLine(string.Format("CreateLocalDbConnection.{0}", ErrorUtils.FormatErrorMsg(exc)));
            }

            ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings["LocalDB"];
            if (cs == null)
                cs = new ConnectionStringSettings("LocalDB", DEFAULT_SQLite_ConnectionString);

            Trace.WriteLine(string.Format(" = LocalDB: {0}", cs.ConnectionString));
            SQLiteConnection conn = new SQLiteConnection(cs.ConnectionString);

            LocalDbFactory = SQLiteFactory.Instance;

            Trace.WriteLine(string.Format(" = LocalDB: opening..."));
            conn.Open();
            Trace.WriteLine(string.Format("   = LocalDB: opened."));

            List<string> extraInit = null;
            GetCustomInitialization(pConnectionType, ref extraInit);

            if (pLogType != null)
            {
                switch (pConnectionType)
                {
                    case EDbConnectionType.Default:
                    case EDbConnectionType.Inserter:
                        extraInit = pLogType.Owner.DbInserterInitializationCommands;
                        break;
                    case EDbConnectionType.Reader:
                        extraInit = pLogType.Owner.DbReaderInitializationCommands;
                        break;
                }
            }
            if (extraInit != null && extraInit.Count > 0)
            {
                Trace.WriteLine(string.Format(" ! {0} db-initialization commands", extraInit.Count));
                DbUtils.ExecuteCommands(conn, extraInit);
            }

            Trace.WriteLine(string.Format(" = LocalDB: connection ready."));
            return conn;
        }

        public static void ExecuteCommands(DbConnection pDb, List<string> pCommands)
        {
            using (DbCommand cmd = pDb.CreateCommand())
            {
                foreach (string cmdText in pCommands)
                {
                    if (cmdText.Trim().StartsWith("--")) continue;
                    if (cmdText.Trim().StartsWith("//")) continue;
                    cmd.CommandText = cmdText;
                    int r = cmd.ExecuteNonQuery();
                    Trace.WriteLine(string.Format(" = DB-exec( {0} ): resule={1}", cmd.CommandText, r));
                }
            }
        }

        public static void ExecuteCommands(DbConnection pDb, string pCommand)
        {
            List<string> commands = new List<string>();
            commands.Add(pCommand);
            ExecuteCommands(pDb, commands);
        }

        public static object GetMaxValueOf(string pTableName, string pFieldName, string pExtraConditions, DbConnection conn)
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                string stmt = string.Format("SELECT max({0}) FROM {1}", pFieldName, pTableName);
                if (pExtraConditions != null && !string.IsNullOrEmpty(pExtraConditions.Trim()))
                    stmt += (" WHERE " + pExtraConditions);
                cmd.CommandText = stmt;

                object v = null;
                Trace.WriteLineIf(DbUtils.TrcLvl.TraceInfo, DbUtils.TrcLvl.TraceInfo ? string.Format(" * GetMaxValueOf.SQL: {0}", cmd.CommandText) : "");
                v = cmd.ExecuteScalar();
                bool isNull = (v == null || v is DBNull);
                if (isNull)
                    v = null;
                Trace.WriteLineIf(DbUtils.TrcLvl.TraceInfo, DbUtils.TrcLvl.TraceInfo ? string.Format("   = GetMaxValueOf.Result: {0}", (v != null ? v.ToString() : "(null)")) : "");
                
                return v;
            }
        }

        public static void EnsureTableConfigLoaded(DbConnection conn, DataTable pCfgTable, string pTableName, string pExtraConditions)
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                string stmt = "SELECT * FROM $(TableName)";
                if (!string.IsNullOrEmpty(pExtraConditions))
                    stmt += string.Format(" WHERE {0}", pExtraConditions);
                stmt = stmt.Replace("$(TableName)", pTableName);

                cmd.CommandText = stmt;

                using (DbDataAdapter da = new SQLiteDataAdapter())
                {
                    da.SelectCommand = cmd;
                    da.Fill(pCfgTable);
                }
            }
        }

        public static void DumpListOfAdoNetProviders()
        {
            int idb = 0;
            DataTable fdb = DbProviderFactories.GetFactoryClasses();
            foreach (DataRow fr in fdb.Rows)
            {
                string line = null;
                foreach (DataColumn fc in fdb.Columns)
                {
                    string v = fr[fc].ToString();
                    if (line == null) line = v;
                    else line += ("\t" + v);
                }
                Trace.WriteLine(string.Format("* DbProvider#{0}: {1}", idb, line));
                idb++;
            }
        }

        public static bool IsNullTs(DateTime pTs)
        {
            return (pTs.Year == 1900 && pTs.Month == 1 && pTs.Day == 1);
        }

        public static string FloatToDb(double n, int pPrecision=2)
        {
            string s = (pPrecision > 0 ? n.ToString(string.Format("N{0}", pPrecision)) : n.ToString());
            s = s.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, ".");
            return s;
        }

    }

}
