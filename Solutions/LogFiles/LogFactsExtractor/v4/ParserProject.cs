/*
 * Log Facts Extractor: main engine classes - document objects.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-07-21
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

    /// <summary>
    /// 
    /// </summary>
    public class ParserProject
    {
        public static TraceSwitchEx TrcLvl = new TraceSwitchEx("Project", "Project");

        public static ParserProject FindProject(LogFactsExtractorEngine pEngine, LogTypeConfig pLogType, string pLocation) // 
        {
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("- FindProject( logType={0}, location={1} )", pLogType.Name, pLocation) : "");

            ParserProject proj = null;

            DbProviderFactory f;
            using (DbConnection db = pEngine.GetLocalDbConnection(EDbConnectionType.Reader, pLogType, out f))
            {
                using (DbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = string.Format(
                        "SELECT * FROM ParseProject WHERE logtype = \'{0}\' AND lower(location) = \'{1}\'",
                        pLogType.Name, pLocation.ToLower());
                    using (DbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            proj = new ParserProject(pEngine, pLogType, pLocation);
                            proj.ProjectId = (int)dr["projectId"];
                            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("  = FindProject: projId={0}", proj.ProjectId) : "");
                        }
                    }

                    if (proj != null)
                    {
                        cmd.CommandText = string.Format("SELECT * FROM LogFile WHERE projectId = {0} ORDER BY logId", proj.ProjectId);
                        using (DbDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                int id = (int)dr["logId"];
                                string location = dr["filename"].ToString();
                                string tsCre = dr["created"].ToString();
                                string tsParse = dr["parsed"].ToString();
                                string name = dr["logtype"].ToString();
                                LogTypeConfig ltc = pEngine.Config.FindLogType(name);
                                if (ltc == null)
                                    ltc = proj.DefaultLogType;
                                ParserProjectFile file = new ParserProjectFile(proj, location)
                                    {
                                        LogId = id,
                                        LogType = ltc,
                                        FileName = location,
                                        Created = StrUtils.NskTimestampToDateTime(tsCre),
                                        Parsed = StrUtils.NskTimestampToDateTime(tsParse)
                                    };
                                proj.LogFiles.Add(file);
                                Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("  = FindProject: add log#{0}[{1}]: {2}",
                                    proj.ProjectId, proj.DefaultLogType.Name, file.FileName) : "");
                            }
                        }
                    }
                }
            }
            return proj;
        }

        public static ParserProject CreateProject(LogFactsExtractorEngine pEngine, LogTypeConfig pLogType, string pLocation, string pComment)
        {
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("- CreateProject( logType={0}, location={1} )", pLogType.Name, pLocation) : "");

            ParserProject proj = null;

            DbProviderFactory f;
            using (DbConnection db = pEngine.GetLocalDbConnection(EDbConnectionType.Reader, pLogType, out f))
            {
                object v = DbUtils.GetMaxValueOf("ParseProject", "projectId", null, db);
                int maxId = 0;
                if (v != null)
                    maxId = (int)Convert.ChangeType(v, typeof(int));

                using (DbCommand cmd = db.CreateCommand())
                {
                    proj = new ParserProject(pEngine, pLogType, pLocation);
                    proj.ProjectId = (int)maxId + 1;

                    string stmt = string.Format(
                        "INSERT INTO ParseProject (projectId, defaultLogType, location, comment, created) VALUES ({0}, \'{1}\', \'{2}\', \'{3}\', \'{4}\')",
                        proj.ProjectId, pLogType.Name, pLocation, pComment, StrUtils.NskTimestampOf(DateTime.Now));
                    cmd.CommandText = stmt;
                    Trace.WriteLineIf(DbUtils.TrcLvl.TraceInfo, DbUtils.TrcLvl.TraceInfo ? string.Format(" * CreateProject.SQL: {0}", cmd.CommandText) : "");
                    int r = cmd.ExecuteNonQuery();
                    Trace.WriteLineIf(DbUtils.TrcLvl.TraceInfo, DbUtils.TrcLvl.TraceInfo ? string.Format("   = CreateProject.SqlResult: {0}", r) : "");
                }
            }
            return proj;
        }

        public static int ListOfProjects(LogFactsExtractorEngine pEngine, DataTable pTable)
        {
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("- ListOfProjects()") : "");

            DbProviderFactory fctry;
            using (DbConnection db = pEngine.GetLocalDbConnection(EDbConnectionType.Reader, null, out fctry))
            {
                using (DbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ParseProject ORDER BY projectId";
                    using (DbDataAdapter da = fctry.CreateDataAdapter())
                    { 
                        da.SelectCommand = cmd;
                        da.Fill(pTable);
                    }
                }
            }
            return pTable.Rows.Count;
        }

        public static int ListOfFilesForProject(LogFactsExtractorEngine pEngine, int pProjectId, DataTable pTable)
        {
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("- ListOfFilesForProject( prj={0} )", pProjectId) : "");

            DbProviderFactory fctry;
            using (DbConnection db = pEngine.GetLocalDbConnection(EDbConnectionType.Reader, null, out fctry))
            {
                using (DbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = string.Format(
                        "SELECT * FROM LogFile WHERE projectId = {0} ORDER BY projectId, logId", pProjectId);
                    using (DbDataAdapter da = fctry.CreateDataAdapter())
                    {
                        da.SelectCommand = cmd;
                        da.Fill(pTable);
                    }
                }
            }
            return pTable.Rows.Count;
        }

        public ParserProject(LogFactsExtractorEngine pEngine, LogTypeConfig pLogType, string pLocation)
        {
            this.Owner = pEngine;
            this.DefaultLogType = pLogType;
            this.Location = pLocation;
            this.LogFiles = new List<ParserProjectFile>();
        }

        public override string ToString()
        {
            return String.Format("Project[prj#{0}; {1}; type={2}; {3} files]",
                this.ProjectId, this.Location, this.DefaultLogType.Name, this.LogFiles.Count);
        }

        public LogFactsExtractorEngine Owner { get; set; }
        public int ProjectId { get; set; }
        public string Location { get; set; }
        public LogTypeConfig DefaultLogType { get; set; }

        public List<ParserProjectFile> LogFiles { get; set; }
        public int FactsCounter { get { return this.factsCounter; } }

        public void Parse()
        {
            this.DefaultLogType.ResetCounters();

            if (this.DefaultLogType.Scope == LogTypeConfig.ELogTypeScope.Directory)
            {
                parseLogsDirectory();
            }
            else
            {
                foreach (ParserProjectFile f in this.LogFiles)
                {
                    f.Parse();
                }
            }
        }

        public void DeleteFromDb()
        {
            foreach (ParserProjectFile f in this.LogFiles)
            {
                f.DeleteFromDb();
            }
            this.Owner.ExecuteSql(string.Format("DELETE FROM ParseProject WHERE projectId = {0}", this.ProjectId), this.DefaultLogType);

            this.Owner.ExecuteSql("VACUUM", this.DefaultLogType);
        }

        public void ClearDb()
        {
            this.Owner.ExecuteSql(string.Format("DELETE FROM LogFile WHERE projectId = {0}", this.ProjectId), this.DefaultLogType);

            if (!string.IsNullOrEmpty(this.DefaultLogType.TableName))
            {
                try
                {
                    this.Owner.ExecuteSql(string.Format("DELETE FROM {0} WHERE projectId = {1}", 
                        this.DefaultLogType.TableName, this.ProjectId), this.DefaultLogType);
                }
                catch (Exception exc)
                {
                    Trace.WriteLine(string.Format("ParseProj.ERR: {0}", ErrorUtils.FormatErrorMsg(exc)));
                }
            }

            this.Owner.ExecuteSql("VACUUM", this.DefaultLogType);
        }

        public void LoadFiles()
        {
            using (DataTable data = new DataTable())
            {
                ListOfFilesForProject(this.Owner, this.ProjectId, data);
                foreach (DataRow dr in data.Rows)
                {
                    ParserProjectFile f = new ParserProjectFile(this, dr["filename"].ToString()) 
                        {
                            LogId = (int)dr["logId"], 
                            FileSize = (int)dr["size"],
                            FileTime = StrUtils.NskTimestampToDateTime(dr["filetime"].ToString()),
                            Created = StrUtils.NskTimestampToDateTime(dr["created"].ToString()),
                            Parsed = StrUtils.NskTimestampToDateTime(dr["parsed"].ToString()),
                            ParseTime = (double)dr["parseTime"],
                            FactsCount = (int)dr["facts"],
                            ErrorsCount = (int)dr["errors"],
                            LastError = dr["exception"].ToString()
                        };
                    this.LogFiles.Add(f);
                }
            }
        }

        public int IndexOfFile(string pFilename)
        {
            for (int i=0; i<this.LogFiles.Count; i++)
            { 
                if (StrUtils.IsSameText(this.LogFiles[i].FileName, pFilename))
                    return i;
            }
            return -1;
        }

        public ParserProjectFile AddFile(string pFilename, LogTypeConfig pLtCfg)
        {
            ParserProjectFile f = null;
            int idx = IndexOfFile(pFilename);
            if (idx < 0)
                f = ParserProjectFile.CreateLogFile(this, pFilename, pLtCfg);
            else
                f = this.LogFiles[idx];
            return f;
        }

        public void EnsureFactRegistered(LogFact pFact)
        {
            int factId = -1;
            lock (this)
            {
                this.factsCounter++;
                factId = this.factsCounter;
                pFact.FactID = factId;
                pFact.Log.FactsCount++;
                pFact.Pattern.FactsCount++;
            }
            Trace.WriteLine(string.Format("  + Fact #{0}: t={1}, line#{2}", factId, pFact.Pattern.Name, pFact.LineNo));

            LogTypeConfig logType = this.DefaultLogType;
            if (pFact.Pattern.Owner != null)
                logType = pFact.Pattern.Owner;

            // TO-DO: insert data here...
            // $(logId), $(lineNo), $(factType), $(line), $(logTime), $(tid), $(clientId), $(rep), $(msg), $(execTime), $(replySize), $(btoReply), $(responseMsg), $(created)
            string stmt = logType.FactInsertStatement;
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(tableName)", logType.TableName);
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(projectId)", this.ProjectId.ToString());
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(logId)", pFact.Log.LogId.ToString());
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(lineNo)", pFact.LineNo.ToString());
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(factType)", AppUtils.QuoteStr(pFact.Pattern.Name));
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(line)", AppUtils.QuoteStr(pFact.Line));
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(logTime)", AppUtils.QuoteStr(pFact.LogTime));
            stmt = AppUtils.ReplaceMacroValue(stmt, "$(created)", AppUtils.QuoteStr(StrUtils.NskTimestampOf(DateTime.Now)));
            foreach (DataColumn dc in logType.TableConfig.Columns)
            {
                string macroId = string.Format("$({0})", dc.ColumnName.ToLower());
                if (stmt.ToLower().IndexOf(macroId) < 0) continue;

                bool isText = dc.DataType.Equals(typeof(string));
                string value = null;
                string tossDef;
                if (pFact.Pattern.TosserDefs.TryGetValue(dc.ColumnName.ToLower(), out tossDef))
                {
                    // explicit ref to value by #
                    if (tossDef.StartsWith("#"))
                    {
                        int vIdx = Convert.ToInt32(tossDef.Remove(0, 1));
                        if (vIdx < pFact.Values.Count)
                            value = pFact.Values[vIdx];
                        else
                            value = "{WRONG}";
                    }
                    else
                    {
                        // ref to multiple values by $(...)
                        if (tossDef.IndexOf("$(") >= 0)
                        {
                            value = tossDef;
                            for (int i = 0; i < pFact.Values.Count; i++)
                                value = value.Replace(string.Format("$({0})", i), pFact.Values[i]);
                        }
                        else // constant text
                            value = tossDef;
                    }
                }
                else
                {
                    string s;
                    if (pFact.Log.Attributes.TryGetValue(dc.ColumnName.ToLower(), out s))
                        value = s;
                    else
                        value = "null";
                }

                if (isText && value != null && value.CompareTo("null") != 0)
                    value = AppUtils.QuoteStr(value);

                if (value != null)
                    stmt = AppUtils.ReplaceMacroValue(stmt, macroId, value);
            }
            lock (pFact.Log.Attributes)
            {
                foreach (KeyValuePair<string, string> attr in pFact.Log.Attributes)
                {
                    stmt = AppUtils.ReplaceMacroValue(stmt, string.Format("$({0})", attr.Key), attr.Value);
                }
            }

            if (stmt.IndexOf("$(") >= 0)
                StrUtils.ExpandParameters(ref stmt, "$(", ")", _getPrmValue, this);

            this.Owner.WriteFact(pFact, stmt);
        }

        #region Implementation details

        private void parseLogsDirectory()
        {
            DirectoryInfo dir = new DirectoryInfo(this.Location);
            FileInfo[] files = dir.GetFiles("*.log");
            long fullSize = 0;
            foreach (FileInfo fi in files)
            {
                fullSize += fi.Length;

                ParserProjectFile f = ParserProjectFile.FindLogFile(this, fi.FullName, null);
                if (f == null)
                    f = ParserProjectFile.CreateLogFile(this, fi.FullName, null);
                this.LogFiles.Add(f);

                f.Parse();
            }
        }

        private bool _getPrmValue(string pPrmName, out string pPrmValue, object pContext)
        {
            pPrmValue = "null";
            return true;
        }

        private int factsCounter = 0;

        #endregion // Implementation details
    }


    /// <summary>
    /// 
    /// </summary>
    public class ParserProjectFile
    {
        public static TraceSwitchEx TrcLvl { get { return LogFactsExtractorEngine.TclLvl; } }         

        public static ParserProjectFile FindLogFile(ParserProject pProject, string pLocation, LogTypeConfig pLogType)
        {            
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("- FindLogFile( logType={0}, location={1} )", 
                (pLogType != null ? pLogType.Name : "(null)"), pLocation) : "");

            if (pLogType == null)
                pLogType = pProject.DefaultLogType;

            ParserProjectFile file = null;

            DbProviderFactory f;
            using (DbConnection db = pProject.Owner.GetLocalDbConnection(EDbConnectionType.Reader, pLogType, out f))
            {
                using (DbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = string.Format(
                        "SELECT * FROM LogFile WHERE projectId = {0} AND lower(filename) = \'{1}\'",
                        pProject.ProjectId, pLocation.ToLower());
                    using (DbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // logId INT, logType TEXT(20), filename TEXT, created TEXT(26), parsed TEXT(26),
                            file = new ParserProjectFile(pProject, pLocation);
                            file.LogId = (int)dr["logId"];
                            string logTypeName = dr["logType"].ToString();
                            LogTypeConfig ltc = pProject.Owner.Config.FindLogType(logTypeName);
                            if (ltc != null)
                                file.LogType = ltc;
                            file.FileName = dr["filename"].ToString();
                            file.Created = StrUtils.NskTimestampToDateTime(dr["created"].ToString());
                            file.Parsed = StrUtils.NskTimestampToDateTime(dr["parsed"].ToString());
                            file.LogFile = new FileInfo(file.FileName);

                            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("  = FindLogFile: logId={0}", file.LogId) : "");
                        }
                    }
                }
            }
            return file;
        }

        public static ParserProjectFile CreateLogFile(ParserProject pProject, string pLocation, LogTypeConfig pLogType)
        {
            Trace.WriteLineIf(ParserProject.TrcLvl.TraceInfo, ParserProject.TrcLvl.TraceInfo ? string.Format("- CreateLogFile( logType={0}, location={1} )",
                (pLogType != null ? pLogType.Name : "(null)"), pLocation) : "");

            if (pLogType == null)
                pLogType = pProject.DefaultLogType;

            ParserProjectFile file = null;

            DbProviderFactory f;
            using (DbConnection db = pProject.Owner.GetLocalDbConnection(EDbConnectionType.Reader, pLogType, out f))
            {
                int maxId = 0;
                object v = DbUtils.GetMaxValueOf("LogFile", "logId", string.Format("projectId = {0}", pProject.ProjectId), db);
                if (v != null)
                    maxId = (int)Convert.ChangeType(v, typeof(int));                

                using (DbCommand cmd = db.CreateCommand())
                {
                    file = new ParserProjectFile(pProject, pLocation) { LogType = pLogType, FileName = pLocation, Created = DateTime.Now, Parsed = new DateTime(1900,1,1) };
                    file.LogId = maxId + 1;
                    file.LogFile = new FileInfo(file.FileName);
                    DateTime fileTs = (file.LogFile.Exists ? file.LogFile.LastWriteTime : new DateTime(1900, 1, 1));

                    string stmt = string.Format(
                        "INSERT INTO LogFile (projectId, logId, logType, filename, size, filetime, created, parsed) " +
                        "  VALUES ({0}, {1}, \'{2}\', \'{3}\', {4}, \'{5}\', \'{6}\', \'{7}\')",
                        file.Project.ProjectId, file.LogId, pLogType.Name, pLocation, file.LogFile.Length,
                        StrUtils.NskTimestampOf(fileTs), 
                        StrUtils.NskTimestampOf(file.Created), StrUtils.NskTimestampOf(file.Parsed) );
                    cmd.CommandText = stmt;
                    int r = cmd.ExecuteNonQuery();
                }
            }
            return file;
        }

        public ParserProjectFile(ParserProject pProject, string pLocation)
        {
            this.Project = pProject;
            this.LogType = this.Project.DefaultLogType;
            this.FileName = pLocation;
            this.Attributes = new Dictionary<string,string>();
        }

        public override string ToString()
        {
            return String.Format("ProjFile[prj#{0}; log#{1}; {2}; sz#{3}]", 
                this.Project.ProjectId, this.LogId, Path.GetFileName(this.FileName), this.FileSize.ToString("N0"));
        }

        public ParserProject Project { get; set; }

        public int LogId { get; set; }
        public LogTypeConfig LogType { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public DateTime FileTime { get; set; }
        public DateTime Created { get; set; }
        public DateTime Parsed { get; set; }
        public double ParseTime = 0;
        public int FactsCount = 0;
        public int ErrorsCount = 0;
        public string LastError = "";

        public FileInfo LogFile { get; set; }
        public string IdPrefix { get; protected set; }

        public string RelativePath 
        {
            get 
            {
                string root = this.Project.Location.ToLower();
                string fn = this.FileName;
                if (fn.ToLower().StartsWith(root))
                {
                    if (fn.ToLower().CompareTo(root) == 0)
                        fn = Path.GetFileName(fn);
                    else
                    {
                        fn = fn.Remove(0, root.Length);
                        if (fn.StartsWith("\\") || fn.StartsWith("/"))
                            fn = fn.Remove(0, 1);
                    }
                }
                return fn;
            }
        }

        public Dictionary<string, string> Attributes { get; protected set; }

        public LogFactsExtractorEngine Engine { get { return this.Project.Owner; } }
        public LogFactsExtractorConfig Config { get { return this.Project.Owner.Config; } }

        public void DeleteFromDb()
        {
            this.Engine.ExecuteSql(string.Format("DELETE FROM LogFile WHERE projectId = {0} AND logId = {1}",
                this.Project.ProjectId, this.LogId), this.LogType);

            this.Engine.ExecuteSql(string.Format("DELETE FROM {0} WHERE projectId = {1} AND logId = {2}",
                this.LogType.TableName, this.Project.ProjectId, this.LogId), this.LogType);
        }

        public void ClearDb()
        {
            this.Engine.ExecuteSql(string.Format("DELETE FROM {0} WHERE projectId = {1} AND logId = {2}", 
                this.LogType.TableName, this.Project.ProjectId, this.LogId), this.LogType);

            //this.Engine.ExecuteSql("VACUUM", this.LogType);
        }

        public void UpdateInDb(string pUpdStmt)
        {
            if (!string.IsNullOrEmpty(pUpdStmt))
            {
                this.Engine.ExecuteSql(string.Format(
                    "UPDATE LogFile SET {0} WHERE projectId = {1} AND logId = {2}",
                    pUpdStmt, this.Project.ProjectId, this.LogId), 
                    this.LogType);
            }
        }

        public void Parse()
        {
            this.IdPrefix = string.Format("log#{0}[{1}]: ", this.LogId, Path.GetFileName(this.FileName));
            Trace.WriteLine(string.Format("+ Parse[id={0}; {1}]: waiting for pool...", this.LogId, this.FileName));
            this.Engine.ParsingPool.WaitOne();
            int apc;
            lock (this.Engine.SyncRoot)
            {
                this.Engine.ActiveParserChannels++;
                apc = this.Engine.ActiveParserChannels;
            }
            Trace.WriteLine(string.Format("  = Parse[id={0}; {1}]: entered pool (ActiveParserChannels={2})...", this.LogId, this.FileName, apc));
            try
            {
                ClearDb();
                this.ParseTime = 0;

                try
                {
                    parseLogFile();
                }
                catch (Exception exc)
                {
                    this.Parsed = StrUtils.NskTimestampToDateTime("1900-01-01:00:00:00.000000");
                    this.LastError = ErrorUtils.FormatErrorMsg(exc);
                    UpdateInDb(string.Format("parsed = \'{0}\', parseTime = {1}, facts = {2}, errors = {3}, exception = \'{4}\'",
                        StrUtils.NskTimestampOf(this.Parsed), DbUtils.FloatToDb(this.ParseTime), this.FactsCount, this.ErrorsCount,
                        ErrorUtils.FormatErrorMsg(exc) ));
                    throw;
                }

                this.Parsed = DateTime.Now;
                UpdateInDb(string.Format("parsed = \'{0}\', parseTime = {1}, facts = {2}, errors = {3}, exception = \'{4}\'",
                    StrUtils.NskTimestampOf(this.Parsed), DbUtils.FloatToDb(this.ParseTime), this.FactsCount, this.ErrorsCount, ""));
            }
            catch (Exception exc)
            {
                this.LastError = ErrorUtils.FormatErrorMsg(exc);
                string msg = string.Format("ProjParse.ERR: {0}", ErrorUtils.FormatErrorMsg(exc));
                Trace.WriteLine(msg);
                this.Engine.Progress(EProgressAction.Error, -1, this.IdPrefix + msg);
            }

            lock (this.Engine.SyncRoot)
            {
                apc = this.Engine.ActiveParserChannels;
                this.Engine.ActiveParserChannels--;
            }
            Trace.WriteLine(string.Format("+ Parse[id={0}; {1}]: releasing pool (ActiveParserChannels={2})...", this.LogId, this.FileName, apc));
            this.Engine.ParsingPool.Release();
            Trace.WriteLine(string.Format("  = Parse[id={0}; {1}]: pool released.", this.LogId, this.FileName));

            this.Reload();

            this.Engine.Progress(EProgressAction.Completed, this, this.IdPrefix + "Completed.");
        }

        public void Reload()
        {
            DbProviderFactory f;
            using (DbConnection db = this.Project.Owner.GetLocalDbConnection(EDbConnectionType.Reader, this.Project.DefaultLogType, out f))
            {
                using (DbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = string.Format(
                        "SELECT * FROM LogFile WHERE projectId = {0} AND logId = {1}",
                        this.Project.ProjectId, this.LogId);
                    using (DbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // logId INT, logType TEXT(20), filename TEXT, created TEXT(26), parsed TEXT(26),
                            string logTypeName = dr["logType"].ToString();
                            LogTypeConfig ltc = this.Project.Owner.Config.FindLogType(logTypeName);
                            if (ltc != null)
                                this.LogType = ltc;
                            this.FileName = dr["filename"].ToString();
                            this.Created = StrUtils.NskTimestampToDateTime(dr["created"].ToString());
                            this.Parsed = StrUtils.NskTimestampToDateTime(dr["parsed"].ToString());
                            this.LogFile = new FileInfo(this.FileName);

                            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(" = Reload: logId={0}", this.LogId) : "");
                        }
                    }
                }
            }
        }

        #region Implementation details

        private void parseLogFile()
        {
            if (!File.Exists(this.FileName))
                throw new Exception(string.Format("Log file[{0}] is not found", this.FileName));

            lock (this.Attributes)
                this.Attributes["logfilename"] = this.FileName;

            Trace.WriteLine(string.Format("+ Parse[id={0}; {1}]: loading file...", this.LogId, this.FileName));
            DateTime t1 = DateTime.Now;
            List<string> lines = new List<string>();
            using (StreamReader sr = File.OpenText(this.FileName))
            {
                while (!sr.EndOfStream)
                {
                    lines.Add(sr.ReadLine());
                }
            }
            DateTime t2 = DateTime.Now;
            Trace.WriteLine(string.Format(" = {0} log lines loaded. Elapsed time: {1} sec", lines.Count, (t2 - t1).TotalSeconds.ToString("N2")));

            int cnt = lines.Count;
            if (this.Config.IOQueueLength > 0)
                cnt += 100;
            this.Engine.Progress(EProgressAction.Setup, cnt, null);
            int progressStep = 1000, count = lines.Count;
            if (count < 10000 || this.Config.WorkersCount <= 1)
            {
                parseLogPortion(new TaskContext() { TaskNo = 0, Lines = lines, LogType = this.LogType, LineFrom = 0, LineTo = count - 1, ProgressStep = progressStep });
            }
            else
            {
                List<TaskContext> taskContexts = new List<TaskContext>();
                int portionSize = BitUtils.AlignTo((count + this.Config.WorkersCount) / this.Config.WorkersCount, 10);
                Trace.WriteLine(string.Format(" = workersCount:{0}; portionSize:{1}", this.Config.WorkersCount, portionSize));
                int lineFrom = 0;
                for (int iTask = 0; iTask < this.Config.WorkersCount; iTask++)
                {
                    int lineTo = lineFrom + portionSize;
                    if (lineTo > count)
                        lineTo = count;
                    TaskContext ctx = new TaskContext() { TaskNo = iTask, Lines = lines, LogType = this.LogType, LineFrom = lineFrom, LineTo = lineTo, ProgressStep = progressStep };
                    taskContexts.Add(ctx);
                    ctx.Worker = new Task(() => parseLogPortion(ctx));
                    Trace.WriteLine(string.Format(" + add task[{0} .. {1}]", lineFrom, lineTo));
                    lineFrom = lineTo;
                }
                // start parsing tasks
                Trace.WriteLine(string.Format(" * Starting tasks: {0} tasks...", taskContexts.Count));
                foreach (TaskContext ctx in taskContexts)
                {
                    ctx.Worker.Start();
                }
                // wait for tasks completion
                Trace.WriteLine(string.Format(" * Waiting for completion: {0} tasks...", taskContexts.Count));
                foreach (TaskContext ctx in taskContexts)
                {
                    ctx.Worker.Wait();
                }
            }
            DateTime t3 = DateTime.Now;
            this.ParseTime = (t3 - t2).TotalSeconds;
            Trace.WriteLine(string.Format(" * Log parsing completed. Elapsed time: {0} sec", this.ParseTime.ToString("N2")));
            
            // waiting until SQL WorkUnits queue empty...
            if (this.Config.IOQueueLength > 0)
            {
                this.Engine.FlushIoQueue(this.IdPrefix);
            }
            DateTime t4 = DateTime.Now;
            Trace.WriteLine(string.Format(" * Flushing log prasing results completed. Elapsed time: {0} sec", (t4 - t3).TotalSeconds.ToString("N2")));
            Trace.WriteLine(string.Format(" * Full completion time: {0} sec", (t4 - t1).TotalSeconds.ToString("N2")));

            Trace.WriteLine(string.Format(" i {0} total log facts extracted", this.Project.FactsCounter));
            foreach (ExtractorPattern ep in this.LogType.Patterns)
            {
                Trace.WriteLine(string.Format("   i [{0}].factsCount = {1}, errorsCount = {2}", ep.Name, ep.FactsCount, ep.ErrorsCount));
            }            
        }

        private void parseLogPortion(TaskContext pCtx)
        {
            DateTime t1 = DateTime.Now;
            Trace.WriteLine(string.Format(" +++> Task#{0}.Started ( [{1} .. {2}] )...", pCtx.TaskNo, pCtx.LineFrom, pCtx.LineTo));
            int count = pCtx.Lines.Count;
            LogFact fact = new LogFact();
            int workerCnt = this.Config.WorkersCount;
            for (int iLine = pCtx.LineFrom; iLine < pCtx.LineTo; iLine++)
            {
                int gLineNo = 0;
                lock (this)
                {
                    this.lineCounter++;
                    gLineNo = this.lineCounter;
                }
                if ((gLineNo % pCtx.ProgressStep) == 0)
                    this.Engine.Progress(EProgressAction.Step, pCtx.ProgressStep, string.Format(this.IdPrefix + "W[{0}]: {1} of {2}", workerCnt, gLineNo.ToString("N0"), count.ToString("N0")));

                string line = pCtx.Lines[iLine];

                fact.Clear();
                foreach (ExtractorPattern ep in pCtx.LogType.Patterns)
                {
                    /*DBG:
                    if (line.Contains("Message(") && ep.Name.Contains("Msg"))
                        line += "_"; */

                    if (line.IndexOf(ep.Text) >= 0)
                    {
                        Match m = ep.Rexp.Match(line);
                        if (m.Success)
                        {
                            fact.Context = pCtx;
                            fact.Pattern = ep;
                            fact.LineNo = iLine;
                            fact.Line = line;
                            fact.Log = this;
                            DateTime ts = pCtx.LogType.LogTimestampParser.ParseEx(m.Groups[1].Value, this.Attributes);
                            fact.LogTime = StrUtils.NskTimestampOf(ts);
                            int gi = 0;
                            foreach (Group mg in m.Groups)
                            {
                                if (gi > 0)
                                    fact.Values.Add(mg.Value);
                                gi++;
                            }
                            this.Project.EnsureFactRegistered(fact);
                        }
                        else
                        {
                            ep.ErrorsCount++;
                            this.ErrorsCount++;
                            Trace.WriteLine(string.Format(" Task#{0}.[{1}].ERROR for line#{2}: {3}", pCtx.TaskNo, ep.Name, iLine, line));
                        }
                        break;
                    }
                }
            }
            DateTime t2 = DateTime.Now;
            Trace.WriteLine(string.Format(" <--- Task#{0}.Finished. Elapsed time = {1} sec", pCtx.TaskNo, (t2 - t1).TotalSeconds.ToString("N2")));
        }

        private int lineCounter = 0;

        #endregion // Implementation details
    }


    /// <summary>Root point of configuration holder</summary>
    public class LogFact
    {
        public object Context;

        // refs to fact-extraction objects
        public ExtractorPattern Pattern;
        public ParserProjectFile Log;
        public int FactID;

        // standard fields for any fact of any log type
        public int LineNo;
        public string LogTime;
        public string Line;

        public List<string> Values = new List<string>();

        public void Clear()
        {
            this.Pattern = null;
            this.LineNo = -1;
            this.LogTime = null;
            this.Line = null;
            this.Values.Clear();
        }

        public override string ToString()
        {
            return String.Format("Fact[{0}]: L#{1}, T:{2}", Pattern.Name, LineNo, LogTime);
        }
    }

}
