using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace XService.Utils
{
    public class SyncUtils
    {
        public static void Initialize()
        {
            lock (typeof(SyncUtils))
            {
                if (_instance == null)
                    _instance = new SyncUtils();

                _instance.Reset();
            }
        }

        private static SyncUtils _instance = null;
        public static SyncUtils Instance
        {
            get
            {
                lock (typeof(SyncUtils))
                {
                    if (_instance == null)
                        _instance = new SyncUtils();
                    return _instance;
                }
            }
        }

        protected SyncUtils()
        {
            int n;
            string s = ConfigurationManager.AppSettings["SyncUtils.DEBUG_MODE"];
            if (!string.IsNullOrEmpty(s))
                this.debugMode = StrUtils.GetAsBool(s);

            if (this.debugMode)
            {
                s = ConfigurationManager.AppSettings["SyncUtils.MAX_HISTORY_ITEMS"];
                if (!string.IsNullOrEmpty(s) && StrUtils.GetAsInt(s, out n))
                    this.MAX_HISTORY_ITEMS = n;

                s = ConfigurationManager.AppSettings["SyncUtils.MAX_LOCK_AGE"];
                if (!string.IsNullOrEmpty(s) && StrUtils.GetAsInt(s, out n))
                    this.MAX_LOCK_AGE = n;
            }

            this.locksHistory = new List<LockInfo>(MAX_HISTORY_ITEMS);
        }

        public override string ToString()
        {
            lock (this.syncRoot)
            {
                return String.Format("SyncUtils[{0} active wait-locks, {1} entered locks, {2} locks in history]",
                    this.locks.Count, this.enteredLocks.Count, this.locksHistory.Count);
            }
        }

        /// <summary>Reset all locts and counters</summary>
        public void Reset()
        {
            lock (this.syncRoot)
            {
                this.locks.Clear();
                this.enteredLocks.Clear();
                this.locksHistory.Clear();
                this.dlCounter = 0;
            }
        }

        /// <summary>Return diagnostic data</summary>
        public void GetDiagnosticData(out string pLocks, out string pEnteredLocks, out string pHistoryLocks, out int pMaxAge, out int pMaxUsage)
        {
            lock (this.syncRoot)
            {
                pMaxAge = GetMaxLockAge();
                pMaxUsage = GetMaxLockUsage();

                pLocks = AsDump;
                pEnteredLocks = EnteredLocks;
                pHistoryLocks = HistoryDump;
            }
        }

        /// <summary>Log and register lock-wait</summary>
        /// <param name="pMsg">Lock identification message (for log)</param>
        /// <param name="pLockObject">Sync-point object for a lock</param>
        /// <returns>LockInfo object describing a lock</returns>
        public LockInfo EnteringLock(string pMsg, object pLockObject)
        {
            bool hasDeadlocks = false;
            string list1 = "", list2 = "", list3 = "";
            LockInfo info = null;
            lock (this.syncRoot)
            {
                info = new LockInfo() { Thrd = Thread.CurrentThread, WaitStarted = DateTime.Now, Message = pMsg, LockObject = pLockObject };
                locks.Add(info);

                hasDeadlocks = (this.debugMode && MAX_LOCK_AGE > 0 && GetMaxLockAge() >= MAX_LOCK_AGE);
                if (hasDeadlocks)
                {
                    // write disagnostic output on each 100th deadlock-detection
                    bool isFit = ((dlCounter % 100) == 0);
                    dlCounter++;
                    if (isFit)
                    {
                        list1 = AsDump;
                        list2 = EnteredLocks;
                        list3 = HistoryDump;
                    }
                }
            }
            Trace.WriteLine(string.Format("[sync] Entering {0}...", pMsg));

            if (hasDeadlocks && !string.IsNullOrEmpty(list1))
            {
                Trace.WriteLine(string.Format("[sync] Dead-Locks detected!"));
                Trace.WriteLine(string.Format("[sync]  1.LocksList: {0}", list1));
                Trace.WriteLine(string.Format("[sync]  2.EnteredLocks: {0}", list2));
                Trace.WriteLine(string.Format("[sync]  3.LocksHistory: {0}", list3));
            }

            return info;
        }

        /// <summary>Log and remove lock-wait registration. Optionally, register entered lock (if LockInfo.NeedToRegister is true)</summary>
        /// <param name="pInfo">LockInfo object describing a lock</param>
        public void LockEntered(LockInfo pInfo)
        {
            Trace.WriteLine(string.Format("[sync] = Entered {0}. LockWait.dT={1} ms", pInfo.Message, (DateTime.Now - pInfo.WaitStarted).TotalMilliseconds.ToString("N2")));
            lock (this.syncRoot)
            {
                pInfo.Status = LockInfo.EStatus.Entered;
                pInfo.LockEntered = DateTime.Now;

                if (this.debugMode)
                {
                    this.locksHistory.Add(pInfo);
                    while (this.locksHistory.Count > MAX_HISTORY_ITEMS)
                        this.locksHistory.RemoveAt(0);
                }

                if (pInfo.NeedToRegister)
                {
                    this.enteredLocks.Add(pInfo);
                }

                this.locks.Remove(pInfo);
            }
        }

        /// <summary>Mark lock as exited, remove its registration (if exists)</summary>
        /// <param name="pInfo">LockInfo object describing a lock</param>
        public void LeavingLock(LockInfo pInfo)
        {
            Trace.WriteLine(string.Format("[sync] = Leaving {0}. LockUsage.dT={1} ms", pInfo.Message, (DateTime.Now - pInfo.WaitStarted).TotalMilliseconds.ToString("N2")));
            lock (this.syncRoot)
            {
                pInfo.Status = LockInfo.EStatus.Leave;
                this.enteredLocks.Remove(pInfo);
            }
        }

        /// <summary>Search for max age of wait-locks</summary>
        /// <returns>Max age (in seconds) of wait-lock (or 0 when no wait-locks registered)</returns>
        public int GetMaxLockAge()
        {
            List<LockInfo> pList = this.locks;
            lock (this.syncRoot)
            {
                double maxAge = 0;
                if (pList.Count > 0)
                {
                    maxAge = Double.MinValue;
                    foreach (LockInfo info in pList)
                    {
                        double a = info.WaitAge;
                        if (maxAge < a) maxAge = a;
                    }
                }
                return (int)maxAge;
            }
        }

        /// <summary>Search for max lock usage age of entered-locks</summary>
        /// <returns>Max usage age (in seconds) of entered-lock (or 0 when no entered-locks registered)</returns>
        public int GetMaxLockUsage()
        {
            List<LockInfo> pList = this.enteredLocks;
            lock (this.syncRoot)
            {
                double maxAge = 0;
                if (pList.Count > 0)
                {
                    maxAge = Double.MinValue;
                    foreach (LockInfo info in pList)
                    {
                        double a = info.UsageAge;
                        if (maxAge < a) maxAge = a;
                    }
                }
                return (int)maxAge;
            }
        }
        

        /// <summary>Write to trace Active wait-locks and active entered-locks</summary>
        public void WriteToTrace()
        {
            lock (this.syncRoot)
            {
                Trace.WriteLine(" A.LocksList: " + this.AsDump);
                Trace.WriteLine(" B.EnteredLocks: " + this.EnteredLocks);
            }
        }
        
        internal string ListToDump(List<LockInfo> pList)
        {
            lock (this.syncRoot)
            {
                double maxWaitAge = 0, minWaitAge = 0;
                double maxUseAge = 0, minUseAge = 0;
                if (pList.Count > 0)
                {
                    maxWaitAge = Double.MinValue;
                    minWaitAge = Double.MaxValue;
                    foreach (LockInfo info in pList)
                    {
                        double a = info.WaitAge;
                        if (minWaitAge > a) minWaitAge = a;
                        if (maxWaitAge < a) maxWaitAge = a;
                        double b = info.UsageAge;
                        if (minUseAge > a) minUseAge = a;
                        if (minUseAge < a) minUseAge = a;
                    }
                }

                // Note: null cannot be a key in Dictionary! 
                // See here - https://stackoverflow.com/questions/2174692/why-cant-you-use-null-as-a-key-for-a-dictionarybool-string/2174949#2174949
                Dictionary<object, int> objRefs = new Dictionary<object, int>();
                foreach (LockInfo info in pList)
                {
                    int n;
                    object obj = info.LockObject;
                    if (obj == null) obj = DBNull.Value;
                    if (objRefs.TryGetValue(info.LockObject, out n))
                        n++;
                    else
                        n = 1; 
                    objRefs[info.LockObject] = n;
                }

                StringBuilder sb = new StringBuilder(pList.Count * 140);
                sb.AppendFormat("{0} items(wait=[{1} .. {2}s], use=[{3} .. {4}s])=[ ", pList.Count, 
                    minWaitAge.ToString("N2"), maxWaitAge.ToString("N2"), minUseAge.ToString("N2"), maxUseAge.ToString("N2"));
                foreach (LockInfo info in pList)
                {
                    int refCnt;
                    if (!objRefs.TryGetValue(info.LockObject, out refCnt))
                        refCnt = 1;
                    sb.AppendFormat("({0}T{1}/wait={2}s/{3}msg={4}/{5}:{6}), ",
                        info.StatusMarker,
                        info.Thrd.ManagedThreadId,
                        info.WaitAge.ToString("N2"),
                        (info.Status >= LockInfo.EStatus.Entered ? string.Format("use={0}s/", info.UsageAge.ToString("N2")) : ""),
                        info.Message,
                        refCnt, string.Format("{0}@{1}", info.LockObject.GetType().Name, info.LockObject.GetHashCode())
                        );
                }
                sb.AppendFormat("]");
                objRefs.Clear();
                return sb.ToString();
            }
        }

        /// <summary>Dump of active wait-locks</summary>
        public string AsDump
        {
            get { return ListToDump(this.locks); }
        }

        /// <summary>Dump of entered wait-locks</summary>
        public string EnteredLocks
        {
            get { return ListToDump(this.enteredLocks); }
        }

        /// <summary>Dump of last N wait-locks which were successfully entered</summary>
        public string HistoryDump
        {
            get { return ListToDump(this.locksHistory); }
        }

        /// <summary>Enable/disable debug-mode (when it will register wait-locks and so on)</summary>
        public bool DebugMode
        {
            get { return this.debugMode; }
            set { this.debugMode = value; }
        }

        #region Static Interface

        /// <summary>Log and register lock-wait</summary>
        /// <param name="pMsg">Lock identification message (for log)</param>
        /// <param name="pLockObject">Sync-point object for a lock</param>
        /// <returns>LockInfo object describing a lock</returns>
        public static LockInfo S_EnteringLock(string pMsg, object pLockObject)
        {
            return Instance.EnteringLock(pMsg, pLockObject);
        }

        /// <summary>Log and remove lock-wait registration. Optionally, register entered lock (if LockInfo.NeedToRegister is true)</summary>
        /// <param name="pInfo">LockInfo object describing a lock</param>
        public static void S_LockEntered(LockInfo pInfo)
        {
            Instance.LockEntered(pInfo);
        }

        /// <summary>Mark lock as exited, remove its registration (if exists)</summary>
        /// <param name="pInfo">LockInfo object describing a lock</param>
        public static void S_LeavingLock(LockInfo pInfo)
        {
            Instance.LeavingLock(pInfo);
        }

        /// <summary>Write to trace Active wait-locks and active entered-locks</summary>
        public static void S_WriteToTrace()
        {
            Instance.WriteToTrace();
        }

        /// <summary>Dump of active wait-locks</summary>
        public static string S_AsDump
        {
            get { return Instance.AsDump; }
        }

        /// <summary>Dump of last N wait-locks which were successfully entered</summary>
        public string S_HistoryDump
        {
            get { return Instance.HistoryDump; }
        }

        /// <summary>Dump of entered wait-locks</summary>
        public string S_EnteredLocks
        {
            get { return Instance.EnteredLocks; }
        }

        /// <summary>Enable/disable debug-mode (when it will register wait-locks and so on)</summary>
        public static bool S_DebugMode
        {
            get { return Instance.debugMode; }
            set { Instance.debugMode = value; }
        }

        /// <summary>Return diagnostic data</summary>
        public static void S_GetDiagnosticData(out string pLocks, out string pEnteredLocks, out string pHistoryLocks, out int pMaxAge, out int pMaxUsage)
        {
            Instance.GetDiagnosticData(out pLocks, out pEnteredLocks, out pHistoryLocks, out pMaxAge, out pMaxUsage);
        }

        #endregion

        /// <summary>Holder or wait-lock</summary>
        public class LockInfo
        {
            /// <summary>Status of lock-info object</summary>
            public enum EStatus
            {
                Wait,
                Entered,
                Leave
            }

            /// <summary>Ref to Thread which requested/entered a lock</summary>
            public Thread Thrd;
            
            /// <summary>Sync-point object for a lock</summary>
            public object LockObject;
            
            /// <summary>Lock identification message (for log)</summary>
            public string Message;
            
            /// <summary>When waiting for a lock started</summary>
            public DateTime WaitStarted = DateTime.MinValue;

            /// <summary>When lock entered</summary>
            public DateTime LockEntered = DateTime.MinValue;

            /// <summary>When lock leave</summary>
            public DateTime LockLeave = DateTime.MinValue;

            /// <summary>If need to register entered lock in internal list</summary>
            public bool NeedToRegister = false;
            
            /// <summary>Lock status</summary>
            public EStatus Status = EStatus.Wait;

            /// <summary>Age (in seconds) of lock-wait</summary>
            public double WaitAge
            {
                get
                {
                    if (this.Status == EStatus.Wait)
                        return (DateTime.Now - this.WaitStarted).TotalSeconds;
                    else
                        return (this.LockEntered - this.WaitStarted).TotalSeconds;
                }
            }

            /// <summary>Age (in seconds) of lock-usage</summary>
            public double UsageAge
            {
                get
                {
                    if (this.Status == EStatus.Wait)
                        return 0;
                    else if (this.Status == EStatus.Entered)
                        return (DateTime.Now - this.LockEntered).TotalSeconds;
                    else
                        return (this.LockLeave - this.LockEntered).TotalSeconds;
                }
            }

            /// <summary>String prefix indicating lock status</summary>
            public string StatusMarker
            {
                get
                {
                    switch (this.Status)
                    {
                        case EStatus.Entered: return ">>";
                        case EStatus.Leave: return "<<";
                        default: return "";
                    }
                }
            }

            /// <summary>Returns debug dump for this lock</summary>
            public string AsDump
            {
                get
                {
                    return string.Format("({0}T{1}/age={2}sec/{3}msg={4}/obj={5}), ",
                        this.StatusMarker,
                        this.Thrd.ManagedThreadId,
                        this.WaitAge.ToString("N2"),
                        (this.Status >= EStatus.Entered ? string.Format("use={0}ms/", this.UsageAge.ToString("N2")) : ""),
                        this.Message,
                        CommonUtils.ObjectToStr(this.LockObject)
                        //string.Format("{0}@{1}", this.LockObject.GetType().Name, this.LockObject.GetHashCode())
                        );
                }
            }

            public override string ToString()
            {
                return this.AsDump;
            }

        }

        /// <summary>Holder or auto-processed wait-lock</summary>
        public class LockedScope : IDisposable
        {
            /// <summary>Object used as sync-point</summary>
            public object SyncObject { get; protected set; }
            
            /// <summary>If need to enter lock</summary>
            public bool NeedToEnterLock { get; protected set; }

            /// <summary>Flag if lock was entered</summary>
            public bool Entered { get; protected set; }

            /// <summary>LockInfo object</summary>
            public LockInfo Lock { get; protected set; }

            /// <summary>
            /// Enter locked scope using specified object as sync-point.
            /// When TraceMode is ON then also write diagnostic info to trace log and register lock-info.
            /// </summary>
            /// <param name="pSyncRoot">Sync-point object</param>
            /// <param name="pTraceMode">TraceMode, if need to write diagnostic info to trace log and register lock-info</param>
            /// <param name="pMsg">Diagnostic message to write to trace log</param>
            public LockedScope(object pSyncRoot, bool pTraceMode, string pMsg)
            {
                this.SyncObject = pSyncRoot;
                this.NeedToEnterLock = true;

                this.Lock = pTraceMode ? SyncUtils.S_EnteringLock(pMsg, this.SyncObject) : null;

                if (this.NeedToEnterLock)
                {
                    Monitor.Enter(this.SyncObject);
                }
                this.Entered = true;

                if (this.Lock != null)
                {
                    this.Lock.NeedToRegister = true; // to register in list of entered locks
                    SyncUtils.S_LockEntered(this.Lock);
                }
            }

            /// <summary>
            /// Enter locked scope using specified object as sync-point.
            /// When TraceMode is ON then also write diagnostic info to trace log and register lock-info.
            /// </summary>
            /// <param name="pSyncRoot">Sync-point object</param>
            /// <param name="pEnterLock">If need to call Monitor.Enter() and then Monitor.Exit()</param>
            /// <param name="pTraceMode">TraceMode, if need to write diagnostic info to trace log and register lock-info</param>
            /// <param name="pMsg">Diagnostic message to write to trace log</param>
            public LockedScope(object pSyncRoot, bool pEnterLock, bool pTraceMode, string pMsg)
            {
                this.SyncObject = pSyncRoot;
                this.NeedToEnterLock = pEnterLock;

                this.Lock = pTraceMode ? SyncUtils.S_EnteringLock(pMsg, this.SyncObject) : null;

                if (this.NeedToEnterLock)
                {
                    Monitor.Enter(this.SyncObject);
                }
                this.Entered = true;

                if (this.Lock != null)
                {
                    this.Lock.NeedToRegister = true; // to register in list of entered locks
                    SyncUtils.S_LockEntered(this.Lock);
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool pDisposing)
            {
                if (this.Entered)
                {
                    this.Entered = false;
                    Monitor.Exit(this.SyncObject);
                }
                if (this.Lock != null)
                {
                    SyncUtils.S_LeavingLock(this.Lock);
                    this.Lock = null;
                }
            }
        }

        #region Implementation details 

        private object syncRoot = new object();
        private List<LockInfo> locks = new List<LockInfo>();
        private List<LockInfo> enteredLocks = new List<LockInfo>();
        private List<LockInfo> locksHistory;
        private long dlCounter = 0;

        private bool debugMode = false;
        private int MAX_HISTORY_ITEMS = 100;
        private int MAX_LOCK_AGE = 30;

        #endregion // Implementation details 
    }
}
