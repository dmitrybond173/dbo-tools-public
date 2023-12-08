/*
 * Log Facts Extractor: visualization plug-in for CSLMON logs.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-08-28
 * 
 * See also:
 *   https://stackoverflow.com/questions/38597121/how-do-i-rotate-a-label-in-vb-net
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XService.Utils;

namespace Plugin.CslmonClientsAndSessions
{
    public class Visualizer
    {
        public Visualizer(LogFactsExtractorPlugin pOwner)
        {
            this.Owner = pOwner;

            this.Executors = new List<Executor>();
            this.ExecutorRefs = new Dictionary<string, Executor>();

            object obj;
            if (this.Owner.GetVisualParams().TryGetValue("minTs", out obj)) this.From = (DateTime)obj;
            if (this.Owner.GetVisualParams().TryGetValue("maxTs", out obj)) this.To = (DateTime)obj;
        }

        public LogFactsExtractorPlugin Owner { get; private set; }

        public DateTime From = DateTime.MinValue;
        public DateTime To = DateTime.MinValue;
        public List<Executor> Executors { get; protected set; }
        public Dictionary<string, Executor> ExecutorRefs { get; protected set; }

        public void Clear()
        {
            this.ExecutorRefs.Clear();
            this.Executors.Clear();
        }

        public void GenerateScheme()
        {
            object obj; 
            string visType = null;
            if (this.Owner.GetVisualParams().TryGetValue("VisualizationType", out obj))
                visType = obj.ToString();

            if (string.IsNullOrEmpty(visType) || StrUtils.IsSameText(visType, "bitmap"))
                generateBitmap();
            else if (StrUtils.IsSameText(visType, "svg"))
                generateSvg();
        }

        private void generateSvg()
        {
            // [005. Приручаем SVG – Лев Солнцев] https://www.youtube.com/watch?time_continue=4&v=2DRu77MC6Ns

            MessageBox.Show("CslmonCallsVisualization:generateSvg - not implemented!",
                "Information", 
                MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        private void generateBitmap()
        {
            this.Clear();

            // loop over log facts and build statistic
            int cnt = 0;
            DateTime t1 = DateTime.Now;
            foreach (DataRow dr in this.Owner.SourceData.Rows)
            {
                //Trace.WriteLine(string.Format("CslmonCallsVisualization: + row# {0}...", this.Owner.SourceData.Rows.IndexOf(dr) ));
                Collect(new FactRecord(dr));
                cnt++;
            }
            DateTime t2 = DateTime.Now;
            Trace.WriteLine(string.Format("CslmonCallsVisualization: {0} fact records processed. Elapsed time = {1} sec", cnt, (t2-t1).TotalSeconds.ToString("N2")));
            //progressor(EProgressAction.Setup, this.Scheme.Clients.Count, "Preparing drawing...");

            int timeDepth;
            DateTime schemaMinTs, schemaMaxTs;
            this.GetMetrics(out schemaMinTs, out schemaMaxTs, out timeDepth);
            if (schemaMinTs < this.From)
                schemaMinTs = this.From;
            if (schemaMaxTs < this.To)
                schemaMaxTs = this.To;
            schemaMinTs = PluginUtils.AdjustTimeByBoundOfMinutes(schemaMinTs, 5);
            this.From = schemaMinTs;
            int schemaLowTv = 0; // PluginUtils.TimeToSec(schemaMinTs);
            int schemaHighTv = PluginUtils.TimeToSec(schemaMaxTs, schemaMinTs);
            timeDepth = (((schemaHighTv - schemaLowTv) + 299) / 300) * 300;
            int columnWidth = 5;
            Point offestTopLeft = new Point(50, 10);
            Point offestBottomRight = new Point(10, 10);
            Size sz = new Size(
                offestTopLeft.X + offestBottomRight.X + this.Executors.Count * (columnWidth + 2), 
                offestTopLeft.Y + offestBottomRight.Y + timeDepth
                );
            Trace.WriteLine(string.Format("CslmonCallsVisualization: {0} clients object discovered. suggested bmp size=({1} x {2})", 
                this.Executors.Count, sz.Width, sz.Height));

            Bitmap bmp;
            try
            {
                bmp = new Bitmap(sz.Width, sz.Height, PixelFormat.Format32bppArgb); //PixelFormat.Format16bppRgb555);
            }
            catch (Exception exc)
            {
                MessageBox.Show(string.Format("Fail to create bitmap [{2} x {3}]!{0}{1}",
                    Environment.NewLine, ErrorUtils.FormatErrorMsg(exc), sz.Width, sz.Height),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ;
            }
            
            Graphics g = Graphics.FromImage(bmp);
            Brush brBack = new SolidBrush(Color.White);
            g.FillRectangle(brBack, 0, 0, sz.Width, sz.Height);

            Pen penLightDiv = new Pen(Color.FromArgb(200, 0xF8, 0xE0, 0xE0));
            Pen penNormalDiv = new Pen(Color.FromArgb(240, 0xE0, 0xC8, 0xD0));
            Pen penHeavyDiv = new Pen(Color.FromArgb(250, 0x90, 0xA8, 0xA0));
            Font fntLabel = new Font("Courier New", 7);
            Font fntLabelA = new Font("Arial", 6);
            Brush brLabel = new SolidBrush(Color.Navy);

            for (int iDiv = schemaLowTv; iDiv < schemaHighTv; iDiv++)
            {
                bool isMinute = ((iDiv % 60) == 0);
                bool isMin5 = ((iDiv % 300) == 0);
                bool isHour = ((iDiv % 3600) == 0);
                Pen p = (isHour ? penHeavyDiv : (isMin5 ? penNormalDiv : (isMinute ? penLightDiv : null) ) );
                if (p != null)
                {
                    int y = iDiv - schemaLowTv;
                    g.DrawLine(p, 8, offestTopLeft.Y + y, sz.Width - 8, offestTopLeft.Y + y);
                    if (isMin5 || isHour)
                    {
                        DateTime tLab = schemaMinTs + new TimeSpan(0, 0, y);
                        g.DrawString(string.Format("{0:0#}:{1:0#}:{2:0#}", tLab.Hour, tLab.Minute, tLab.Second), fntLabel, brLabel, new PointF(0, y));
                    }
                }
            }
                
            Pen penClientLife = new Pen(Color.FromArgb(255, 0x70, 0x70, 0x70));

            Brush brMsg = new SolidBrush(Color.Red);
            Brush brColumn = new SolidBrush(Color.FromArgb(128, 0xA0, 0xD0, 0xD0));
            Brush brLifetime = new SolidBrush(Color.FromArgb(200, 0xD0, 0xD0, 0xD0)); // Color.Silver);
            Brush brTxCommit = new SolidBrush(Color.FromArgb(170, 0x00, 0x80, 0x00)); // Color.Green);
            Brush brTxAbort = new SolidBrush(Color.FromArgb(170, 0xE8, 0x00, 0x00)); // Color.Red);
            Brush brSrvCallNormal = new SolidBrush(Color.FromArgb(170, 0x5F, 0x9E, 0xA0)); // Color.CadetBlue);
            Brush brSrvCallHighlight = new SolidBrush(Color.Orange);

            int cliIdx = 0;
            //progressor(EProgressAction.Step, 1, "");
            List<string> execRefs = new List<string>();
            foreach (Executor cli in this.Executors)
            {
                string clientCaption = string.Format("#{0} = (#{1}) {2}{3}", cliIdx + 1, cli.LogId, cli.ID, (cli.Crashed ? " /crashed!" : ""));

                Trace.WriteLine(string.Format(" #{0}: {1} /{2}", cliIdx, cli.ToString(), clientCaption));
                
                bool topCut = false, bottomCut = false;
                DateTime cliMinTs, cliMaxTs;
                cli.GetMetrics(out cliMinTs, out cliMaxTs);
                if (cliMinTs < this.From)
                {
                    cliMinTs = this.From;
                    topCut = true;
                }
                if (cliMaxTs < this.From)
                {
                    cliMaxTs = this.To;
                    bottomCut = true;
                }
                int cliLowTv = PluginUtils.TimeToSec(cliMinTs, schemaMinTs);
                int cliHighTv = PluginUtils.TimeToSec(cliMaxTs, schemaMinTs);

                int x = offestTopLeft.X + cliIdx * (columnWidth + 2);
                int yStart = offestTopLeft.Y + (cliLowTv - schemaLowTv);
                int yFinish = offestTopLeft.Y + (cliHighTv - schemaLowTv);

                // paint column line
                Pen pn = ((cliIdx > 0 && (cliIdx % 10) == 0) ? penHeavyDiv : ((cliIdx > 0 && (cliIdx % 5) == 0) ? penNormalDiv : penLightDiv));
                g.DrawLine(pn, x - 1, offestTopLeft.Y, x - 1, sz.Height - offestBottomRight.Y);
                if (topCut)
                    g.DrawLine(penHeavyDiv, x, offestTopLeft.Y, x + columnWidth, offestTopLeft.Y);
                if (bottomCut)
                    g.DrawLine(penHeavyDiv, x, sz.Height - offestBottomRight.Y, x + columnWidth, sz.Height - offestBottomRight.Y);

                // paint executor life-time
                Rectangle r = new Rectangle(x, yStart, columnWidth, yFinish - yStart);
                g.FillRectangle(brLifetime, r);

                // paint column label
                g.DrawString((cliIdx + 1).ToString(), fntLabelA, brLabel, x, offestTopLeft.Y);
                execRefs.Add(clientCaption);

                // paint transaction scopes
                for (int iTx = 0; iTx < cli.Transactions.Count; iTx++)
                {
                    Executor.TxScope tx = cli.Transactions[iTx];
                    int txStart = PluginUtils.TimeToSec(tx.Started, schemaMinTs);
                    DateTime txEnd = tx.Finished;
                    if (!tx.Closed)
                    {
                        // when TX was not closed - then use next start of TX as end-time or end of executor 
                        if ((iTx + 1) < cli.Transactions.Count)
                            txEnd = cli.Transactions[iTx + 1].Started;
                        else
                            txEnd = cli.Finished;
                    }
                    int txFinish = PluginUtils.TimeToSec(txEnd, schemaMinTs);
                    if ((iTx + 1) < cli.Transactions.Count && PluginUtils.IsTheSameSecond(txEnd, cli.Transactions[iTx + 1].Started))
                        txFinish--; // forcely cut 1 second when 2 subsequent transactions finished and started at the same second

                    g.FillRectangle(tx.Commit ? brTxCommit : brTxAbort,
                        x + 1, offestTopLeft.Y + txStart, 1, (txFinish - txStart));
                    g.FillRectangle(tx.Commit ? brTxCommit : brTxAbort,
                        x, offestTopLeft.Y + txStart, 2, 1);
                }

                // put server-calls
                for (int iCall = 0; iCall < cli.Calls.Count; iCall++)
                {
                    Executor.SrvCall sc = cli.Calls[iCall];
                    int scStart = PluginUtils.TimeToSec(sc.Started, schemaMinTs);
                    DateTime scEnd = sc.Finished;
                    if (!sc.Closed)
                    {
                        // when TX was not closed - then use next start of TX as end-time or end of executor 
                        if ((iCall + 1) < cli.Calls.Count)
                            scEnd = cli.Calls[iCall + 1].Started;
                        else
                            scEnd = cli.Finished;
                    }
                    int scFinish = PluginUtils.TimeToSec(scEnd, schemaMinTs);
                    if ((iCall + 1) < cli.Calls.Count && PluginUtils.IsTheSameSecond(scEnd, cli.Calls[iCall + 1].Started))
                        scFinish--; // forcely cut 1 second when 2 subsequent calls finished and started at the same second
                    Brush br = (isHighlightedSrvCls(sc.SrvName) ? brSrvCallHighlight : brSrvCallNormal);
                    g.FillRectangle(br, x + 3, offestTopLeft.Y + scStart, 1, (scFinish - scStart));
                    g.FillRectangle(br, x + 3, offestTopLeft.Y + scStart, 2, 1);
                }

                // paint crash-indicator
                if (cli.Crashed)
                    g.FillRectangle(brTxAbort, x, yFinish, columnWidth + 1, 2);

                cliIdx++;

                //int progressStep = 10;
                //if ((cliIdx % progressStep) == 0)
                //    progressor(EProgressAction.Step, progressStep, string.Format("{0} of {1}", cliIdx, this.Scheme.Clients.Count));
            }
            Trace.WriteLine(string.Format("CslmonCallsVisualization: visualization refs:\n{0}", CollectionUtils.Join(execRefs, "\n\t")));

            string fn = this.Owner.LogFilename;
            if (string.IsNullOrEmpty(fn))
            {
                string tsId = StrUtils.CompactNskTimestampOf(DateTime.Now).Substring(0, 15).Replace(",", "-");
                fn = Environment.ExpandEnvironmentVariables(string.Format("%TEMP%\\log-scheme-{0}.bmp", tsId));
            }

            fn = Path.ChangeExtension(fn, string.Format(".{0}.png", this.Owner.ProjectId));

            Trace.WriteLine(string.Format("CslmonCallsVisualization: Saving [{0}] image...", fn));
            bmp.Save(fn, ImageFormat.Png);
            Trace.WriteLine(string.Format("CslmonCallsVisualization: image saved."));

            this.Owner.GetParams()[this.Owner.VisualFileID] = fn;

            PluginUtils.OpenFile(fn);

            MessageBox.Show("Refs to executors:\n" + CollectionUtils.Join(execRefs, "\n"),
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void GetMetrics(out DateTime pMinTs, out DateTime pMaxTs, out int pTimeDepth)
        {
            pMinTs = DateTime.MaxValue;
            pMaxTs = DateTime.MinValue;
            foreach (Executor cli in this.Executors)
            {
                DateTime tsMin, tsMax;
                cli.GetMetrics(out tsMin, out tsMax);

                if (tsMin < pMinTs) pMinTs = tsMin;
                if (tsMax > pMaxTs) pMaxTs = tsMax;
            }
            if (pMinTs == DateTime.MaxValue)
                pMinTs = DateTime.MinValue;

            //int secMin = PluginUtils.TimeToSec(pMinTs);
            //int secMax = PluginUtils.TimeToSec(pMaxTs);
            //pTimeDepth = (secMax - secMin);

            pTimeDepth = (int)(pMaxTs - pMinTs).TotalSeconds;
        }

        public void Collect(FactRecord pItem)
        {
            Executor cli;
            if (!this.ExecutorRefs.TryGetValue(pItem.ColumnID, out cli))
            {
                cli = new Executor() { ID = pItem.ColumnID };
                this.ExecutorRefs[cli.ID] = cli;
                this.Executors.Add(cli);
            }
            cli.Collect(pItem);
        }

        private bool isHighlightedSrvCls(string pSrvName)
        {
            return this.Owner.SelectedServerClasses.Contains(pSrvName.ToLower());
        }

        public class FactRecord
        {
            //it.LogTime, it.TID, it.FactType, it.ClientId, it.REP, it.ExecTime 
            public int LogId;
            public DateTime Time;
            public DateTime AppStart;
            public int PID;
            public string TID;
            public string FactType;
            public string ClassId;

            public string ColumnID
            {
                get { return string.Format("{0}-{1}", StrUtils.CompactNskTimestampOf(this.AppStart).Substring(0, 15), this.PID); }
            }

            public FactRecord(DataRow pData)
            {
                this.LogId = (int)pData["LogId"];
                this.Time = StrUtils.NskTimestampToDateTime(pData["LogTime"].ToString());
                this.AppStart = StrUtils.NskTimestampToDateTime(pData["AppStart"].ToString());
                this.PID = (int)pData["PID"];
                this.TID = pData["TID"].ToString();
                this.FactType = pData["FactType"].ToString();
                this.ClassId = pData["ClassId"].ToString();
            }
        }

        public class Executor
        {
            public int LogId = -1;
            public string ID;
            public DateTime Started = DateTime.MinValue;
            public DateTime Finished = DateTime.MinValue;
            public bool Crashed;
            public List<SrvCall> Calls = new List<SrvCall>();
            public List<TxScope> Transactions = new List<TxScope>();

            public void GetMetrics(out DateTime pMinValidTs, out DateTime pMaxValidTs)
            {
                pMinValidTs = this.Started;
                pMaxValidTs = this.Finished;
            }

            public void Collect(FactRecord pItem)
            {
                this.Started = pItem.AppStart;
                this.Finished = pItem.Time;
                if (this.LogId < 0)
                    this.LogId = pItem.LogId;

                switch (pItem.FactType)
                {
                    case "ExecSrvCall":
                        {
                            SrvCall c = new SrvCall();
                            this.Calls.Add(c);
                            c.Started = pItem.Time;
                            c.SrvName = pItem.ClassId;
                        }
                        break;
                    case "ExecSrvCallDone":
                        {
                            if (this.Calls.Count > 0)
                            {
                                SrvCall c = this.Calls[this.Calls.Count - 1];
                                c.Finished = pItem.Time;
                                c.Closed = true;
                            }
                        }
                        break;

                    case "ExecTx":
                        {
                            bool isTxBegin = StrUtils.IsSameText(pItem.ClassId, "Begin WORK");
                            TxScope tx;
                            if (isTxBegin)
                            {
                                tx = new TxScope();
                                this.Transactions.Add(tx);
                                tx.Started = pItem.Time;
                            }
                            else
                            {
                                if (this.Transactions.Count > 0)
                                {
                                    tx = this.Transactions[this.Transactions.Count - 1];
                                    tx.Finished = pItem.Time;
                                    tx.Commit = StrUtils.IsSameText(pItem.ClassId, "Commit WORK");
                                    tx.Closed = true; // so, TX was explicitly finished
                                }
                            }
                        }
                        break;

                    case "ExecCrash":
                        this.Crashed = true;
                        break;
                }
            }

            public override string ToString()
            {
                return String.Format("<client id={0} logId={1} {2} tx={3} calls={4} />",
                    this.ID, this.LogId, (this.Crashed ? "crashed=yes" : ""),
                    this.Transactions.Count, this.Calls.Count 
                    );
            }

            public class SrvCall
            {
                public string SrvName;
                public DateTime Started;
                public DateTime Finished;
                public bool Closed = false;
            }

            public class TxScope
            {
                public DateTime Started;
                public DateTime Finished;
                public bool Closed = false;
                public bool Commit;
            }
        }

    }
}
