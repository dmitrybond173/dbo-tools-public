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

namespace Plugin.CslmonSyncVisualization
{
    public class Visualizer
    {
        public Visualizer(LogFactsExtractorPlugin pOwner)
        {
            this.Owner = pOwner;

            this.CrSections = new List<CrSection>();
            this.SelectedCrSections = new List<CrSection>();
            this.CrSectionRefs = new Dictionary<string, CrSection>();

            object obj;
            this.Params = this.Owner.GetVisualParams();
            if (this.Params.TryGetValue("minTs", out obj)) this.From = (DateTime)obj;
            if (this.Params.TryGetValue("maxTs", out obj)) this.To = (DateTime)obj;
            if (this.Params.TryGetValue("SelectedSyncNames", out obj)) this.SyncNames = (List<string>)obj;
        }

        public LogFactsExtractorPlugin Owner { get; private set; }

        public Dictionary<string, object> Params { get; private set; }
        public DateTime From = DateTime.MinValue;
        public DateTime To = DateTime.MinValue;
        public List<string> SyncNames { get; private set; }
        public List<CrSection> CrSections { get; protected set; }
        public List<CrSection> SelectedCrSections { get; protected set; }
        public Dictionary<string, CrSection> CrSectionRefs { get; protected set; }

        public void Clear()
        {
            this.CrSectionRefs.Clear();
            this.CrSections.Clear();
        }

        public void GenerateScheme()
        {
            object obj; 
            string visType = null;
            if (this.Params.TryGetValue("VisualizationType", out obj))
                visType = obj.ToString();

            if (string.IsNullOrEmpty(visType) || StrUtils.IsSameText(visType, "bitmap"))
                generateBitmap();
            else if (StrUtils.IsSameText(visType, "svg"))
                generateSvg();
        }

        private void buildStructures()
        {
            // loop over log facts and build statistic
            int cnt = 0;
            DateTime t1 = DateTime.Now;
            foreach (DataRow dr in this.Owner.SourceData.Rows)
            {
                //Trace.WriteLine(string.Format("CslmonSyncVisualization: + row# {0}...", this.Owner.SourceData.Rows.IndexOf(dr) ));
                Collect(new FactRecord(dr));
                cnt++;
            }
            foreach (CrSection sect in this.CrSections)
                sect.Loaded();

            if (this.SyncNames != null)
            {
                this.SelectedCrSections.Clear();
                foreach (CrSection sect in this.CrSections)
                {
                    sect.DisplayIndex = this.SyncNames.IndexOf(sect.ID.ToLower());
                    if (sect.DisplayIndex >= 0)
                        this.SelectedCrSections.Add(sect);
                }
            }
            else
                this.SelectedCrSections.AddRange(this.CrSections.ToArray());

            DateTime t2 = DateTime.Now;
            Trace.WriteLine(string.Format("CslmonSyncVisualization: {0} fact records processed. Elapsed time = {1} sec", cnt, (t2 - t1).TotalSeconds.ToString("N2")));
            //progressor(EProgressAction.Setup, this.Scheme.Clients.Count, "Preparing drawing...");
        }

        private void generateSvg()
        {
            // [005. Приручаем SVG – Лев Солнцев] https://www.youtube.com/watch?time_continue=4&v=2DRu77MC6Ns

            MessageBox.Show("CslmonSyncVisualization:generateSvg - not implemented!",
                "Information", 
                MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        private static StringFormat vertLab_drwFmt = new System.Drawing.StringFormat() { FormatFlags = StringFormatFlags.DirectionVertical };

        private void generateBitmap()
        {
            this.Clear();

            buildStructures();

            int columnWidth = 20;

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

            Point offestTopLeft = new Point(10, 90);
            Point offestBottomRight = new Point(10, 10);
            Size sz = new Size(
                offestTopLeft.X + offestBottomRight.X + this.SelectedCrSections.Count * (columnWidth + 2), 
                offestTopLeft.Y + offestBottomRight.Y + timeDepth
                );
            Trace.WriteLine(string.Format("CslmonSyncVisualization: {0} clients object discovered. suggested bmp size=({1} x {2})", 
                this.CrSections.Count, sz.Width, sz.Height));

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
            Font fntLabelA = new Font("Arial", 8);
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
                        g.DrawString(string.Format("{0:0#}:{1:0#}:{2:0#}", tLab.Hour, tLab.Minute, tLab.Second), fntLabel, brLabel, new PointF(0, y + offestTopLeft.Y));
                    }
                }
            }
                
            Pen penClientLife = new Pen(Color.FromArgb(255, 0x70, 0x70, 0x70));

            Pen penClosed = new Pen(Color.Silver); // Color.FromArgb(255, 0x70, 0x70, 0x70));
            Pen penLock1 = new Pen(Color.Red); //Color.FromArgb(255, 0xC0, 0x60, 0xA0));
            Pen penLock2 = new Pen(Color.Cyan); //Color.FromArgb(255, 0xC0, 0xA0, 0x60));

