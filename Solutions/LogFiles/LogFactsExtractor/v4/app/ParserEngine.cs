/*
 * Log Facts Extractor: main engine classes - document objects.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-05-25
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;
//using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using XService.Data;
using XService.Utils;

namespace LogFactExtractor4
{
    public enum EProgressAction { Setup, Step, Completed, Error };
    public delegate void ProgressMethod(EProgressAction pAction, object pValue, string pMsg);

    /// <summary></summary>
    public class LogFactsExtractorEngine
    {
        public static TraceSwitchEx TclLvl = new TraceSwitchEx("TraceLevel", "TraceLevel");

        public LogFactsExtractorEngine()
        {
            DbUtils.DumpListOfAdoNetProviders();

            initDefault();            

            this.isAlive = true;
        }

        public ProgressMethod Progressor;

        public object SyncRoot = new object();
        public int ActiveParserChannels = 0;
        public int MaxChannelsInPool { get; protected set; }
        public Semaphore ParsingPool { get; protected set; }
        public List<PluginDescriptor> Plugins { get; protected set; }

        public void Progress(EProgressAction pAction, object pValue, string pMsg)
        {
            if (this.Progressor != null)
                this.Progressor(pAction, pValue, pMsg);
        }

        public void ReleaseSemaphore(int pCount)
        {
            Trace.WriteLine(string.Format("ReleaseSemaphore( {0} )", pCount));
            if (pCount <= 0)
                pCount = this.MaxChannelsInPool;
            for (int i = 0; i < pCount; i++)
            {
                try { this.ParsingPool.Release(1); }
                catch (Exception exc)
                {
                    Trace.WriteLine(string.Format("ReleaseSemaphore[#{0}].ERR: {1}", i, ErrorUtils.FormatErrorMsg(exc)));
                    break; 
                }
            }
        }

        #region Configuration

        public void LoadConfig()
        {
            string fn = PathUtils.IncludeTrailingSlash(TypeUtils.ApplicationHomePath) + "LogFactsExtractor.cfg";
            Trace.WriteLine(string.Format("--- ConfigDeSerializer: loading [{0}] ...", fn));
            XmlSerializer serializer = new XmlSerializer(typeof(LogFactsExtractorConfig),
                new Type[] { 
                    typeof(LogTypeConfig), typeof(ExtractorPattern)
                });
            serializer.UnknownElement += serializer_UnknownElement;
            serializer.UnknownAttribute += serializer_UnknownAttribute;
            serializer.UnknownNode += serializer_UnknownNode;
            serializer.UnreferencedObject += serializer_UnreferencedObject;
            using (FileStream fs = new FileStream(fn, FileMode.Open))
            {
                LogFactsExtractorConfig cfg = (LogFactsExtractorConfig)serializer.Deserialize(fs);
                if (cfg != null)
                    this.Config = cfg;
            }
            //this.Config.Owner = this;
            this.Config.AfterDeserialization();

            // (re)create I/O queues
            this.isAlive = false;
            Thread.Sleep(350);
            this.isAlive = true;

            Trace.WriteLine(string.Format("--- initializing {0} SQL-queue workers ...", this.Config.IOQueueLength));
            for (int i = 0; i < this.Config.IOQueueLength; i++)
                ThreadPool.QueueUserWorkItem(this.sqlQueueHandler, i);

            this.MaxChannelsInPool = this.Config.ParsersPoolChannels;
            this.ParsingPool = new Semaphore(0, this.MaxChannelsInPool);
            Trace.WriteLine(string.Format("= ParsingPoolChannels: {0}", this.MaxChannelsInPool));

            scanForPlugins();

            Trace.WriteLine(string.Format("--- Config initializing completed."));
        }

        private void serializer_UnreferencedObject(object sender, UnreferencedObjectEventArgs e)
        {
            Trace.WriteLine(string.Format("!Serializer.Err[UnreferencedObject]: id={0}, obj={1}", e.UnreferencedId, e.UnreferencedObject.ToString()));
        }

        private void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Trace.WriteLine(string.Format("!Serializer.Err[UnknownNode]: pos=[L:{0}, C:{1}], node={2}, type={3}, txt={4}", e.LineNumber, e.LinePosition, e.Name, e.NodeType, e.Text));
        }

        private void serializer_UnknownElement(object sender, XmlElementEventArgs e)
        {
            Trace.WriteLine(string.Format("!Serializer.Err[UnknownElement]: pos=[L:{0}, C:{1}], expected={2}, xml={3}", e.LineNumber, e.LinePosition, e.ExpectedElements, e.Element.OuterXml));
        }

        private void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            Trace.WriteLine(string.Format("!Serializer.Err[UnknownAttribute]: pos=[L:{0}, C:{1}], expected={2}, xml={3}", e.LineNumber, e.LinePosition, e.ExpectedAttributes, e.Attr.OuterXml));
        }

        public void SaveConfig()
        {
            string fn = PathUtils.IncludeTrailingSlash(TypeUtils.ApplicationHomePath) + "LogFactsExtractor.cfg";
            XmlSerializer serializer = new XmlSerializer(typeof(LogFactsExtractorConfig),
                new Type[] { 
                    typeof(LogTypeConfig), typeof(ExtractorPattern)
                });
            using (TextWriter writer = new StreamWriter(fn))
            {
                serializer.Serialize(writer, this.Config);
            }
        }

        public LogTypeConfig SelectLogType(string pFilename)
        {
            string fn = Path.GetFileName(pFilename);
            foreach (LogTypeConfig lt in this.Config.LogTypes)
            {
                //if (lt.Scope != LogTypeConfig.ELogTypeScope.File) continue;

                Regex rexp = lt.LogFilenamePatternRexp;
                if (rexp != null && (rexp.IsMatch(pFilename) || rexp.IsMatch(fn)))
                    return lt;
            }

            return null;
        }

        public LogFactsExtractorConfig Config { get; protected set; }

        #endregion // Configuration

        #region DB-Layer

        public DbConnection GetLocalDbConnection(EDbConnectionType pConnectionType, LogTypeConfig pLogType, out DbProviderFactory pFactory)
        {
            DbConnection db = getLocalDbConnection(pConnectionType, pLogType);
            pFactory = DbUtils.LocalDbFactory;
            return db;
        }

        public void ClearDb()
        {
            DbProviderFactory dbFactory;
            using (DbConnection db = this.GetLocalDbConnection(EDbConnectionType.Inserter, null, out dbFactory))
            {
                int r;
                using (DbCommand cmd = db.CreateCommand())
                {
                    foreach (LogTypeConfig lt in this.Config.LogTypes)
                    {
                        cmd.CommandText = string.Format("DELETE FROM {0}", lt.TableName);
                        try
                        {
                            r = cmd.ExecuteNonQuery();
                            Trace.WriteLine(string.Format(" * LogType[{0}].ClearTable: {1}. Result: {2}", lt.Name, lt.TableName, r));
                        }
                        catch (Exception exc)
                        {
                            Trace.WriteLine(string.Format(" * LogType[{0}].ERR: {1}", lt.Name, ErrorUtils.FormatErrorMsg(exc)));
                        }
                    }

                    /*
                    cmd.CommandText = "DELETE FROM LogParse";
                    r = cmd.ExecuteNonQuery();
                    Trace.WriteLine(string.Format(" * CommonDb.ClearTable: LogParse. Result: {0}", r));
                    */

                    cmd.CommandText = "DELETE FROM LogFile";
                    r = cmd.ExecuteNonQuery();
                    Trace.WriteLine(string.Format(" * CommonDb.ClearTable: LogFile. Result: {0}", r));

                    cmd.CommandText = "VACUUM";
                    r = cmd.ExecuteNonQuery();
                    Trace.WriteLine(string.Format(" * CommonDb.ClearTable: LogFile. Result: {0}", r));
                }
            }
        }

        public int ExecuteSql(string pStmt, LogTypeConfig pLogType)
        {
            lock (this)
            {
                if (this.localDb == null)
                    this.localDb = getLocalDbConnection(EDbConnectionType.Inserter, pLogType);
            }
            using (DbCommand cmd = this.localDb.CreateCommand())
            {
                cmd.CommandText = pStmt;
                int r = cmd.ExecuteNonQuery();
                Trace.WriteLine(string.Format("    = SQL[{0}]: ResultCode: {1}", pLogType.Name, r));
                return r;
            }
        }

        public void WriteFact(LogFact pFact, string pStmt)
        {
            if (this.Config.IOQueueLength > 0)
            {
                SqlTaskContext wu = new SqlTaskContext()
                {
                    Task = (TaskContext)pFact.Context,
                    Caption = string.Format("Fact#{0}/{1}", pFact.FactID, pFact.Pattern.Name),
                    Statement = pStmt
                };
                lock (this.sqlsQueue)
                    this.sqlsQueue.Enqueue(wu);
                Trace.WriteLine(string.Format("  = Enqued SQL WU# {0}. QLen={1}", wu.ID, this.sqlsQueue.Count));
            }
            else
            {
                lock (this)
                {
                    if (this.localDb == null)
                        this.localDb = getLocalDbConnection(EDbConnectionType.Inserter, pFact.Pattern.Owner);
                }
                using (DbCommand cmd = this.localDb.CreateCommand())
                {
                    cmd.CommandText = pStmt;
                    int r = cmd.ExecuteNonQuery();
                    Trace.WriteLine(string.Format("    = Fact #{0}: ResultCode: {1}", pFact.FactID, r));
                }
            }
        }

        public void FlushIoQueue(string pSrcId)
        {
            this.Progress(EProgressAction.Step, 0, pSrcId + "Waiting to flush I/O queue...");
            int startCnt = -1;
            while (true)
            {
                lock (this.sqlsQueue)
                {
                    if (startCnt < 0)
                        startCnt = this.sqlsQueue.Count;
                    int delta = Math.Abs(startCnt - this.sqlsQueue.Count);
                    if ((delta % 100) == 0)
                        this.Progress(EProgressAction.Step, 0, string.Format(pSrcId + "Waiting to flush I/O queue: {0} of {1} ...", delta, startCnt));
                    if (this.sqlsQueue.Count == 0)
                        break;
                }
                Thread.Sleep(330);
            }
            this.Progress(EProgressAction.Step, 0, pSrcId + "Flushing of I/O queue completed.");
        }

        #endregion // DB-Layer

        #region Implementation details

        private void initDefault()
        {
            this.Config = new LogFactsExtractorConfig();
            this.Plugins = new List<PluginDescriptor>();
        }

        #region DB-Layer

        private DbConnection getLocalDbConnection(EDbConnectionType pConnectionType, LogTypeConfig pLogType)
        {
            DbConnection conn = DbUtils.CreateLocalDbConnection(pConnectionType, pLogType);

            ensureInfrastructureDbCreated(conn);

            if (pLogType != null)
                pLogType.EnsureFactsTable(conn); //ensureFactsTableCreated(pLogType, conn);

            Trace.WriteLine(string.Format(" = LocalDB: connection initialized and validated."));
            return conn;
        }

        private void ensureInfrastructureDbCreated(DbConnection conn)
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                DataTable tab = new DataTable();
                Trace.WriteLine(string.Format("! Validate if infrastructure tables exists..."));
                try
                {
                    DbUtils.EnsureTableConfigLoaded(conn, tab, "ParseProject", "projectId = -1");
                    DbUtils.EnsureTableConfigLoaded(conn, tab, "LogFile", "projectId = -1");
                }
                catch (Exception exc)
                {
                    Trace.WriteLine(string.Format(" ! ERR: {0}", ErrorUtils.FormatErrorMsg(exc)));

                    List<string> dbInitStatements = new List<string>();
                    AppUtils.TextToStatements(this.Config.CreateTablesStatement, dbInitStatements, ';');
                    int idx = 0;
                    foreach (string stmt in dbInitStatements)
                    {
                        cmd.CommandText = stmt;
                        Trace.WriteLine(string.Format("! InitDb[#{0}]: {1}", idx, stmt));
                        int r = cmd.ExecuteNonQuery();
                        Trace.WriteLine(string.Format("  = InitDb[#{0}]->result: {1}", idx, r));
                        idx++;
                    }

                    DbUtils.EnsureTableConfigLoaded(conn, tab, "ParseProject", "projectId = -1");
                    DbUtils.EnsureTableConfigLoaded(conn, tab, "LogFile", "projectId = -1");
                }
            }
        }

        private void sqlQueueHandler(object ctx)
        {
            int wqIdx = (int)ctx;
            Trace.WriteLine(string.Format(" ! SqlQueueHandler[#{0}] started (pri={1})...", wqIdx, Thread.CurrentThread.Priority));

            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            Trace.WriteLine(string.Format(" ! SqlQueueHandler[#{0}]: new pri={1}", wqIdx, Thread.CurrentThread.Priority));

            int neCounter = 0;
            DbConnection conn = null;
            DbCommand cmd = null;
            while (this.isAlive)
            {
                SqlTaskContext sqlWorkUnit = null;
                lock (this.sqlsQueue)
                {
                    if (this.sqlsQueue.Count > 0)
                    {
                        sqlWorkUnit = this.sqlsQueue.Dequeue();
                        Trace.WriteLine(string.Format("  = Wrk#{0}: SQU-WU #{1} dequeued...", wqIdx, sqlWorkUnit.ID));
                    }
                }

                if (sqlWorkUnit != null)
                {
                    if (cmd == null)
                    {
                        conn = getLocalDbConnection(EDbConnectionType.Inserter, sqlWorkUnit.Task.LogType);
                        Trace.WriteLine(string.Format("  ! Wrk#{0}: Created db-connection.", wqIdx));
                        cmd = conn.CreateCommand();
                        Trace.WriteLine(string.Format("  ! Wrk#{0}: Created db-command.", wqIdx));
                    }
                    cmd.CommandText = sqlWorkUnit.Statement;
                    Trace.WriteLine(string.Format("  +++ Wrk#{0}: SQL stmt: {1}", wqIdx, cmd.CommandText));
                    int r = cmd.ExecuteNonQuery();
                    Trace.WriteLine(string.Format("  +++ Wrk#{0}: [{1}]->ResultCode: {2}", wqIdx, sqlWorkUnit.Caption, r));
                }

                if (sqlWorkUnit != null)
                {
                    neCounter++;
                    if ((neCounter % 10) == 0)
                        Thread.Sleep(1);
                }
                else
                {
                    Thread.Sleep(3);
                    neCounter = 0;
                }
            }
            if (cmd != null)
                cmd.Dispose();
            if (conn != null)
                conn.Dispose();
            Trace.WriteLine(string.Format(" ! SqlQueueHandler[#{0}]: finished.", wqIdx, Thread.CurrentThread.Priority));
        }

        #endregion // DB-Layer

        private void scanForPlugins()
        {
            DirectoryInfo dir = new DirectoryInfo(TypeUtils.ApplicationHomePath);
            FileInfo[] files = dir.GetFiles("Plugin.*.dll");
            foreach (FileInfo f in files)
            {
                PluginDescriptor descr;
                Assembly asm;
                try 
                { 
                    asm = Assembly.LoadFile(f.FullName);
                    descr = new PluginDescriptor() { Asm = asm };
                    string asmName = asm.GetName().Name;

                    Type[] types = asm.GetTypes();
                    Type t = AppUtils.FindType(types, "LogFactsExtractorPlugin");
                    if (t == null)
                        t = AppUtils.FindType(types, "Plugin");
                    descr.PluginType = t;

                    if (!(descr != null && descr.PluginType != null))
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (missing type)", asmName));
                     
                    ConstructorInfo ci = descr.PluginType.GetConstructor(Type.EmptyTypes);
                    if (ci == null)
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (missing constructor)", asmName));
                    descr.PluginObj = ci.Invoke(null);
                    if (descr.PluginObj == null)
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (fail to construct object)", asmName));

                    MemberInfo[] miArr = descr.PluginType.GetMember("GetInfo", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
                    if (!(miArr != null && miArr.Length > 0))
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (fail to find GetInfo method)", asmName));
                    MethodInfo mi = (MethodInfo)miArr[0];
                    descr.Info = (Dictionary<string, string>)mi.Invoke(descr.PluginObj, null);
                    if (descr.Info == null)
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (invalid plugin info)", asmName));

                    miArr = descr.PluginType.GetMember("GetParams", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
                    if (!(miArr != null && miArr.Length > 0))
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (fail to find GetInfo method)", asmName));
                    mi = (MethodInfo)miArr[0];
                    descr.CustomParams = (Dictionary<string, object>)mi.Invoke(descr.PluginObj, null);
                    if (descr.CustomParams == null)
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (invalid plugin params)", asmName));

                    bool isOk = ( descr.Info.TryGetValue("Name", out descr.Name) && descr.Info.TryGetValue("LogType", out descr.LogTypeName) ) 
                        && ( !string.IsNullOrEmpty(descr.Name) && !string.IsNullOrEmpty(descr.LogTypeName) );
                    if (!isOk)
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (invalid plugin info - missing fields)", asmName));
                    descr.LogTypeRef = this.Config.FindLogType(descr.LogTypeName);

                    string s;
                    if (descr.Info.TryGetValue("PerProject", out s))
                        descr.PerFile = !StrUtils.GetAsBool(s);

                    miArr = descr.PluginType.GetMember("Activate", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
                    if (!(miArr != null && miArr.Length > 0))
                        throw new Exception(string.Format("Assembly[{0}] - wrong plugin API (no activation method found)", asmName));
                    descr.Activation = (MethodInfo)miArr[0];

                    this.Plugins.Add(descr);
                }
                catch (Exception exc)
                {
                    Trace.WriteLine(string.Format("scanForPlugins[{0}].ERR: {1}", f.Name, ErrorUtils.UnrollExceptionMsg(exc, true)));
                }
            }
        }

        private bool isAlive;
        private DbConnection localDb;
        //private int logId, parseId;
        //private int factCounter = 0;
        //private int lineCounter = 0;
        private Queue<SqlTaskContext> sqlsQueue = new Queue<SqlTaskContext>();

        #endregion // Implementation details
    }

    public class PluginDescriptor
    {
        public Assembly Asm;
        public Type PluginType;
        public object PluginObj;
        public Dictionary<string, string> Info;
        public Dictionary<string, object> CustomParams;
        public MethodInfo Activation;

        public string Name;
        public string LogTypeName;
        public LogTypeConfig LogTypeRef;
        public bool PerFile = true;

        public void Activate(DbConnection db, DbProviderFactory factory, string pTableName, int pProjId, int pLogId)
        {
            if (this.Activation != null)
            {
                try
                {
                    this.Activation.Invoke(this.PluginObj, new object[] { db, factory, pTableName, pProjId, pLogId });
                }
                catch (Exception exc)
                {
                    string msg = ErrorUtils.UnrollExceptionMsg(exc, true);
                    Trace.WriteLine(string.Format("Plugin.ERR: {0}", msg));
                    MessageBox.Show(msg, "Plugin Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
