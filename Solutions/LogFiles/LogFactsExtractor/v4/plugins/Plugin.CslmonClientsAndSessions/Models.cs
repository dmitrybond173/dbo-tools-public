/*
 * Log Facts Extractor: visualization plug-in for CSLMON logs.
 * 
 * Data models for for CSLMON clients and sessions
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2023-12-06
 * 
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XService.Utils;

namespace Plugin.CslmonClientsAndSessions
{
    /// <summary>
    /// Root point of all data holders related for processing ClientAndSessions log facts
    /// </summary>
    public class Document
    {
        public Document() 
        {
            this.Clients = new List<Client>();
            this.ClientGroups = new List<List<Client>>();
        }

        public int ProjectId;
        public int LogFileId;
        public bool SortByInitTime = false;

        public string MinLogTs
        {
            get { return this.minLogTs; }
            set 
            {
                this.minLogTs = value;
                this.MinLog = StrUtils.NskTimestampToDateTime(value);
            }
        }
        public string MaxLogTs
        {
            get { return this.maxLogTs; }
            set
            {
                this.maxLogTs = value;
                this.MaxLog = StrUtils.NskTimestampToDateTime(value);
            }
        }
        public DateTime MinLog = DateTime.MinValue;
        public DateTime MaxLog = DateTime.MinValue;

        public List<Client> Clients { get; protected set; }
        public List<List<Client>> ClientGroups { get; protected set; }

        public void Clear()
        {
            this.ClientGroups.Clear();
            this.Clients.Clear();
        }

        /// <summary>
        /// Parse log facts data and rebuild list of: CSL clients, executor sessions, BTO server calls, transactions, etc
        /// </summary>
        public void Load(DbDataReader dr, DbConnection db)
        { 
            while (dr.Read()) 
            {
                string ts = dr["LogTime"].ToString();
                string ep = dr["ClientEP"].ToString();
                string tid = dr["TID"].ToString();
                string action = dr["action"].ToString();
                int lineNo = (int)dr["lineno"];

                //DBG: if (tid != "1384") continue;
                //DBG: if (tid != "1D90") continue;

                string ft = dr["FactType"].ToString();

                string recDump = PluginUtils.DrDump(dr);

                Client cli = null;
                if (StrUtils.IsSameText(ft, "NewClient"))
                {
                    cli = new Client();
                    cli.Index = generateClientIndex(tid);
                    cli.Connected = StrUtils.NskTimestampToDateTime(ts);
                    cli.ClientID = dr["ClientID"].ToString();
                    cli.TID = tid;
                    cli.EP = ep;
                    this.Clients.Add(cli);
                }
                else if (StrUtils.IsSameText(ft, "CloseClient"))
                {
                    cli = findLastClient(tid);
                    if (cli != null)
                    {
                        cli.Closed = StrUtils.NskTimestampToDateTime(ts);
                        cli.IoCount = (int)dr["nValue1"];
                    }
                }
                else 
                {
                    cli = findLastClient(tid);
                    if (cli != null)
                    {
                        // ----- Server I/O
                        if (StrUtils.IsSameText(ft, "SrvIO"))
                        {
                            PluginUtils.Assert(StrUtils.IsSameText(ep, cli.EP), string.Format("Client EP is not match: {0} but expected {1}", ep, cli.EP));

                            SrvIO io = new SrvIO() { Owner = cli };
                            io.Lines.Add((int)dr["lineno"]);
                            io.Started = StrUtils.NskTimestampToDateTime(ts);
                            io.SrvClass = dr["action"].ToString();
                            io.SeqNo = (int)dr["seq"];
                            cli.IO.Add(io);
                        }
                        else if (StrUtils.IsSameText(ft, "SrvIoCompleted"))
                        {
                            // it is possible SrvIoCompleted record could be after ClientClosed and new Client created...
                            if (!StrUtils.IsSameText(ep, cli.EP))
                            {
                                cli = findLastClient(tid, ep);
                            }
                            PluginUtils.Assert(StrUtils.IsSameText(ep, cli.EP), string.Format("Client EP is not match: {0} but expected {1}", ep, cli.EP));

                            // Note: when srvClass is empty then - there is no SrvIO object because that is completion for TX call
                            string srvClass = dr["action"].ToString();
                            if (srvClass.Length > 0)
                            {
                                SrvIO io = cli.LastIO;

                                int seqNo = (int)dr["seq"];
                                PluginUtils.Assert(seqNo == io.SeqNo, string.Format("CSL request seq# is not match: {0} but expected {1}", seqNo, io.SeqNo));

                                io.Lines.Add((int)dr["lineno"]);
                                io.Completed = StrUtils.NskTimestampToDateTime(ts);
                                io.SrvClass = dr["action"].ToString();
                                io.DeltaTime = (int)dr["dT"];
                            }
                        }
                        // ----- TX
                        if (StrUtils.IsSameText(ft, "TX"))
                        {
                            PluginUtils.Assert(StrUtils.IsSameText(ep, cli.EP), string.Format("Client EP is not match: {0} but expected {1}", ep, cli.EP));

                            if (StrUtils.IsSameText(action, "BGN"))
                            {
                                TxScope tx = new TxScope() { Owner = cli };
                                tx.Lines.Add(lineNo);
                                tx.ClientEP = ep;
                                tx.Started = StrUtils.NskTimestampToDateTime(ts);
                                tx.SeqStart = (int)dr["seq"];
                                cli.Transactions.Add(tx);
                            }
                            else
                            {
                                TxScope tx = cli.LastTx;
                                PluginUtils.Assert(StrUtils.IsSameText(ep, tx.ClientEP), string.Format("TX Client EP is not match: {0} but expected {1}", ep, tx.ClientEP));
                                tx.Lines.Add(lineNo);
                                tx.Completed = StrUtils.NskTimestampToDateTime(ts);
                                tx.SeqFinish = (int)dr["seq"];
                                tx.IsCommitted = StrUtils.IsSameText(action, "COM");
                            }
                        }
                        // ----- Sessions
                        else if (StrUtils.IsSameText(ft, "NewSession"))
                        {
                            PluginUtils.Assert(StrUtils.IsSameText(ep, cli.EP), string.Format("Client EP is not match: {0} but expected {1}", ep, cli.EP));

                            //DBG: if (tid == "1D90") tid += "";

                            Session sess = new Session() { Owner = cli };
                            sess.Lines.Add(lineNo);
                            sess.TryToAllocate = StrUtils.NskTimestampToDateTime(ts);
                            cli.Sessions.Add(sess);
                        }
                        else if (StrUtils.IsSameText(ft, "SessionPid"))
                        {
                            Session sess = cli.LastSession;
                            sess.Lines.Add(lineNo);
                            sess.IsNew = true;
                            sess.ExecID = dr["execId"].ToString();
                            sess.WaitingForPipe = StrUtils.NskTimestampToDateTime(ts);
                        }
                        else if (StrUtils.IsSameText(ft, "PipeConnected"))
                        {
                            Session sess = cli.LastSession;
                            sess.Lines.Add(lineNo);
                            sess.IsNew = true;
                            sess.Allocated = StrUtils.NskTimestampToDateTime(ts);
                            sess.ExecID = dr["execId"].ToString();
                        }
                        else if (StrUtils.IsSameText(ft, "SessionToClient"))
                        {
                            Session sess = cli.LastSession;
                            sess.Lines.Add(lineNo);
                            sess.ExecID = dr["execId"].ToString();
                            sess.Allocated = StrUtils.NskTimestampToDateTime(ts);
                        }
                        else if (StrUtils.IsSameText(ft, "SessionReleased"))
                        {
                            Session sess = cli.LastSession;
                            sess.Lines.Add(lineNo);
                            sess.Released = StrUtils.NskTimestampToDateTime(ts);
                        }
                    }
                }
            }

            findClientGroups();

            // loaded
            Trace.WriteLine(string.Format("{0} clients loaded", this.Clients.Count));
        }

        private void findClientGroups()
        {
            DateTime anchor = this.MinLog;
            Dictionary<double, List<Client>> clientRefs = new Dictionary<double, List<Client>>();
            foreach (Client clnt in this.Clients) 
            {
                TimeSpan v = clnt.Connected - anchor;
                List<Client> list;
                if (!clientRefs.TryGetValue(v.TotalSeconds, out list))
                {
                    list = new List<Client>();
                    clientRefs[v.TotalSeconds] = list;
                }
                list.Add(clnt);
            }

            this.ClientGroups.Clear();

            double delta = 5.0;
            double groupValue = 0;
            List<Client> currentGroup = null;
            foreach (KeyValuePair<double, List<Client>> it in clientRefs) 
            {                
                bool isNewGroup = ( (this.ClientGroups.Count == 0) || (it.Key - groupValue) > delta );
                if (isNewGroup)
                {
                    // create 1st group
                    currentGroup = new List<Client>();
                    this.ClientGroups.Add(currentGroup);
                    groupValue = it.Key;
                }
                currentGroup.AddRange(it.Value);                
            }
        }

        private int generateClientIndex(string tid)
        {
            int index = 0;
            foreach (Client cli in this.Clients)
            {
                if (StrUtils.IsSameText(cli.TID, tid))
                    index++;
            }
            return index;
        }

        private Client findLastClient(string tid, string ep=null)
        {
            Client result = null;
            for (int i = this.Clients.Count - 1; i >= 0; i--)
            {
                Client cli = this.Clients[i];
                if (StrUtils.IsSameText(cli.TID, tid))
                {
                    bool isMatch = (ep == null || (ep != null && StrUtils.IsSameText(cli.EP, ep)));
                    if (isMatch)
                    {
                        result = cli;
                        break;
                    }
                }                
            }
            return result;
        }

        private string minLogTs = null;
        private string maxLogTs = null;
    }


    /// <summary>
    /// Holder of CSL client properties rebuilt from log facts
    /// </summary>
    public class Client
    { 
        public Client() 
        {
            this.Connected = DateTime.MinValue;
            this.Closed = DateTime.MinValue;
            this.Index = 0;

            this.IO = new List<SrvIO>();
            this.Sessions = new List<Session>();
            this.Transactions = new List<TxScope>();
        }

        public override string ToString()
        {
            return String.Format("Cli[{0}/{1}; #{2}; {3}..{4}/dT={5}sec; {6}]",
                this.TID, this.EP, this.ClientID,
                StrUtils.NskTimestampOf(this.Connected).Substring(0, 19),
                StrUtils.NskTimestampOf(this.Closed).Substring(0, 19),
                (this.Closed - this.Connected).TotalSeconds.ToString("N1"),
                this.IoCount
                );
        }

        public List<int> Lines = new List<int>();

        public int Index { get; set; }
        public string ID { get { return string.Format("{0}-{1}", this.TID, this.Index); } }
        public DateTime Connected { get; set; } = DateTime.MinValue;
        public DateTime Closed { get; set; } = DateTime.MinValue;
        public bool IsClosed { get { return (this.Closed != DateTime.MinValue); } }
        public string TID { get; set; }
        public string ClientID { get; set; }
        public string EP { get; set; }
        public int IoCount { get; set; }

        public List<SrvIO> IO { get; protected set; }
        public List<Session> Sessions { get; protected set; }
        public SrvIO LastIO { get { return this.IO[this.IO.Count - 1]; } }
        public Session LastSession { get { return this.Sessions[this.Sessions.Count - 1]; } }

        public List<TxScope> Transactions {  get; protected set; }
        public TxScope LastTx { get { return this.Transactions[this.Transactions.Count - 1]; } }

        public string FirstSessionInitTs
        {
            get             
            {
                string result = "2900-01-01:00:00:00.000000";
                foreach (Session s in this.Sessions)
                {
                    if (s.IsNew)
                    {
                        result = StrUtils.NskTimestampOf(s.WaitingForPipe);
                        break;
                    }
                }
                return result;
            }
        }
    }


    /// <summary>
    /// Holder of CSLMON executor session properties rebuilt from log facts
    /// </summary>
    public class Session
    {
        public Session()
        {
        }

        public override string ToString()
        {
            return String.Format("Sess[{0}; {1}{2} -> {3} -> {4}; {5}]",
                this.ExecID,
                (this.IsNew ? "new; " : ""),
                StrUtils.NskTimestampOf(this.TryToAllocate).Substring(0, 19),
                StrUtils.NskTimestampOf(this.Allocated).Substring(0, 19),
                StrUtils.NskTimestampOf(this.Released).Substring(0, 19),
                this.Attempts
                );
        }

        public Client Owner;
        public List<int> Lines = new List<int>();

        public bool IsNew { get; set; } = false;
        public string ExecID { get; set; } = "-";
        public DateTime TryToAllocate { get; set; } = DateTime.MinValue;
        public DateTime WaitingForPipe { get; set; } = DateTime.MinValue; // start waiting for pipe
        public DateTime Allocated { get; set; } = DateTime.MinValue;
        public DateTime Released { get; set; } = DateTime.MinValue;
        public int Attempts { get; set; } = 0;
    }


    /// <summary>
    /// Holder of BTO call properties rebuilt from log facts
    /// </summary>
    public class SrvIO
    {
        public SrvIO()
        { 
        }

        public override string ToString()
        {
            return String.Format("SrvIo[#{0}; {1}; {2} .. {3}; dT={4}]",
                this.SeqNo, this.SrvClass,
                StrUtils.NskTimestampOf(this.Started).Substring(0, 19),
                StrUtils.NskTimestampOf(this.Completed).Substring(0, 19),                
                this.DeltaTime
                );
        }

        public Client Owner;
        public List<int> Lines = new List<int>();

        public DateTime Started { get; set; } = DateTime.MinValue;
        public DateTime Completed { get; set; } = DateTime.MinValue;
        public string SrvClass { get; set; } = null;
        public int SeqNo { get; set; } = -1;
        public int DeltaTime { get; set; } = 0;
    }


    /// <summary>
    /// Holder of BTO transaction properties rebuilt from log facts
    /// </summary>
    public class TxScope
    {
        public TxScope()
        {
        }

        public override string ToString()
        {
            return String.Format("TxScope[#{0}; {1}..{2}; {3} .. {4}]",
                (this.IsOpened ? "in-progress" : (this.IsCommitted ? "commited" : "aborted")),
                this.SeqStart, this.SeqFinish,
                StrUtils.NskTimestampOf(this.Started).Substring(0, 19),
                StrUtils.NskTimestampOf(this.Completed).Substring(0, 19)                
                );
        }

        public Client Owner;
        public List<int> Lines = new List<int>();

        public bool IsOpened 
        {
            get { return (this.Started != DateTime.MinValue && this.Completed == DateTime.MinValue); }
        }

        public string ClientEP = null;
        public DateTime Started { get; set; } = DateTime.MinValue;
        public DateTime Completed { get; set; } = DateTime.MinValue;
        public bool IsCommitted { get; set; } = false;
        public int SeqStart { get; set; } = -1;
        public int SeqFinish { get; set; } = -1;
    }


}