            Brush brMsg = new SolidBrush(Color.Red);
            Brush brColumn = new SolidBrush(Color.FromArgb(128, 0xA0, 0xD0, 0xD0));
            Brush brLifetime = new SolidBrush(Color.FromArgb(200, 0xD0, 0xD0, 0xD0)); // Color.Silver);

            int cliIdx = 0;
            //progressor(EProgressAction.Step, 1, "");
            List<string> execRefs = new List<string>();
            foreach (CrSection sect in this.SelectedCrSections)
            {
                Trace.WriteLine(string.Format(" #{0}", cliIdx, sect.ToString()));

                int x = sect.DisplayIndex * columnWidth;

                string id = StrUtils.GetAfterPattern(sect.ID, ":");
                SizeF txtSz = g.MeasureString(id, fntLabelA);
                g.DrawString(id, fntLabelA, brLabel, new PointF(x, offestTopLeft.Y - txtSz.Width - 2), vertLab_drwFmt);

                foreach (CrSection.CrAccess acc in sect.CallsTotal)
                {
                    int xDelta = 0;
                    int yStart = PluginUtils.TimeToSec(acc.Entering, schemaMinTs);
                    int yEnd;
                    if (!acc.IsEntered) { yEnd = schemaHighTv; xDelta = 1; }
                    else if (!acc.IsLeaved) { yEnd = schemaHighTv; xDelta = 2; }
                    else yEnd = PluginUtils.TimeToSec(acc.Leaved, schemaMinTs);

                    Pen pn = (!acc.IsLocked ? penClosed : (acc.IsEntered ? penLock1 : penLock2));
                    g.DrawLine(pn, offestTopLeft.X + x + xDelta, offestTopLeft.Y + yStart, offestTopLeft.X + x + xDelta, offestTopLeft.Y + yEnd);
                }
                
                cliIdx++;
                //int progressStep = 10;
                //if ((cliIdx % progressStep) == 0)
                //    progressor(EProgressAction.Step, progressStep, string.Format("{0} of {1}", cliIdx, this.Scheme.Clients.Count));
            }
            Trace.WriteLine(string.Format("CslmonSyncVisualization: visualization refs:\n{0}", CollectionUtils.Join(execRefs, "\n\t")));

            string fn = this.Owner.LogFilename;
            if (string.IsNullOrEmpty(fn))
            {
                string tsId = StrUtils.CompactNskTimestampOf(DateTime.Now).Substring(0, 15).Replace(",", "-");
                fn = Environment.ExpandEnvironmentVariables(string.Format("%TEMP%\\log-scheme-{0}.bmp", tsId));
            }

            fn = Path.ChangeExtension(fn, string.Format(".{0}.png", this.Owner.ProjectId));

            Trace.WriteLine(string.Format("CslmonSyncVisualization: Saving [{0}] image...", fn));
            bmp.Save(fn, ImageFormat.Png);
            Trace.WriteLine(string.Format("CslmonSyncVisualization: image saved."));

            this.Owner.GetParams()[this.Owner.VisualFileID] = fn;

            PluginUtils.OpenFile(fn);

