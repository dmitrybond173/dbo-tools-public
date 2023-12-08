/*
 * Log Facts Extractor: visualization plug-in for CSLMON logs.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-08-28
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

namespace Plugin.CslmonClientsAndSessions
{
    public class LogFactsExtractorPlugin
    {
        public LogFactsExtractorPlugin()
        {
            this.info = new Dictionary<string, string>();
            this.info["Name"] = "Cslmon-ClientsAndSessions";
            this.info["LogType"] = "CSLMON-Logs";
            this.info["PerProject"] = "true";
            this.info["VisualizationTypes"] = "bitmap,svg";

            this.ServerClasses = new List<string>();
            this.SelectedServerClasses = new List<string>();

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
                this.visualParams["SvgTemplates"] = new XmlRegistry(regFn, false);
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
        public List<string> ServerClasses;
        public List<string> SelectedServerClasses;
        public string LogFilename;
        public string VisualFileID;

        public DataTable SourceData { get { return this.sourceData; } }

        public void Activate(DbConnection db, DbProviderFactory factory, string pTableName, int pProjId, int pLogId)
        {
            Trace.WriteLine(string.Format("CslmonCallsVisualization..Activate( (db), (factory), tab={0}, projId={1}, logId={2} )", 
                pTableName, pProjId, pLogId));

            this.VisualFileID = string.Format("VisualFilename_p{0}", pProjId);

            object obj;
            if (this.customParams.TryGetValue("LogFilename", out obj))
                this.LogFilename = obj.ToString();
            else
                this.LogFilename = null;

            string imgFn = null;
            if (!string.IsNullOrEmpty(this.LogFilename))
            {
                imgFn = Path.ChangeExtension(this.LogFilename, "png");
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
                        return ;
                    }
                    if (answer == DialogResult.Cancel)
                        return ;
                }
            }

            this.TableName = pTableName;
            this.ProjectId = pProjId;
            this.LogId = pLogId;

            string minTs = null, maxTs = null;
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
                        minTs = dr[0].ToString();
                        maxTs = dr[1].ToString();
                        Trace.WriteLine(string.Format("CslmonCallsVisualization.: minTs={0}, maxTs={1}", minTs, maxTs));
                    }
                }
                bool isOk = (!string.IsNullOrEmpty(minTs) && !string.IsNullOrEmpty(maxTs));
                if (!isOk)
                {
                    MessageBox.Show("Cannot determine Min/Max Log-Time!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DateTime dtMinTs = StrUtils.NskTimestampToDateTime(minTs);
                DateTime dtMaxTs = StrUtils.NskTimestampToDateTime(maxTs);
                this.visualParams["minTs"] = dtMinTs;
                this.visualParams["maxTs"] = dtMaxTs;

                this.ServerClasses.Clear();
                cmd.CommandText = string.Format(
                    "SELECT DISTINCT classId FROM {0} WHERE projectId = {1} AND factType = \'ExecSrvCall\'",
                    pTableName, pProjId
                    );
                using (DbDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        this.ServerClasses.Add(dr[0].ToString());
                    }
                }
                Trace.WriteLine(string.Format("CslmonCallsVisualization.: server classes= {0}", CollectionUtils.Join(this.ServerClasses, ";")));

                if (!this.customParams.TryGetValue("MainForm", out obj))
                    obj = null;

                if (!FormCslmonVisualizationParams.Execute((Form)obj, this.visualParams)) return;

                DateTime fromTs = DateTime.MinValue;
                DateTime toTs = DateTime.MinValue;
                if (this.visualParams.TryGetValue("minTs", out obj)) fromTs = (DateTime)obj;
                if (this.visualParams.TryGetValue("maxTs", out obj)) toTs = (DateTime)obj;

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

        private void buildVisualization()
        {
            this.info["VisualizationType"] = "bitmap";

            Visualizer viz = new Visualizer(this);
            viz.GenerateScheme();
        }

        private Dictionary<string, string> info;
        private Dictionary<string, object> customParams;
        private Dictionary<string, object> visualParams;
        private DataTable sourceData;
    }


    public sealed class PluginUtils
    {
        public static void NskTsToUi(string pTimestamp, DateTimePicker pDateUi, DateTimePicker pTimeUi)
        {
            DateTime ts = StrUtils.NskTimestampToDateTime(pTimestamp);
            pDateUi.Value = ts;
            pTimeUi.Value = ts;
            if (pTimestamp.Length > 19)
                pTimeUi.Tag = Convert.ToInt32(pTimestamp.Remove(0, 20));
        }

        public static string UiToNskTs(DateTimePicker pDateUi, DateTimePicker pTimeUi)
        {
            DateTime date = pDateUi.Value;
            DateTime time = pTimeUi.Value;
            DateTime ts = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
            int fraction = 0;
            if (pTimeUi.Tag != null)
                fraction = (int)pTimeUi.Tag;
            string nskTs = StrUtils.NskTimestampOf(ts).Substring(0, 19);
            return nskTs + "." + fraction.ToString("D6");
        }

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

        public static int TimeToSec(DateTime pTime, DateTime pBaseLine)
        {
            return (int)(pTime - pBaseLine).TotalSeconds;
        }

        /// <summary>Validates if specified time (only time, without date) fit specified range</summary>
        public static bool IsTimeInRange(DateTime pTime, DateTime pFrom, DateTime pTo)
        {
            int t = TimeToMs(pTime);
            int tfrom = TimeToMs(pFrom);
            int tto = TimeToMs(pTo);
            return (tfrom <= t && t <= tto);
        }

        public static bool IsTheSameSecond(DateTime pT1, DateTime pT2)
        {
            bool isSame = (
                pT1.Year == pT2.Year && pT1.Month == pT2.Month && pT1.Day == pT2.Day
                && pT1.Hour == pT2.Hour && pT1.Minute == pT2.Minute && pT1.Second == pT2.Second
                );
            return isSame;
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

        public static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (StrUtils.IsSameText(encoders[j].MimeType, mimeType))
                    return encoders[j];
            }
            return null;
        }

        public static void OpenFile(string pFilename)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = pFilename;
            psi.Verb = "open";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }
    }
}
