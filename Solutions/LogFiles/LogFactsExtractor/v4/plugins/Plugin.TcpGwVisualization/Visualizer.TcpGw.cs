/*
 * Log Facts Extractor: visualization plug-in for TCP-GW logs.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2019-08-24
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

namespace Plugin.TcpGwVisualization
{
    public class Visualizer
    {
        public Visualizer(LogFactsExtractorPlugin pOwner)
        {
            this.Owner = pOwner;

            this.Clients = new List<Client>();
            this.ClientRefs = new Dictionary<int, Client>();
        }

        public LogFactsExtractorPlugin Owner { get; private set; }

        public List<Client> Clients { get; protected set; }
        public Dictionary<int, Client> ClientRefs { get; protected set; }

        public void Clear()
        {
            this.ClientRefs.Clear();
            this.Clients.Clear();
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
            MessageBox.Show("TcpGwVisualization:generateSvg - not implemented!",
                "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        private void generateBitmap()
        {
            this.Clear();

            // loop over log facts and build statistic
            int cnt = 0;
            foreach (DataRow dr in this.Owner.SourceData.Rows)
            {
                Collect(new FactRecord(dr));
                cnt++;
            }
            Trace.WriteLine(string.Format("TcpGwVisualization: {0} fact records processed", cnt));
            
            //progressor(EProgressAction.Setup, this.Scheme.Clients.Count, "Preparing drawing...");

            int timeDepth;
            DateTime schemaMinTs, schemaMaxTs;
            this.GetMetrics(out schemaMinTs, out schemaMaxTs, out timeDepth);
            schemaMinTs = PluginUtils.AdjustTimeByBoundOfMinutes(schemaMinTs, 5);
            int schemaLowTv = PluginUtils.TimeToSec(schemaMinTs);
            int schemaHighTv = PluginUtils.TimeToSec(schemaMaxTs);
            Point offestTopLeft = new Point(50, 10);
            Point offestBottomRight = new Point(10, 10);
            Size sz = new Size(offestTopLeft.X + offestBottomRight.X + this.Clients.Count * 2, offestTopLeft.Y + offestBottomRight.Y + timeDepth);
            Trace.WriteLine(string.Format("TcpGwVisualization: {0} clients object discovered. suggested bmp size=({1} x {2})", this.Clients.Count, sz.Width, sz.Height));

            Bitmap bmp;
            try
            {
                bmp = new Bitmap(sz.Width, sz.Height, PixelFormat.Format16bppRgb555);
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
                    int deltaX = (isHour ? - 20 : (isMin5 ? -10 : 0));
                    g.DrawLine(p, offestTopLeft.X + deltaX, offestTopLeft.Y + y, sz.Width - offestBottomRight.X, offestTopLeft.Y + y);
                    if (isMin5 || isHour)
                    {
                        DateTime tLab = schemaMinTs + new TimeSpan(0, 0, y);
                        g.DrawString(string.Format("{0:0#}:{1:0#}:{2:0#}", tLab.Hour, tLab.Minute, tLab.Second), fntLabel, brLabel, new PointF(0, y));
                    }
                }
            }

            for (int iC = 0; iC < this.Clients.Count; iC++)
            { 
                bool is100 = (iC % 100 == 0);
                bool is10 = (!is100 && iC % 10 == 0);
                bool is5 = (!is10 && iC % 10 == 0);
                Pen p = (is100 ? penHeavyDiv : (is10 ? penNormalDiv : (is5 ? penLightDiv : null) ) );
                if (p != null)
                {
                    int x = offestTopLeft.X + iC * 2;
                    g.DrawLine(p, x, offestTopLeft.Y, x, sz.Height - offestBottomRight.Y);
                }
            }
                
            Pen penClientLife = new Pen(Color.FromArgb(255, 0x70, 0x70, 0x70));

            Brush brMsg = new SolidBrush(Color.Red);
            Brush brCslWait = new SolidBrush(Color.Orange);
            Brush brCslExec = new SolidBrush(Color.Magenta);

            int cliIdx = 0;
            //progressor(EProgressAction.Step, 1, "");
            foreach (Client cli in this.Clients)
            {
                DateTime cliMinTs, cliMaxTs;
                cli.GetMetrics(out cliMinTs, out cliMaxTs);
                Trace.WriteLine(string.Format(" #{0}: {1}", cliIdx, cli.ToString()));

                int cliLowTv = PluginUtils.TimeToSec(cliMinTs);
                int cliHighTv = PluginUtils.TimeToSec(cliMaxTs);

                int y, x = offestTopLeft.X + cliIdx * 2;

                // client life-time - is a base line (as background for all other operations)
                int yStart = offestTopLeft.Y + (cliLowTv - schemaLowTv);
                int yFinish = offestTopLeft.Y + (cliHighTv - schemaLowTv);
                g.DrawLine(penClientLife, x, yStart, x, yFinish);

                // msg-received - should be first
                if (PluginUtils.IsTimeInRange(cli.MsgReceived, cliMinTs, cliMaxTs))
                {
                    y = 10 + (PluginUtils.TimeToSec(cli.MsgReceived) - schemaLowTv);
                    g.FillRectangle(brMsg, x, y, 1, 1);
                }

                // cal-wait - should be second
                if (PluginUtils.IsTimeInRange(cli.CslWait, cliMinTs, cliMaxTs) && PluginUtils.IsTimeInRange(cli.CslAccess, cliMinTs, cliMaxTs))
                {
                    yStart = 10 + (PluginUtils.TimeToSec(cli.CslWait) - schemaLowTv);
                    yFinish = 10 + (PluginUtils.TimeToSec(cli.CslAccess) - schemaLowTv);
                    g.FillRectangle(brCslWait, x, yStart, 1, (yFinish - yStart));
                }

                // csl-access
                if (PluginUtils.IsTimeInRange(cli.CslAccess, cliMinTs, cliMaxTs))
                {
                    double execT = cli.ExecTime;
                    if (execT < 1) execT = 1;
                    TimeSpan tdiff = new TimeSpan(0, 0, 0, 0, (int)(execT * 1000.0));
                    DateTime tsCslEnd = cli.CslAccess + tdiff;
                    yStart = 10 + (PluginUtils.TimeToSec(cli.CslAccess) - schemaLowTv);
                    yFinish = 10 + (PluginUtils.TimeToSec(tsCslEnd) - schemaLowTv);
                    g.FillRectangle(brCslExec, x, yStart, 1, (yFinish - yStart));
                }

                cliIdx++;
                int progressStep = 10;
                //if ((cliIdx % progressStep) == 0)
                //    progressor(EProgressAction.Step, progressStep, string.Format("{0} of {1}", cliIdx, this.Scheme.Clients.Count));
            }

            string fn = this.Owner.LogFilename;
            if (string.IsNullOrEmpty(fn))
            {
                // _123456789_123456789
                // 20190828,112233.333444
                string tsId = StrUtils.CompactNskTimestampOf(DateTime.Now).Substring(0, 15).Replace(",", "-");
                fn = Environment.ExpandEnvironmentVariables(string.Format("%TEMP%\\log-scheme-{0}.bmp", tsId));
            }

            fn = Path.ChangeExtension(fn, ".png");

            Trace.WriteLine(string.Format("TcpGwVisualization: Saving [{0}] image...", fn));
            bmp.Save(fn, ImageFormat.Png);
            Trace.WriteLine(string.Format("TcpGwVisualization: Image saved."));

            this.Owner.GetParams()[this.Owner.VisualFileID] = fn;

            PluginUtils.OpenFile(fn);
        }

        public void GetMetrics(out DateTime pMinTs, out DateTime pMaxTs, out int pTimeDepth)
        {
            pMinTs = DateTime.MaxValue;
            pMaxTs = DateTime.MinValue;
            foreach (Client cli in this.Clients)
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
            Client cli;
            if (!this.ClientRefs.TryGetValue(pItem.ClientId, out cli))
            {
                cli = new Client() { ID = pItem.ClientId };
                this.ClientRefs[cli.ID] = cli;
                this.Clients.Add(cli);
            }
            cli.Collect(pItem);
        }


        public class FactRecord
        {
            //it.LogTime, it.TID, it.FactType, it.ClientId, it.REP, it.ExecTime 
            public DateTime Time;
            public string TID;
            public string FactType;
            public int ClientId;
            public string REP;
            public double ExecTime;

            public FactRecord(DataRow pData)
            {
                this.Time = StrUtils.NskTimestampToDateTime(pData["LogTime"].ToString());
                this.TID = pData["TID"].ToString();
                this.FactType = pData["FactType"].ToString();
                this.ClientId = (int)pData["ClientID"];
                this.REP = pData["REP"].ToString();

                if (!pData.IsNull("ExecTime"))
                    this.ExecTime = (double)pData["ExecTime"];
            }
        }

        public class Client
        {
            public int ID;
            public string REP;
            public DateTime Connected;
            public DateTime Disposed;
            public DateTime MsgReceived;
            public DateTime CslWait;
            public DateTime CslAccess;
            public double WaitTime;
            public double ExecTime;

            public double SessionLiveTime
            {
                get { return (this.Disposed - this.Connected).TotalSeconds; }
            }

            public void GetMetrics(out DateTime pMinValidTs, out DateTime pMaxValidTs)
            {
                pMinValidTs = DateTime.MaxValue;
                pMaxValidTs = DateTime.MinValue;

                DateTime ts = this.Connected;
                if (ts != DateTime.MinValue)
                {
                    if (ts < pMinValidTs) pMinValidTs = ts;
                    if (ts > pMaxValidTs) pMaxValidTs = ts;
                }

                ts = this.Disposed;
                if (ts != DateTime.MinValue)
                {
                    if (ts < pMinValidTs) pMinValidTs = ts;
                    if (ts > pMaxValidTs) pMaxValidTs = ts;
                }

                ts = this.MsgReceived;
                if (ts != DateTime.MinValue)
                {
                    if (ts < pMinValidTs) pMinValidTs = ts;
                    if (ts > pMaxValidTs) pMaxValidTs = ts;
                }

                ts = this.CslWait;
                if (ts != DateTime.MinValue)
                {
                    if (ts < pMinValidTs) pMinValidTs = ts;
                    if (ts > pMaxValidTs) pMaxValidTs = ts;
                }

                ts = this.CslAccess;
                if (ts != DateTime.MinValue)
                {
                    if (ts < pMinValidTs) pMinValidTs = ts;
                    if (ts > pMaxValidTs) pMaxValidTs = ts;
                }

                // if still not found pMinValidTs - then need to reset it to DateTime.MinValue
                if (pMinValidTs == DateTime.MaxValue)
                    pMinValidTs = DateTime.MinValue;
            }

            public void Collect(FactRecord pItem)
            {
                switch (pItem.FactType)
                {
                    case "TcpGwClient":
                        this.Connected = pItem.Time;
                        this.REP = pItem.REP;
                        break;
                    case "TcpGwDispose":
                        this.Disposed = pItem.Time;
                        break;

                    case "TcpGwMsg":
                        this.MsgReceived = pItem.Time;
                        break;

                    case "TcpGwCslWait":
                        this.CslWait = pItem.Time;
                        break;

                    case "TcpGwCslAccess":
                        this.CslAccess = pItem.Time;
                        this.WaitTime = pItem.ExecTime;
                        break;

                    case "TcpGwCslReply":
                        this.ExecTime = pItem.ExecTime;
                        break;
                }
            }

            public override string ToString()
            {
                return String.Format("<client id={0} live={1} wait={2} exec={3} cre={4} disp={5} msg={6} cslWait={7} cslAcc={8} />",
                    this.ID,
                    this.SessionLiveTime.ToString("N2"), this.WaitTime.ToString("N2"), this.ExecTime.ToString("N2"),
                    StrUtils.CompactNskTimestampOf(this.Connected).Substring(0,18),
                    StrUtils.CompactNskTimestampOf(this.Disposed).Substring(0, 18), 
                    StrUtils.CompactNskTimestampOf(this.MsgReceived).Substring(0, 18),
                    StrUtils.CompactNskTimestampOf(this.CslWait).Substring(0, 18), 
                    StrUtils.CompactNskTimestampOf(this.CslAccess).Substring(0, 18)
                    );
            }
        }

    }
}