            MessageBox.Show("Refs to crSections:\n" + CollectionUtils.Join(execRefs, "\n"),
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void GetMetrics(out DateTime pMinTs, out DateTime pMaxTs, out int pTimeDepth)
        {
            pMinTs = DateTime.MaxValue;
            pMaxTs = DateTime.MinValue;
            foreach (CrSection cli in this.CrSections)
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
            CrSection cli;
            if (!this.CrSectionRefs.TryGetValue(pItem.ColumnID, out cli))
            {
                cli = new CrSection() { ID = pItem.ColumnID };
                this.CrSectionRefs[cli.ID] = cli;
                this.CrSections.Add(cli);
            }
            cli.Collect(pItem);
        }

        private bool isHighlightedSrvCls(string pSrvName)
        {
            return this.Owner.SelectedSyncNames.Contains(pSrvName.ToLower());
        }

        /// <summary>
        /// Parsed fact record
        /// </summary>
        public class FactRecord
        {
            //it.LogTime, it.TID, it.FactType, it.ClientId, it.REP, it.ExecTime 
            public int LogId;
            public int LineNo;
            public DateTime Time;
            public string FactType;
            public string TID;
            public string SyncId;
            public int WaitTime;
            public string Info;

            public override string ToString()
            {
                return String.Format("Fact[#{0}; #{1}; {2}; {3}; tid={4}; sync={5}; {6}]",
                    this.LogId, this.LineNo, this.FactType, StrUtils.CompactNskTimestampOf(this.Time), this.TID, this.SyncId, this.Info);
            }

            public string ColumnID
            {
                get { return this.SyncId; }
            }

            public FactRecord(DataRow pData)
            {
                this.LogId = (int)pData["LogId"];
                this.LineNo = (int)pData["LineNo"];
                this.Time = StrUtils.NskTimestampToDateTime(pData["LogTime"].ToString());
                this.TID = pData["TID"].ToString();
                this.FactType = pData["FactType"].ToString();
                this.SyncId = pData["SyncId"].ToString();
                this.WaitTime = (pData.IsNull("WaitTime") ? -1 : (int)pData["WaitTime"]);
                this.Info = pData["SyncId"].ToString();
            }
        }


        /// <summary>
        /// Holder of all access calls related to certain critical section
        /// </summary>
        public class CrSection
        {
            public int LogId = -1;
            public int DisplayIndex = -1;
            public string ID;
            public DateTime Start = DateTime.MinValue;
            public DateTime Finish = DateTime.MinValue;
            public bool Crashed;
            public Dictionary<string, List<CrAccess>> Calls = new Dictionary<string, List<CrAccess>>();
            public List<CrAccess> CallsTotal = new List<CrAccess>();
            public CrAccess LastCall = null;

            public override string ToString()
            {
                return String.Format("CrSection[ id={0} logId={1} {2} threads={3} ]",
                    this.ID, this.LogId, (this.LastCall != null && !this.LastCall.Closed ? "locked=yes" : ""),
                    this.Calls.Count
                    );
            }

            public void GetMetrics(out DateTime pMinValidTs, out DateTime pMaxValidTs)
            {
                pMinValidTs = this.Start;
                pMaxValidTs = this.Finish;
            }

            public CrAccess GetTopCall(string pTID)
            { 
                List<CrAccess> list;
                if (this.Calls.TryGetValue(pTID, out list) && list.Count > 0)
                    return list[list.Count - 1];
                return null;
            }

            public List<CrAccess> EnsureList(string pTID)
            {
                List<CrAccess> list;
                if (!this.Calls.TryGetValue(pTID, out list))
                {                
                    list = new List<CrAccess>();
                    this.Calls[pTID] = list;
                }
                return list;
            }

            public void Loaded()
            {
                this.CallsTotal.Clear();
                foreach (KeyValuePair<string, List<CrAccess>> it in this.Calls)
                {
                    foreach (CrAccess acc in it.Value)
                        this.CallsTotal.Add(acc);
                }
                this.CallsTotal.Sort(cmpByLineNo);
            }

            private static int cmpByLineNo(CrAccess it1, CrAccess it2)
            {
                if (it1.LineStart < it2.LineStart)
                    return -1;
                else if (it1.LineStart > it2.LineStart)
                    return 1;
                else 
                    return 0;
            }

            public void Collect(FactRecord pItem)
            {
                if (this.Start == DateTime.MinValue || this.Start < pItem.Time)
                    this.Start = pItem.Time;
                if (this.Finish == DateTime.MinValue || this.Finish > pItem.Time)
                    this.Finish = pItem.Time;

                if (this.LogId < 0)
                    this.LogId = pItem.LogId;

                if (this.ID == null)
                    this.ID = pItem.SyncId;

                CrAccess acc = null;
                switch (pItem.FactType)
                {
                    case "Entering":
                        {
                            List<CrAccess> list = EnsureList(pItem.TID);
                            acc = new CrAccess();
                            list.Add(acc);
                            acc.LineStart = pItem.LineNo;
                            acc.Entering = pItem.Time;
                            acc.TID = pItem.TID;
                        }
                        break;
                    case "Entered":
                        {
                            acc = this.GetTopCall(pItem.TID);
                            if (acc != null)
                            {
                                acc.Entered = pItem.Time;
                                acc.LineFinish = pItem.LineNo;
                            }
                        }
                        break;
                    case "Leaved":
                        {
                            acc = this.GetTopCall(pItem.TID);
                            if (acc != null)
                            {
                                acc.Leaved = pItem.Time;
                                acc.LineFinish = pItem.LineNo;
                                acc.Closed = true;
                            }
                        }
                        break;
                }
                if (acc != null)
                    this.LastCall = acc;
            }

            /// <summary>
            /// Holder of single access attempt for certain critical section
            /// </summary>
            public class CrAccess
            {
                public string TID;
                public int LineStart = -1, LineFinish = -1;
                public DateTime Entering = DateTime.MinValue;
                public DateTime Entered = DateTime.MinValue;
                public DateTime Leaved = DateTime.MinValue;
                public bool Closed = false;

                public bool IsEntered { get { return (this.Entered != DateTime.MinValue); } }
                public bool IsLeaved { get { return (this.Leaved != DateTime.MinValue); } }
                public bool IsLocked { get { return !this.IsEntered || !this.IsLeaved || !this.Closed; } }

                public override string ToString()
                {
                    return String.Format("CrAcc[ [{0}..{1}]; tid={2}; req={3}/{4}/{5}; {6}{7}{8} ]",
                        this.LineStart, this.LineFinish,
                        this.TID, StrUtils.CompactNskTimestampOf(this.Entering), StrUtils.CompactNskTimestampOf(this.Entered), StrUtils.CompactNskTimestampOf(this.Leaved),
                        (this.IsEntered ? "entered/":""), 
                        (this.IsLeaved ? "leaved/":""), 
                        (this.IsLocked ? "locked/":"")
                        );
                }
            }

        }

    }
}
