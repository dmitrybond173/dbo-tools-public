/*
 * Custom TraceListener by Dmitry Bond.
 * Date August 24, 2005
 */
namespace System.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Web;
    using XService.Utils;

    /// <summary>
    /// TraceSwitchEx is extended version of TraceSwitch.
    /// The main difference - it register all instanced in TraceSwitchEx.Instances list.
    /// </summary>
    public class TraceSwitchEx : TraceSwitch
    {
        /// <summary>All instances of TraceSwitchEx will be saved in this list</summary>
        public static List<TraceSwitch> Instances
        {
            get 
            {
                lock (typeof(TraceSwitch))
                {
                    if (instances == null)
                        instances = new List<TraceSwitch>();
                    return instances;
                }
            }
        }

        /// <summary>Unregister (remove) specified instances of TraceSwitchEx from list of instances</summary>
        public static void Unregister(TraceSwitch pInstance)
        {
            lock (typeof(TraceSwitch))
            {
                if (Instances.Contains(pInstance))
                    Instances.Remove(pInstance);
            }
        }

        /// <summary>Calling constructor of TraceSwitch and register object instance in TraceSwitchEx.Instances list</summary>
        public TraceSwitchEx(string displayName, string description)
            : base(displayName, description)
        {
            Instances.Add(this);
        }

        /// <summary>Calling constructor of TraceSwitch and register object instance in TraceSwitchEx.Instances list</summary>
        public TraceSwitchEx(string displayName, string description, string defaultSwitchValue)
            : base(displayName, description, defaultSwitchValue)
        {
            Instances.Add(this);
        }

        private static List<TraceSwitch> instances;
    }

    /// <summary>
    /// AdvancedTraceListener is extended version of TextWriterTraceListener class.
    /// It supports many nice features like - log files cleanup and rotation, log records formatting and so on.
    /// 
    /// Full list of supported options:
    ///   * MaxFileSize = {number}[kb|mb] - max log file size, when bigger it will perform log routing (by size or by filename)
    ///   * Encoding = {encoding} - text encoding for log file
    ///   * SavedLogPercents = {number} - how many percents of log file to save when doing log file routing by size
    ///   * LogSizeOverAction = {Nothing|ResetSize|Rename}  - method of log file routing: nothing or save only 1st NN percents of log or rename log file to *.Lnn
    ///   * MaxRenamedFiles = {number} - how many renamed log files to save when LogSizeOverAction=Rename
    ///   * LinePrefix = {text} - pettern to insert in front of each log line
    ///   * TimeRotationFilenamePattern = {timestamp-pattern} - pattern for timestamp value to router log files (timestamp value will be inserted as suffix into log filename)
    ///   * TimeStampFormat = {timestamp-pattern} - pattern for timestamp value to insert in front of each log line
    ///   * AutoFlush = {false|TRUE} - auto-flush after each log write
    ///   * CleanupOlderThan = {number} [weeks|days|hours|] - auto-delete routed log files which are older than specified time interval, when time specifier is not specified then it is time in minutes
    ///   
    /// Here is example of section in app.config:
    /// <example>
    ///     <system.diagnostics>
    ///        <!-- [...] -->
    ///        <trace autoflush="true">
    ///            <listeners>
    ///                <add name="defaultLogger" type="System.Diagnostics.AdvancedTraceListener, XService.Net2"
    ///                     initializeData="$XService-TestApp-$(TimeRoute)-$(PID).log;LinePrefix=${TID};TimeRouteFilenamePattern=yyyyMMdd-HH;CleanupOlderThan=7days"/>
    ///            </listeners>
    ///        </trace>
    ///    </system.diagnostics>
    /// </example>
    /// </summary>
    public class AdvancedTraceListener : TextWriterTraceListener
    {
        public const string NAME_MaxFileSize = "MaxFileSize";
        public const string NAME_Encoding = "Encoding";
        public const string NAME_SavedLogPercents = "SavedLogPercents";
        public const string NAME_LogSizeOverAction = "LogSizeOverAction";
        public const string NAME_MaxRenamedFiles = "MaxRenamedFiles";
        public const string NAME_LogLinePrefix = "LinePrefix";
        public const string NAME_TimeRouteFilenamePattern = "TimeRouteFilenamePattern";
        public const string NAME_TimeRouteFilenamePattern2 = "TimeRotationFilenamePattern";
        public const string NAME_TimeStampFormat = "TimeStampFormat";
        public const string NAME_AutoFlush = "AutoFlush";
        public const string NAME_CleanupOlderThan = "CleanupOlderThan";

        public AdvancedTraceListener()
            : base()
        {
            this.appStartTs = Process.GetCurrentProcess().StartTime;
            this.appStartTsStr = StrUtils.CompactNskTimestampOf(this.appStartTs);
            this.Writer = null;
            LastInstance = this;
        }

        public AdvancedTraceListener(string pInitializationData)
            : base()
        {
            this.filename = pInitializationData;
            this.appStartTs = Process.GetCurrentProcess().StartTime;
            this.appStartTsStr = StrUtils.CompactNskTimestampOf(this.appStartTs);
            this.Writer = null;
            ParseInitializationData();
            LastInstance = this;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (this.Writer != null)
                {
                    try
                    {
                        TextWriter wr = this.Writer;
                        this.Writer = null;
                        wr.Dispose();
                    }
                    catch { }
                }
            }
            
            if (LastInstance != null && LastInstance.Equals(this))
                LastInstance = null;

            GC.SuppressFinalize(this);
        } 

        #region Interface

        public static bool AppendTimestamp = true;

        public static string TimeStampFormat = "yyyyMMdd,HHmmss.ff";

        public static AdvancedTraceListener LastInstance = null;

        public string Filename
        {
            get { return this.filename; }
            set 
            {
                if (StrUtils.IsSameText(this.filename, value)) return;

                this.filename = value;
                InitWriter();
            }
        }

        public override void Write(string message)
        {
            // if this call is part of call to this.WriteLine ...
            if (this.writing == true)
            {
                // pass it to base class as is
                base.Write(message);
                return;
            }

            try
            {
                // if writer still is not initialized ...
                if (this.Writer == null)
                    this.Writer = InitWriter(); // initialize it

                // if writer is ready ...
                if (this.Writer != null)
                {
                    // seek to end of file to avoid problems with overlaped IO
                    StreamWriter sw = (this.Writer as StreamWriter);
                    if (sw != null)
                    {
                        if (this.maxFileSize > 0 && sw.BaseStream.Length > this.maxFileSize)
                            ReopenWriter();
                        sw.BaseStream.Seek(0, SeekOrigin.End);
                    }
                    base.Write(message);
                }
            }
            catch
            {
                // tracing and logging should not lead to application crash!
                // then we should ignore any exception here.
            }
        }

        public override void WriteLine(string message)
        {
            try
            {
                bool isWriterJustInitialized = false;
                // if writer still is not initialized ...
                if (this.Writer == null)
                {
                    this.Writer = InitWriter(); // initialize it
                    isWriterJustInitialized = true;
                }

                // if writer is ready ...
                if (this.Writer != null)
                {
                    // seek to end of file to avoid problems with overlaped IO
                    StreamWriter sw = (this.Writer as StreamWriter);
                    if (sw != null)
                    {
                        // check file size
                        if (this.maxFileSize > 0 && sw.BaseStream.Length > this.maxFileSize)
                        {
                            ReopenWriter();
                            isWriterJustInitialized = true;
                        }
                        sw.BaseStream.Seek(0, SeekOrigin.End);
                    }

                    if (!isWriterJustInitialized && !string.IsNullOrEmpty(this.timeRotationFilenamePattern))
                    {
                        bool isNameChanged = false;
                        try
                        {
                            string newTimeRouteValue = GetTimeRouteValue(); 
                            if (string.IsNullOrEmpty(this.timeRotationValue))
                                this.timeRotationValue = newTimeRouteValue;
                            isNameChanged = (string.Compare(newTimeRouteValue, this.timeRotationValue, true) != 0);
                        }
                        catch { }
                        if (isNameChanged)
                        {
                            LogSizeOverAction saved = this.logSizeOverAction;
                            logSizeOverAction = LogSizeOverAction.Nothing;
                            try { ReopenWriter(); }
                            finally { logSizeOverAction = saved; }
                        }
                    }

                    // check if need to cleanup old log files
                    if (this.cleanupOlderThanMin > 0)
                    {
                        TimeSpan tDiff = DateTime.Now - this.lastCleanup;
                        if (tDiff.TotalMinutes > 1 || this.lastCleanup == DateTime.MinValue)
                        {
                            this.lastCleanup = DateTime.Now;
                            cleanupOldFiles();
                        }
                    }

                    // set last write
                    this.lastWrite = DateTime.Now;

                    // prepare message 
                    sbText.Remove(0, sbText.Length); // cleanup buffer
                    if (!string.IsNullOrEmpty(this.lineprefix))
                        message = GetLinePrefix() + " " + message;
                    if (AppendTimestamp)
                    {
                        sbText.Append(DateTime.Now.ToString(TimeStampFormat) + " ");
                    }
                    int identLen = this.IndentLevel * this.IndentSize;
                    if (identLen != this.identStr.Length)
                    {
                        if (identLen <= 0)
                            this.identStr = "";
                        else
                            this.identStr = StrUtils.Strng(" ", identLen);
                    }
                    if (this.identStr.Length > 0)
                        sbText.Append(this.identStr);
                    sbText.Append(message);

                    // write 
                    this.writing = true;
                    try { base.WriteLine(sbText.ToString()); }
                    finally { this.writing = false; }

                    if (this.autoFlush)
                        this.Writer.Flush();
                }
            }
            catch (Exception exc)
            {
                // tracing and logging should not lead to application crash!
                // then we should ignore any exception here.
            }
        }

        #endregion // Interface

        #region Protected Interface

        /// <summary>Will close existent writer, rename log file(s), open writer again</summary>
        protected virtual void ReopenWriter()
        {
            switch (this.logSizeOverAction)
            {
                case LogSizeOverAction.Nothing:
                    // close and release writer
                    try { this.Writer.Close(); }
                    finally { this.Writer = null; }
                    // initialize new writer
                    this.Writer = InitWriter();
                    break;

                case LogSizeOverAction.ResetSize:
                    ResetLogFileSize();
                    break;

                case LogSizeOverAction.Rename:
                    RenameLogFile();
                    break;
            }
        }

        /// <summary>Add specified suffix to filename (insert in front of file extension)</summary>
        /// <param name="pFilename">Filename to insert suffix into</param>
        /// <param name="pSuffix">Suffix to insert</param>
        /// <returns>Filename with inserted suffix</returns>
        protected string getSuffixedFilename(string pFilename, string pSuffix)
        {
            if (string.IsNullOrEmpty(pSuffix)) return pFilename;

            // --- instead of "^" pls use $(TimeRoute) macro in log filename ---
            /* 
            // routePattern^ => routePattern + filename <- add filename at right of routePattern
            // ^routePattern => filename + routePattern <- add filename at left of routePattern
            bool isLeft = true; // by default filename added at left of routePattern 
            if (pSuffix.EndsWith("^")) { isLeft = false; pSuffix = pSuffix.Remove(pSuffix.Length - 1, 1); }
            if (pSuffix.StartsWith("^")) { pSuffix = pSuffix.Remove(0, 1); }
            */

            string dir = Path.GetDirectoryName(pFilename);
            string clearFn = Path.GetFileNameWithoutExtension(pFilename);
            string ext = Path.GetExtension(pFilename);
            if (!string.IsNullOrEmpty(dir))
                dir += Path.DirectorySeparatorChar;
            pFilename = dir + clearFn + pSuffix + ext;
            
            /*
            if (isLeft)
                pFilename = dir + clearFn + pSuffix + ext;
            else
                pFilename = dir + pSuffix + clearFn + ext;
            */

            return pFilename;
        }

        /// <summary>
        /// This method opens StreamWriter on first access to the Trace.
        /// In case of failure it will keep silence because logging and tracing 
        /// should not stop application!
        /// But each next attempt to write to Trace will result new attempt 
        /// to open StreamWriter then you could fix trace file location access
        /// problems at run-time without stopping the application.
        /// </summary>
        /// <returns></returns>
        protected virtual TextWriter InitWriter()
        {
            TextWriter txtWriter = null;
            FileStream objStream = null;
            try
            {
                this.timeRotationValue = GetTimeRouteValue();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string actualFn = Environment.ExpandEnvironmentVariables(this.filename);
                //this.filename = fn;

                // make sure base dir is contains trailing slash
                if (!(baseDir.EndsWith("" + Path.DirectorySeparatorChar) || baseDir.EndsWith("" + Path.AltDirectorySeparatorChar)))
                    baseDir += Path.DirectorySeparatorChar;

                // replace ~/ (or ~\) with baseDir
                bool refToBaseDir = (actualFn.StartsWith("~" + Path.DirectorySeparatorChar) || actualFn.StartsWith("~" + Path.AltDirectorySeparatorChar));

                // if no path specified or ref to base dir is used ...
                if (Path.GetPathRoot(actualFn) == "" || refToBaseDir)
                {
                    // fix path to trace log to be full path to file
                    if (refToBaseDir)
                        actualFn = actualFn.Remove(0, 2);
                    actualFn = baseDir + actualFn;
                }

                // fix slashes in path to trace log file
                if (actualFn.IndexOf('/') >= 0 && filename.IndexOf('\\') >= 0)
                    actualFn = filename.Replace('/', Path.DirectorySeparatorChar);

                string filenameToOpen = actualFn;
                if (!string.IsNullOrEmpty(this.timeRotationFilenamePattern))
                {
                    // when $(timeroute) macro is not used then need to add timeRoutePattern to log filename as suffix
                    string chkFn = this.filenameTemplate.ToLower();
                    bool isAlreadyUseTs = (
                        (chkFn.IndexOf("$(timeroute)") >= 0 || chkFn.IndexOf("${timeroute}") >= 0)
                        || (chkFn.IndexOf("$(timerotation)") >= 0 || chkFn.IndexOf("${timerotation}") >= 0)
                        );
                    if (!isAlreadyUseTs)
                    {
                        string trPattern = "";
                        bool isOkToSetByPattern = false;
                        try
                        {
                            //trPattern = string.Format("{0:" + this.timeRouteFilenamePattern + "}", DateTime.Now);
                            trPattern = this.timeRotationValue;
                            isOkToSetByPattern = true;
                        }
                        catch { }
                        if (isOkToSetByPattern && !string.IsNullOrEmpty(trPattern))
                        {
                            filenameToOpen = getSuffixedFilename(filenameToOpen, trPattern);
                        }
                    }
                    else
                    {
                        this.filename = ReplaceMacroValues(this.filenameTemplate);
                        filenameToOpen = this.filename;
                    }
                }

                FileAccess fileMode = FileAccess.Write;
                if (this.logSizeOverAction == LogSizeOverAction.ResetSize)
                    fileMode = FileAccess.ReadWrite;

                // try to open trace log file in sharing mode
                objStream = new FileStream(
                    filenameToOpen, FileMode.OpenOrCreate, fileMode, FileShare.ReadWrite);

                // seek to end
                objStream.Seek(0, SeekOrigin.End);

                // create writer object
                if (this.logEncoding != null)
                    txtWriter = new StreamWriter(objStream, this.logEncoding);
                else
                    txtWriter = new StreamWriter(objStream);
            }
            catch
            {
                // throw new Exception("Unable to create trace log file!");
                // oops...
                // tracing and logging should not lead to application crash!
                // then we should ignore this exception.
            }
            return txtWriter;
        }

        #endregion // Protected Interface

        #region Implementation details

        protected int PID
        {
            get
            {
                if (this._pid < 0) 
                    this._pid = Process.GetCurrentProcess().Id;
                return _pid;
            }
        }

        protected string replaceStr(string s, string pOld, string pNew)
        {
            int p = s.ToLower().IndexOf(pOld.ToLower());
            if (p >= 0)
            {
                s = s.Remove(p, pOld.Length).Insert(p, pNew);
            }
            return s;
        }

        protected virtual string ReplaceMacroValues(string pText)
        {            
            string s = pText;
            if (s.IndexOf("${") >= 0)
            {
                s = replaceStr(s, "${PID}", string.Format("P{0}", this.PID.ToString()));
                s = replaceStr(s, "${TID}", string.Format("T{0}", Thread.CurrentThread.ManagedThreadId.ToString()));
                s = replaceStr(s, "${TN}", string.Format("tn{0}", Thread.CurrentThread.Name));
                s = replaceStr(s, "${StartTs}", this.appStartTsStr.Substring(0, 15));
                s = replaceStr(s, "${StartDate}", this.appStartTsStr.Substring(0, 8));
                s = replaceStr(s, "${StartTime}", this.appStartTsStr.Substring(8, 6));
                s = replaceStr(s, "${TimeRoute}", this.timeRotationValue);
                s = replaceStr(s, "${TimeRotation}", this.timeRotationValue);
                s = replaceStr(s, "${HomePath}", TypeUtils.ApplicationHomePath);
                s = replaceStr(s, "~", TypeUtils.ApplicationHomePath);
                s = replaceStr(s, "${AppName}", TypeUtils.ApplicationName);
            }
            if (s.IndexOf("$(") >= 0)
            {
                s = replaceStr(s, "$(PID)", string.Format("[P{0}]", this.PID.ToString()));
                s = replaceStr(s, "$(TID)", string.Format("[T{0}]", Thread.CurrentThread.ManagedThreadId.ToString()));
                s = replaceStr(s, "$(TN)", string.Format("[tn{0}]", Thread.CurrentThread.Name));
                s = replaceStr(s, "$(StartTs)", this.appStartTsStr.Substring(0, 15));
                s = replaceStr(s, "$(StartDate)", this.appStartTsStr.Substring(0, 8));
                s = replaceStr(s, "$(StartTime)", this.appStartTsStr.Substring(8, 6));
                s = replaceStr(s, "$(TimeRoute)", this.timeRotationValue);
                s = replaceStr(s, "$(TimeRotation)", this.timeRotationValue);
                s = replaceStr(s, "$(HomePath)", TypeUtils.ApplicationHomePath);
                s = replaceStr(s, "$(AppName)", TypeUtils.ApplicationName);
            }
            return s;
        }

        protected virtual string GetLinePrefix()
        {
            string s = ReplaceMacroValues(this.lineprefix);
            if (!string.IsNullOrEmpty(s)) s += " ";
            return s;
        }

        protected virtual string GetTimeRouteValue()
        {
            string pattern = this.timeRotationFilenamePattern;
            if (pattern.StartsWith("^")) pattern = pattern.Remove(0, 1);
            if (pattern.EndsWith("^")) pattern = pattern.Remove(pattern.Length - 1, 1);
            return string.Format("{0:" + pattern + "}", DateTime.Now);
        }

        protected void cleanupOldFiles()
        {
            string fn = this.filename;
            string patt = fn;

            // when $(timeroute) macro is not used then need to add timeRoutePattern to log filename as suffix
            bool isTrAsSuffix = (this.filenameTemplate.ToLower().IndexOf("$(timeroute)") < 0);
            if (isTrAsSuffix)
            {
                int lastDot = fn.LastIndexOf('.');
                patt = fn.Substring(0, lastDot) + "*" + fn.Remove(0, lastDot);
            }
            else
            {
                fn = this.filenameTemplate;
                fn = fn.ToLower().Replace("$(timeroute)", StrUtils.Strng("?", timeRotationFilenamePattern.Length));
                patt = ReplaceMacroValues(fn);
            }

            string path = Path.GetDirectoryName(patt);
            if (string.IsNullOrEmpty(path))
                path = ".";
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles(Path.GetFileName(patt));
            foreach (FileInfo file in files)
            { 
                TimeSpan tDiff = DateTime.Now - file.LastWriteTime;
                if (tDiff.TotalMinutes > this.cleanupOlderThanMin)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { }
                }
            }
        }

        protected int parseCleanupStr(string pValue)
        {
            int lastDig = pValue.LastIndexOfAny(StrUtils.CH_DIGITS);
            string numStr = pValue.Substring(0, lastDig + 1);
            string uomStr = pValue.Remove(0, lastDig + 1).ToUpper();
            int value = Int32.Parse(numStr);
            int multiplier = 1;
            switch (uomStr)
            {
                case "H": multiplier = 60; break;
                case "HOUR": multiplier = 60; break;
                case "HOURS": multiplier = 60; break;
                case "D": multiplier = 24 * 60; break;
                case "DAY": multiplier = 24 * 60; break;
                case "DAYS": multiplier = 24 * 60; break;
                case "W": multiplier = 7 * 24 * 60; break;
                case "WEEK": multiplier = 7 * 24 * 60; break;
                case "WEEKS": multiplier = 7 * 24 * 60; break;
            }
            return value * multiplier;
        }

        protected virtual void ParseInitializationData()
        {
            try
            {
                string[] items = this.filename.Split(';', ',');
                if (items.Length == 1)
                {
                    items = new string[] { this.filename };
                }
                // if something additional parameters specified in filename ...
                if (items.Length > 0)
                {
                    // first item in list is the filename
                    this.filenameTemplate = items[0].TrimEnd();
                    this.filename = ReplaceMacroValues(this.filenameTemplate);

                    this.filename = this.filename.Replace('\\', Path.DirectorySeparatorChar);
                    this.filename = this.filename.Replace('/', Path.DirectorySeparatorChar);
                    this.filename = this.filename.Replace("" + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar, "" + Path.DirectorySeparatorChar);

                    // rest of items is the optional paramateres
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    foreach (string item in items)
                    {
                        string[] pair = item.Split('=', ':');
                        // ignore invalid keys
                        if (pair.Length != 2) continue;
                        parameters[pair[0].Trim().ToLower()] = pair[1].Trim();
                    }

                    // *** load parameter values

                    // Max size of log file (in kilobytes)
                    string key = (NAME_MaxFileSize).ToLower();
                    string value = "";
                    if (parameters.TryGetValue(key, out value))
                        try { this.maxFileSize = Convert.ToInt32(value) * 1024; }
                        catch { /* ignore */ }
                                        
                    // Action performed when size of log file reach specified limit
                    key = (NAME_Encoding).ToLower();
                    if (parameters.TryGetValue(key, out value))
                        try { this.logEncoding = TextUtils.StrToEncoding(value); }
                        catch { /* ignore */ }

                    // Action performed when size of log file reach specified limit
                    key = (NAME_LogSizeOverAction).ToLower();
                    if (parameters.TryGetValue(key, out value))
                        try { this.logSizeOverAction = (LogSizeOverAction)Enum.Parse(typeof(LogSizeOverAction), value, true); }
                        catch { /* ignore */ }

                    // max number of renamed log files (used when LogSizeOverAction = Rename)
                    key = (NAME_MaxRenamedFiles).ToLower();
                    if (parameters.TryGetValue(key, out value))
                        try { this.maxRenamedFiles = Convert.ToInt32(value); }
                        catch { /* ignore */ }

                    // percents of log file to save when size limit reached (used when LogSizeOverAction = ResetSize)
                    key = (NAME_SavedLogPercents).ToLower();
                    if (parameters.TryGetValue(key, out value))
                        try { this.savedLogPercents = Convert.ToInt32(value) % 100; }
                        catch { /* ignore */ }

                    key = (NAME_TimeRouteFilenamePattern2).ToLower();
                    if (parameters.TryGetValue(key, out value))
                    {
                        this.timeRotationFilenamePattern = value;
                        this.timeRotationValue = GetTimeRouteValue();
                        this.filename = ReplaceMacroValues(this.filenameTemplate);
                    }
                    else
                    {
                        key = (NAME_TimeRouteFilenamePattern).ToLower();
                        if (parameters.TryGetValue(key, out value))
                        {
                            this.timeRotationFilenamePattern = value;
                            this.timeRotationValue = GetTimeRouteValue();
                            this.filename = ReplaceMacroValues(this.filenameTemplate);
                        }
                    }


                    key = NAME_CleanupOlderThan.ToLower();
                    if (parameters.TryGetValue(key, out value))
                        this.cleanupOlderThanMin = parseCleanupStr(value);

                    key = (NAME_TimeStampFormat).ToLower();
                    if (parameters.TryGetValue(key, out value))
                        TimeStampFormat = value;

                    key = (NAME_AutoFlush).ToLower();
                    if (parameters.TryGetValue(key, out value))
                        autoFlush = Convert.ToBoolean(value);

                    key = (NAME_LogLinePrefix).ToLower();
                    if (parameters.TryGetValue(key, out value))
                    {
                        string s = value;
                        s = s.Trim(" \t\r\n".ToCharArray());
                        if (s.StartsWith("+"))
                            this.lineprefix += s;
                        else
                            this.lineprefix = s;
                    }
                }
            }
            catch
            {
                // tracing and logging should not lead to application crash!
                // then we should ignore this exception.
            }
        }

        protected virtual void ResetLogFileSize()
        {
            StreamWriter sw = (this.Writer as StreamWriter);
            if (sw == null) return;
            Stream strm = sw.BaseStream;
            // calculate - which part of existent log to save 
            long sz = strm.Length * this.savedLogPercents / 100;
            if (sz > 0)
            {
                byte[] arr = new byte[sz];
                strm.Position = strm.Length - sz;
                strm.Read(arr, 0, (int)sz);
                strm.Position = 0;
                strm.Write(arr, 0, (int)sz);
            }
            strm.SetLength(sz);
        }

        protected virtual void RenameLogFile()
        {
            // close and release writer
            this.Writer.Close();
            this.Writer = null;

            // rename log file
            int index = 1; // search not allocated filename
            string fn = Path.GetFileNameWithoutExtension(this.filename);
            while (File.Exists(fn + String.Format(".L{0:0#}", index)) && index <= this.maxRenamedFiles)
                index++;
            // remove extra renamed logs
            if (index > this.maxRenamedFiles)
            {
                int i = 1;
                while (File.Exists(fn + String.Format(".L{0:0#}", i)) && i <= this.maxRenamedFiles)
                {
                    if (i == 1)
                        File.Delete(fn + String.Format(".L{0:0#}", i));
                    else
                        File.Move(fn + String.Format(".L{0:0#}", i), fn + String.Format(".L{0:0#}", i - 1));
                    i++;
                }
                index = this.maxRenamedFiles;
            }
            fn += String.Format(".L{0:0#}", index);
            File.Move(this.filename, fn);

            // initialize new writer
            this.InitWriter();
        }

        protected enum LogSizeOverAction
        {
            Nothing,
            ResetSize, // save NN percents of log
            Rename,    // rename log file to *.Lnn
        };

        protected string filenameTemplate;
        protected string filename;
        protected string lineprefix;
        protected string identStr = "";
        protected int maxFileSize = -1;
        protected int savedLogPercents = 10;
        protected int maxRenamedFiles = 10;
        protected int cleanupOlderThanMin = -1;
        protected Encoding logEncoding = null;
        protected string timeRotationFilenamePattern = "";
        protected string timeRotationValue = "";
        protected LogSizeOverAction logSizeOverAction = LogSizeOverAction.ResetSize;
        protected bool autoFlush = true;
        protected DateTime lastWrite = DateTime.MinValue;
        protected DateTime lastCleanup = DateTime.MinValue;
        private DateTime appStartTs;
        private string appStartTsStr;

        protected StringBuilder sbText = new StringBuilder(0x1000);
        protected bool writing = false;
        private int _pid = -1;

        #endregion // Implementation details
    }

    public class TraceUtils
    {
        /// <summary>
        /// Search TraceListener of specified type
        /// </summary>
        /// <param name="pType"></param>
        /// <returns></returns>
        public static TraceListener FindListener(Type pType)
        {
            int i;
            // 1st iteration - search for exact Type match
            for (i = 0; i < Trace.Listeners.Count; i++)
            {
                TraceListener lsnr = Trace.Listeners[i];
                if (lsnr.GetType().Equals(pType))
                    return lsnr;
            }
            // 1st iteration - search for compatible Type match
            for (i = 0; i < Trace.Listeners.Count; i++)
            {
                TraceListener lsnr = Trace.Listeners[i];
                if (lsnr.GetType().IsAssignableFrom(pType))
                    return lsnr;
            }
            return null;
        }
    }
}
