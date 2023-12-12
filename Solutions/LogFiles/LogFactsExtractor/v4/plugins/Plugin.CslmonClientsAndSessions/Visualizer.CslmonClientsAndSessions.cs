/*
 * Log Facts Extractor: visualization plug-in for CSLMON logs.
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2023-12-06
 * 
 * See also:
 *   https://stackoverflow.com/questions/38597121/how-do-i-rotate-a-label-in-vb-net
 *   TextAttributes = https://jenkov.com/tutorials/svg/text-element.html
*/

using PAL;
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
using System.Xml;
//using XService.Components;
using XService.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace Plugin.CslmonClientsAndSessions
{
    public class Visualizer
    {
        public Visualizer(LogFactsExtractorPlugin pOwner)
        {
            this.Owner = pOwner;
            this.Doc = pOwner.Doc;

            object obj;
            if (this.Owner.GetVisualParams().TryGetValue("minTs", out obj)) this.From = (DateTime)obj;
            if (this.Owner.GetVisualParams().TryGetValue("maxTs", out obj)) this.To = (DateTime)obj;

            if (this.Owner.GetVisualParams().TryGetValue("SvgTemplates", out obj))
            {
                this.Templates = (XmlDocument)obj;

                this.Generator = new ScriptGenerator(this.Templates.DocumentElement);
            }

            this.Metrics = new DrawingMetrics(this.Doc);
        }

        public LogFactsExtractorPlugin Owner { get; private set; }

        public XmlDocument Templates { get; protected set; }
        public ScriptGenerator Generator { get; protected set; }

        public DateTime From = DateTime.MinValue;
        public DateTime To = DateTime.MinValue;
        public Document Doc { get; protected set; }
        public DrawingMetrics Metrics { get; protected set; }
        public string OutputFilename { get; protected set; }

        public void Clear()
        {
            this.OutputFilename = null;
        }

        public void GenerateScheme()
        {
            object obj; 
            string visType = null;
            if (this.Owner.GetVisualParams().TryGetValue("VisualizationType", out obj))
                visType = obj.ToString();

            if (string.IsNullOrEmpty(visType) || StrUtils.IsSameText(visType, "svg"))
                generateSvg();
            else if (StrUtils.IsSameText(visType, "bitmap"))
                generateBitmap();

            if (this.OutputFilename != null && File.Exists(this.OutputFilename))
            { 
                PluginUtils.OpenFile(this.OutputFilename);
            }
        }

        private void dumpClients() 
        {
            Trace.WriteLine(string.Format(" * {0} clients in list:", this.Doc.Clients.Count));
            int iClient = 0;
            foreach (Client clnt in this.Doc.Clients)
            {
                iClient++;
                Trace.WriteLine(string.Format("  + #{0}: {1}", iClient, clnt));
                int iSess = 0; 
                foreach (Session sess in clnt.Sessions)
                {
                    iSess++;
                    Trace.WriteLine(string.Format("     + #{0}: {1}", iSess, sess));
                }
            }
        }

        private void generateSvg()
        {
            Trace.WriteLine(string.Format("--- GenerateSvg()"));

            // [005. Приручаем SVG – Лев Солнцев] https://www.youtube.com/watch?time_continue=4&v=2DRu77MC6Ns

            //MessageBox.Show("CslmonCallsVisualization:generateSvg - not implemented!",
            //    "Information", 
            //    MessageBoxButtons.OK, MessageBoxIcon.Stop);

            dumpClients();

            Trace.WriteLine(string.Format(" * Metrics: ts[{0} .. {1}], scale[{2} minutes, {3} seconds]",
                StrUtils.NskTimestampOf(this.Doc.MinLog).Substring(0, 19),
                StrUtils.NskTimestampOf(this.Doc.MaxLog).Substring(0, 19),
                this.Metrics.ScaleLength.TotalMinutes.ToString("N1"), this.Metrics.ScaleLength.TotalSeconds
                ));
            Trace.WriteLine(string.Format(" * Metrics: dotsPerSec={0}, drawingWxH={1} x {2}",
                this.Metrics.DotsPerSecond, this.Metrics.DrawingDim.Width, this.Metrics.DrawingDim.Height ));

            this.Generator.Start("SVG");
            this.Generator.SetVariable("LogFilename", this.Owner.LogFilename);
            this.Generator.SetVariable("drawingWidth", this.Metrics.DrawingDim.Width);
            this.Generator.SetVariable("drawingHeight", this.Metrics.DrawingDim.Height);
            Trace.WriteLine(string.Format(" + adding SVG header..."));
            this.Generator.AddHeader();
            
            ScriptGenerator scp = new ScriptGenerator(this.Generator);
            scp.Start("TimeScale");
            scp.AddHeader();

            // ----- add time scale
            Trace.WriteLine(string.Format(" + adding TimeScale..."));
            TimeSpan tStep = new TimeSpan(0, 1, 0);
            DateTime tScale = this.Metrics.ScaleStart;
            for (int iMin = 0; iMin < this.Metrics.ScaleLength.TotalMinutes; iMin++)
            {
                // https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
                // https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings

                float x = this.Metrics.SecondsToX(iMin * 60);
                scp.SetVariable("time", tScale.ToString("HH:mm"));
                scp.SetVariable("x", x);
                scp.SetVariable("y", this.Metrics.Offset.Y + 2);
                scp.SetVariable("yLabel", this.Metrics.Offset.Y);

                scp.AddItem(null, null);

                tScale += tStep;
            }
            scp.AddFooter();
            string scaleCode = scp.Finish();
            this.Generator.AddItem(scaleCode, null);

            int clientIdx, clientLineY;

            // ----- add client life-time lines
            Trace.WriteLine(string.Format(" + adding clients ({0} items)...", this.Doc.Clients.Count));
            scp.Start("Clients");
            scp.AddHeader();
            clientLineY = 60;
            clientIdx = 0;
            foreach (Client clnt in this.Doc.Clients)
            {
                clientIdx++;

                TimeSpan ds = (clnt.Connected - this.Metrics.ScaleStart);
                TimeSpan df = (clnt.Closed != DateTime.MinValue ? clnt.Closed - this.Metrics.ScaleStart : TimeSpan.MinValue);
                double xStart = ds.TotalSeconds;
                double xFinish = (df != TimeSpan.MinValue ? df.TotalSeconds : this.Metrics.ScaleLength.TotalSeconds);
                float x1 = this.Metrics.SecondsToX((float)xStart);
                float x2 = this.Metrics.SecondsToX((float)xFinish);

                scp.SetVariable("clientTimeStartX", DrawingMetrics.FmtFloat(x1));
                scp.SetVariable("clientTimeFinishX", DrawingMetrics.FmtFloat(x2));
                scp.SetVariable("clientTimeY", clientLineY);
                scp.SetVariable("clientTimeYlab", clientLineY); // - 5);
                scp.SetVariable("ClientEP", clnt.EP);
                scp.SetVariable("ClientLabel", string.Format("{0} ({1})", clnt.TID, clnt.Sessions.Count));

                scp.AddItem(null, null);

                clientLineY += this.Metrics.ClientLineHeight;
            }
            scp.AddFooter();
            string clientsCode = scp.Finish();
            this.Generator.AddItem(clientsCode, null);

            // ----- add session refs
            Trace.WriteLine(string.Format(" + adding client session refs..."));
            scp.Start("SessionRefs");
            scp.AddHeader();
            clientLineY = 60;
            clientIdx = 0;
            // https://jenkov.com/tutorials/svg/text-element.html
            foreach (Client clnt in this.Doc.Clients)
            {
                clientIdx++;

                Trace.WriteLine(string.Format("   + cli#{0}: {1} session refs...", clnt.ID, clnt.Sessions.Count));

                foreach (Session sess in clnt.Sessions)
                {
                    TimeSpan ds = (sess.TryToAllocate - this.Metrics.ScaleStart);
                    TimeSpan df = (sess.Released != DateTime.MinValue ? sess.Released - this.Metrics.ScaleStart : TimeSpan.MinValue);
                    double xStart = ds.TotalSeconds;
                    double xFinish = (df != TimeSpan.MinValue ? df.TotalSeconds : this.Metrics.ScaleLength.TotalSeconds);
                    float x1 = this.Metrics.SecondsToX((float)xStart);
                    float x2 = this.Metrics.SecondsToX((float)xFinish);

                    scp.SetVariable("sessionTimeStartX", DrawingMetrics.FmtFloat(x1));
                    scp.SetVariable("sessionTimeFinishX", DrawingMetrics.FmtFloat(x2));
                    scp.SetVariable("sessionTimeY", clientLineY);
                    scp.SetVariable("sessionTimeYlab", clientLineY - 6);
                    scp.SetVariable("SessionLabel", sess.ExecID);

                    scp.AddItem(null, null);
                }

                clientLineY += this.Metrics.ClientLineHeight;
            }
            scp.AddFooter();
            string sessionRefsCode = scp.Finish();
            this.Generator.AddItem(sessionRefsCode, null);

            // ----- add session init waits
            Trace.WriteLine(string.Format(" + adding client session waits..."));
            scp.Start("SessionInitWaits");
            scp.AddHeader();
            clientLineY = 60;
            clientIdx = 0;
            // https://jenkov.com/tutorials/svg/text-element.html
            foreach (Client clnt in this.Doc.Clients)
            {
                clientIdx++;

                Trace.WriteLine(string.Format("   + cli#{0}: {1} session refs...", clnt.ID, clnt.Sessions.Count));
                foreach (Session sess in clnt.Sessions)
                {
                    if (!sess.IsNew) continue;

                    TimeSpan ds = (sess.TryToAllocate - this.Metrics.ScaleStart);
                    TimeSpan df = (sess.WaitingForPipe - this.Metrics.ScaleStart);
                    double xStart = ds.TotalSeconds;
                    double xFinish = df.TotalSeconds;
                    float x1 = this.Metrics.SecondsToX((float)xStart);
                    float x2 = this.Metrics.SecondsToX((float)xFinish);

                    scp.SetVariable("sessionTimeStartX", DrawingMetrics.FmtFloat(x1));
                    scp.SetVariable("sessionTimeFinishX", DrawingMetrics.FmtFloat(x2));
                    scp.SetVariable("sessionTimeY", clientLineY);

                    scp.AddItem(null, null);
                }

                clientLineY += this.Metrics.ClientLineHeight;
            }
            scp.AddFooter();
            string sessionInitWaitsCode = scp.Finish();
            this.Generator.AddItem(sessionInitWaitsCode, null);

            // ----- add session inits
            Trace.WriteLine(string.Format(" + adding client session initializations..."));
            scp.Start("SessionInitializations");
            scp.AddHeader();
            clientLineY = 60;
            clientIdx = 0;
            // https://jenkov.com/tutorials/svg/text-element.html
            foreach (Client clnt in this.Doc.Clients)
            {
                clientIdx++;

                foreach (Session sess in clnt.Sessions)
                {
                    if (!sess.IsNew) continue;

                    TimeSpan ds = (sess.WaitingForPipe - this.Metrics.ScaleStart);
                    TimeSpan df = (sess.Allocated - this.Metrics.ScaleStart);
                    double xStart = ds.TotalSeconds;
                    double xFinish = df.TotalSeconds;
                    float x1 = this.Metrics.SecondsToX((float)xStart);
                    float x2 = this.Metrics.SecondsToX((float)xFinish);

                    scp.SetVariable("sessionTimeStartX", DrawingMetrics.FmtFloat(x1));
                    scp.SetVariable("sessionTimeFinishX", DrawingMetrics.FmtFloat(x2));
                    scp.SetVariable("sessionTimeY", clientLineY);

                    scp.AddItem(null, null);
                }

                clientLineY += this.Metrics.ClientLineHeight;
            }
            scp.AddFooter();
            string sessionInitCode = scp.Finish();
            this.Generator.AddItem(sessionInitCode, null);

            // ----- close SVG
            Trace.WriteLine(string.Format(" + adding SVG footer..."));
            this.Generator.AddFooter();
            Trace.WriteLine(string.Format(" + generating SVG..."));
            string script = this.Generator.Finish();

            string fn = this.Owner.LogFilename;
            fn = Path.ChangeExtension(fn, string.Format(".{0}.svg", this.Owner.ProjectId));
            this.OutputFilename = fn;
            Trace.WriteLine(string.Format(" = saving to: {0}", fn));
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.WriteLine(script);
            }
        }

        private void generateBitmap()
        {
            this.Clear();

            MessageBox.Show("CslmonCallsVisualization:generateBitmap - not implemented!",
                "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        public void GetMetrics(out DateTime pMinTs, out DateTime pMaxTs, out int pTimeDepth)
        {
            pMinTs = DateTime.MaxValue;
            pMaxTs = DateTime.MinValue;
            if (pMinTs == DateTime.MaxValue)
                pMinTs = DateTime.MinValue;

            pTimeDepth = (int)(pMaxTs - pMinTs).TotalSeconds;
        }

        private bool isHighlightedSrvCls(string pSrvName)
        {
            return false; // this.Owner.SelectedServerClasses.Contains(pSrvName.ToLower());
        }


        public class DrawingMetrics
        {
            public DrawingMetrics(Document pDoc)
            {
                this.Doc = pDoc;
                init();
            }

            public Document Doc { get; protected set; }
            public DateTime ScaleStart;
            public TimeSpan ScaleLength;
            public Point Offset = new Point(50, 30);
            public int ClientLineHeight = 35;
            public Size ScaleSize = new Size(300, 12);
            public int DotsPerSecond;
            public Size DrawingDim = Size.Empty;

            public float SecondsToX(float value)
            {
                float result = value * this.DotsPerSecond + this.Offset.X;
                return result;
                //float y = (float)xFinish * this.Metrics.DotsPerSecond + this.Metrics.Offset.X;
            }

            private void init()
            {
                DateTime dt = this.Doc.MinLog;
                this.ScaleStart = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
                TimeSpan scaleCorrection = (dt - this.ScaleStart);
                this.ScaleLength = (this.Doc.MaxLog - this.Doc.MinLog) + scaleCorrection;
                //this.Offset = new Point(50, 30);
                //Size scaleSz = new Size(300, 12);
                //int clientLineHeight = 22;
                this.DotsPerSecond = this.ScaleSize.Width / 60;
                int drwW = (int)(this.ScaleLength.TotalMinutes * 60.0 * this.DotsPerSecond) + this.Offset.X + 200;
                int drwH = (int)(this.ClientLineHeight * this.Doc.Clients.Count) + this.Offset.Y + 200;
                this.DrawingDim = new Size(drwW, drwH);
            }

            public static string FmtFloat(float x)
            {
                string s = x.ToString();
                s = s.Replace(",", ".");
                return s;
            }

        }
    }
}
