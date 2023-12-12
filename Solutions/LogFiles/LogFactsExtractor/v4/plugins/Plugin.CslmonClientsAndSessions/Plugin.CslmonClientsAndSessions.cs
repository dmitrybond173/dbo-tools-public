/*
 * Log Facts Extractor: visualization plug-in for CSLMON logs.
 * 
 * Plugin entry point 
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2023-12-06
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XService.Utils;
using PAL;
using System.Xml;

namespace Plugin.CslmonClientsAndSessions
{
    public class LogFactsExtractorPlugin
    {
        public LogFactsExtractorPlugin()
        {
            this.Doc = new Document();

            this.info = new Dictionary<string, string>();
            this.info["Name"] = "Cslmon-ClientsAndSessions";
            this.info["LogType"] = "CSLMON-Logs";
            this.info["PerProject"] = "true";
            this.info["VisualizationTypes"] = "svg,bitmap";

            this.customParams = new Dictionary<string, object>();

            this.visualParams = new Dictionary<string, object>();
            this.visualParams["Plugin"] = this;
            //this.visualParams["ServerClasses"] = this.ServerClasses;
            //this.visualParams["SelectedServerClasses"] = this.SelectedServerClasses;
            this.visualParams["VisualizationType"] = "svg";

            Assembly asm = Assembly.GetExecutingAssembly();
            string fn = asm.GetName().Name + ".Templates.xml";
            string regFn = PathUtils.IncludeTrailingSlash(TypeUtils.ApplicationHomePath) + fn;
            if (File.Exists(regFn))
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(regFn);
                this.visualParams["SvgTemplates"] = dom; // new XmlRegistry(regFn, false);
            }
        }

        public Dictionary<string, string> GetInfo()
        {
            return this.info;
        }

        public Dictionary<string, object> GetParams()
        {
            return this.customParams;
        }

        public Dictionary<string, object> GetVisualParams()
        {
            return this.visualParams;
        }

        public string TableName;
        public int ProjectId;
        public int LogId;
        public string LogFilename;
        public string VisualFileID;
        public Document Doc { get; protected set; }

        public DataTable SourceData { get { return this.sourceData; } }

        public void Activate(DbConnection db, DbProviderFactory factory, string pTableName, int pProjId, int pLogId)
        {
            Trace.WriteLine(string.Format("CslmonCaS..Activate( (db), (factory), tab={0}, projId={1}, logId={2} )",
                pTableName, pProjId, pLogId));

            this.dbComn = db;
            this.Doc.Clear();
            this.Doc.ProjectId = pProjId;
            this.Doc.LogFileId = pLogId;

            this.VisualFileID = string.Format("VisualFilename_p{0}", pProjId);

            object obj;
            if (this.customParams.TryGetValue("LogFilename", out obj))
                this.LogFilename = obj.ToString();
            else
                this.LogFilename = null;

            string imgFn = null;
            if (!string.IsNullOrEmpty(this.LogFilename))
            {
                imgFn = Path.ChangeExtension(this.LogFilename, "svg");
            }

            if (!string.IsNullOrEmpty(imgFn) && File.Exists(imgFn))
            {
                FileInfo fi = new FileInfo(imgFn);
                fi.Refresh();
                if (fi.Exists)
                {
                    DialogResult answer = MessageBox.Show(
                        string.Format(
                            "Visual file [{1} @ {2}] for [Project#{3}, Log#{4}] already exists!" +
                            "{0}Click [Yes] - to open existing file." +
                            "{0}Click [No] - to generate new visualization file.",
                            Environment.NewLine, fi.Name, StrUtils.NskTimestampOf(fi.CreationTime).Substring(0, 19), pProjId, pLogId
                            ),
                        "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (answer == DialogResult.Yes)
                    {
                        PluginUtils.OpenFile(fi.FullName);
                        return;
                    }
                    if (answer == DialogResult.Cancel)
                        return;
                }
            }

            this.TableName = pTableName;
            this.ProjectId = pProjId;
            this.LogId = pLogId;

            //string minTs = null, maxTs = null;
            using (DbCommand cmd = db.CreateCommand())
            {
                // LogId should be ignored!
                cmd.CommandText = string.Format(
                    "SELECT min(logtime), max(logtime) FROM {0} WHERE projectId = {1}",
                    pTableName, pProjId
                    );
                using (DbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        this.Doc.MinLogTs = dr[0].ToString();
                        this.Doc.MaxLogTs = dr[1].ToString();
                        Trace.WriteLine(string.Format("CslmonCaS.: minTs={0}, maxTs={1}", this.Doc.MinLogTs, this.Doc.MaxLogTs));
                    }
                }
                bool isOk = (!string.IsNullOrEmpty(this.Doc.MinLogTs) && !string.IsNullOrEmpty(this.Doc.MaxLogTs));
                if (!isOk)
                {
                    MessageBox.Show("Cannot determine Min/Max Log-Time!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DateTime dtMinTs = StrUtils.NskTimestampToDateTime(this.Doc.MinLogTs);
                DateTime dtMaxTs = StrUtils.NskTimestampToDateTime(this.Doc.MaxLogTs);
                this.visualParams["minTs"] = dtMinTs;
                this.visualParams["maxTs"] = dtMaxTs;

                //this.ServerClasses.Clear();
                cmd.CommandText = string.Format(
                    "SELECT * FROM CslmonCaS " +
                    " WHERE projectId = {0} " + 
                    " ORDER BY lineno",
                    this.Doc.ProjectId
                    );
                using (DbDataReader dr = cmd.ExecuteReader())
                {
                    this.Doc.Load(dr, this.dbComn);
                }
                Trace.WriteLine(string.Format("CslmonCaS: {0} clients", this.Doc.Clients.Count));

                if (!this.customParams.TryGetValue("MainForm", out obj))
                    obj = null;

                if (!FormCslmonVisualizationParams.Execute((Form)obj, this.visualParams)) return;

                DateTime fromTs = DateTime.MinValue;
                DateTime toTs = DateTime.MaxValue;
                if (this.visualParams.TryGetValue("minTs", out obj)) fromTs = (DateTime)obj;
                if (this.visualParams.TryGetValue("maxTs", out obj)) toTs = (DateTime)obj;
                this.Doc.MinLogTs = StrUtils.NskTimestampOf(fromTs);
                this.Doc.MaxLogTs = StrUtils.NskTimestampOf(toTs);

                if (this.visualParams.TryGetValue("sortByInitTime", out obj)) this.Doc.SortByInitTime = (bool)obj;

                if (this.Doc.SortByInitTime)
                {
                    this.Doc.Clients.Clear();
                    foreach (List<Client> it in this.Doc.ClientGroups)
                    {
                        it.Sort(cmpByInitTime);
                        this.Doc.Clients.AddRange(it);
                    }
                }

                string extraCondition = "";
                if (!dtMinTs.Equals(fromTs))
                    extraCondition += string.Format(" AND logTime >= \'{0}\'", StrUtils.NskTimestampOf(fromTs));
                if (!dtMaxTs.Equals(toTs))
                    extraCondition += string.Format(" AND logTime <= \'{0}\'", StrUtils.NskTimestampOf(toTs));

                cmd.CommandText = string.Format(
                    "SELECT * FROM {0} WHERE projectId = {1} {2} {3} ORDER BY logId, logTime",
                    pTableName, pProjId, (pLogId > 0 ? string.Format("AND logId = {0}", pLogId) : ""), extraCondition
                    );
                using (DbDataAdapter da = factory.CreateDataAdapter())
                {
                    da.SelectCommand = cmd;

                    if (this.sourceData != null)
                    {
                        this.sourceData.Dispose();
                        this.sourceData = null;
                    }
                    this.sourceData = new DataTable();
                    da.Fill(this.sourceData);
                }
                if (this.sourceData.Rows.Count == 0)
                {
                    MessageBox.Show("No data found with specified conditions!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            buildVisualization();
        }

        private int cmpByInitTime(Client c1, Client c2)
        {
            return c1.FirstSessionInitTs.CompareTo(c2.FirstSessionInitTs);
        }

        private void buildVisualization()
        {
            this.info["VisualizationType"] = "bitmap";

            Visualizer viz = new Visualizer(this);
            viz.GenerateScheme();
        }

        private void buildClientsSchedule(DbDataReader dr)
        {
            this.Doc.Load(dr, this.dbComn);
        }

        private DbConnection dbComn;
        private Dictionary<string, string> info;
        private Dictionary<string, object> customParams;
        private Dictionary<string, object> visualParams;
        private DataTable sourceData;
    }


}
