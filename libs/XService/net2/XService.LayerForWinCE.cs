using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using PAL;
using XService.Utils;
using XService.Utils.IO;

namespace PAL
{
    /// <summary>
    /// ReadonlyInifile
    /// 
    /// </summary>
    public class ReadonlyInifile
    {
        public ReadonlyInifile(string pFilename)
        {
            this.fileinfo = new FileInfo(pFilename);
        }

        public void Reload()
        {
            this.values.Clear();
            LoadIniFile();
        }

        public string this[string pSection, string pKey]
        {
            get { return ReadString(pSection, pKey, ""); }
            set { WriteString(pSection, pKey, value); }
        }

        public string ReadString(string pSection, string pKey, string pDefaultValue)
        {
            EnsureLoaded();
            pSection = pSection.ToUpper();
            if (this.values.ContainsKey(pSection))
            {
                Dictionary<string, string> section = this.values[pSection];
                pKey = pKey.ToUpper();
                if (section.ContainsKey(pKey))
                    return section[pKey];
            }
            return pDefaultValue;
        }

        public void WriteString(string pSection, string pKey, string pDefault)
        {
            throw new XServiceError("ReadonlyInifile.WriteString - is not implemented!");
        }

        protected void EnsureLoaded()
        {
            if (this.loaded) return;
            this.loaded = true;
            LoadIniFile();
        }

        protected virtual void LoadIniFile()
        {
            if (!this.fileinfo.Exists) return;

            Dictionary<string, string> section = null;
            using (StreamReader sr = this.fileinfo.OpenText())
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim(StrUtils.STR_SPACES.ToCharArray());
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("#") || line.StartsWith(";")) continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        string sectionName = line.Remove(line.Length - 1, 1).Remove(0, 1).Trim(StrUtils.STR_SPACES.ToCharArray()).ToUpper();
                        section = new Dictionary<string, string>();
                        this.values.Add(sectionName, section);
                        continue;
                    }

                    if (section == null)
                    {
                        section = new Dictionary<string, string>();
                        this.values.Add("", section);
                    }

                    int p = line.IndexOf('=');
                    if (p < 0) continue;

                    string name = line.Substring(0, p).Trim(StrUtils.STR_SPACES.ToCharArray()).ToUpper();
                    string value = line.Remove(0, p + 1).Trim(StrUtils.STR_SPACES.ToCharArray());

                    section[name] = value;
                }
            }
        }

        private bool loaded = false;
        private FileInfo fileinfo = null;
        private Dictionary<string, Dictionary<string, string>> values = new Dictionary<string, Dictionary<string, string>>();
    }
}

namespace XService.Utils
{
    public class XServiceError : Exception
    {
        public XServiceError(string message) : base(message) { }
    }

    public class EnvironmentUtils
    {
        public static bool IsWindowsPlatform
        {
            get
            {
                string s = ConfigurationManager.AppSettings["Platform"];
                if (!string.IsNullOrEmpty(s))
                {
                    bool isWindows = (
                        string.Compare(s, "Windows", true) == 0
                        || string.Compare(s, "WinNT", true) == 0
                        || string.Compare(s, "Win32", true) == 0
                        || string.Compare(s, "Win32s", true) == 0
                        );
                    return isWindows;
                }
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT: return true;
                    case PlatformID.Win32S: return true;
                    case PlatformID.Win32Windows: return true;
                    default:
                        return false;
                }
            }
        }

    }


    /// <summary>
    /// StrUtils - utilities to manipulate strings.
    /// </summary>
    public sealed class CollectionUtils
    {
        public static string Join(Array list, string delimiter)
        {
            StringBuilder sb = new StringBuilder(list.Length * 16);
            foreach (object item in list)
            {
                if (sb.Length > 0) sb.Append(delimiter);
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }

        public static string Join(IList list, string delimiter)
        {
            StringBuilder sb = new StringBuilder(list.Count * 16);
            foreach (object item in list)
            {
                if (sb.Length > 0) sb.Append(delimiter);
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parse string of format "key1=value1; key2=value2; ... keyN=valueN;"
        /// To split key and value instead of '=' you could also use ':'.
        /// </summary>
        /// <param name="parameters">String to parse</param>
        /// <param name="forceLowerCaseKeys">If true all key names will be lowercased</param>
        /// <returns>Hastable object that contains parsed parameters.</returns>
        private static char[] nv_delims = new char[] { '=', ':' };
        public static Hashtable ParseParametersStr(string parameters, bool forceLowercaseKeys)
        {
            Hashtable result = new Hashtable();
            string[] items = parameters.Split(';');
            foreach (string item in items)
            {
                // skip empty items
                if (item.Trim() == string.Empty) continue;

                // search name-value delimiter
                int p = item.IndexOfAny(nv_delims);
                string key = "";
                string val = "";
                if (p >= 0) // if found ...
                {
                    key = item.Substring(0, p).Trim();
                    val = item.Remove(0, p + 1);
                }
                else
                    key = item;

                // put new item to collection
                if (forceLowercaseKeys) key = key.ToLower();
                result[key] = val;
            }
            return result;
        }

        public static Dictionary<string, string> ParseParametersStrEx(string pValues, string pNnDelimiters, char pItemsDelimiter, bool forceLowercaseKeys)
        {
            Dictionary<string, string> ht = new Dictionary<string, string>();
            string[] defs = pValues.Split(pItemsDelimiter);
            foreach (string def in defs)
            {
                if (def.Trim() == string.Empty) continue;

                string an = def;
                string av = "";

                int p = def.IndexOfAny(pNnDelimiters.ToCharArray());
                if (p >= 0)
                {
                    an = def.Substring(0, p).Trim();
                    av = def.Substring(p + 1, def.Length - p - 1);
                }

                ht[forceLowercaseKeys ? an.ToLower() : an] = av;
            }
            return ht;
        }

        public static Dictionary<string, string> ParseParametersStrEx(string pValues, bool forceLowercaseKeys)
        {
            Dictionary<string, string> ht = new Dictionary<string, string>();
            string[] defs = pValues.Split(';');
            foreach (string def in defs)
            {
                if (def.Trim() == string.Empty) continue;

                string an = def;
                string av = "";

                int p = def.IndexOf("=");
                if (p >= 0)
                {
                    an = def.Substring(0, p).Trim();
                    av = def.Substring(p + 1, def.Length - p - 1);
                }

                ht[forceLowercaseKeys ? an.ToLower() : an] = av;
            }
            return ht;
        }

        public delegate string GetParameterValue(string pParamName);

        public static string ReplaceParameters(string pText, string pMarkers, GetParameterValue pGetValue)
        {
            string[] markers = pMarkers.Split(',');
            if (markers.Length < 1) return pText;

            string marker_Start = markers[0];
            string marker_End = markers.Length > 1 ? markers[1] : markers[0];
            // _123456789_123456789_123456789
            // pwd=$(pwd);abc=$(abc);
            int ps = -1, pe = -1;
            int offset = 0;
            do
            {
                ps = pText.IndexOf(marker_Start, offset);
                if (ps < 0) break;

                pe = pText.IndexOf(marker_End, ps + 1);
                if (pe < 0) break;

                string pn = pText.Substring(ps, pe - ps + 1);
                if (!pn.StartsWith(marker_Start) || !pn.EndsWith(marker_End))
                {
                    offset = pe;
                    continue;
                }
                pn = pn.Remove(pn.Length - marker_End.Length, marker_End.Length).Remove(0, marker_Start.Length);

                string pv = (pGetValue != null ? pGetValue(pn) : string.Empty);
                if (pv == null) pv = string.Empty;
                pText = pText.Remove(ps, pe - ps + 1).Insert(ps, pv);

                offset = ps + pv.Length;
            }
            while (ps >= 0 && pe >= 0);

            return pText;
        }


    } /* CollectionUtils */


    /// <summary>
    /// CommonUtils
    /// </summary>
    public sealed class TypeUtils
    {
        public static Type[] EmptyTypesArr = new Type[] { };

        public static string IncludeTypeNamespace(string pTypeId, string pNamespace)
        {
            int comma = pTypeId.IndexOf(',');
            int dot = pTypeId.IndexOf('.');
            if (comma >= 0)
            {
                // if dot is part of assembly-name dot is not found at all...
                if (dot >= comma || dot < 0)
                    return StrUtils.IncludeTrailing(pNamespace, '.') + pTypeId;
            }
            else
            {
                // if no assembly-name specified and dot is not found at all...
                if (dot < 0)
                    return StrUtils.IncludeTrailing(pNamespace, '.') + pTypeId;
            }
            return pTypeId;
        }

        /*
        /// <summary>
        /// Returns type by specified identifier.
        /// Type identifier should contain full name of type.
        /// Optionally it can contains name of assembly in which type if declared.		
        /// Type identifier format is:
        ///   FullTypeName [, AssemblyName]
        /// </summary>
        /// <param name="TypeId">Type identifier. Example: "System.Diagnistics.MyTracer,Tracer"</param>
        /// <returns>Found type of null if type is not found</returns>
        public static Type GetTypeById(string pTypeId)
        {
            string asm_name = "";
            string type_name = pTypeId.Trim(StrUtils.STR_SPACES.ToCharArray());
            int n = pTypeId.IndexOf(',');
            bool isAsmNameSpecified = false;
            if (n >= 0)
            {
                // _12345678
                // Type, Asm
                asm_name = pTypeId.Substring(n + 1).Trim(StrUtils.STR_SPACES.ToCharArray());
                type_name = type_name.Substring(0, n).Trim(StrUtils.STR_SPACES.ToCharArray());
                isAsmNameSpecified = true;
            }

            Assembly asm = Assembly.GetExecutingAssembly();
            if (asm_name != null && asm_name != string.Empty)
            {
                asm = Assembly.Load(new AssemblyName(asm_name));
                if (asm == null) return null;
            }
            else
                isAsmNameSpecified = false;

            Type t = SearchType(type_name, asm);
            if (t == null && !isAsmNameSpecified)
            {
                asm = Assembly.GetCallingAssembly();
                t = SearchType(type_name, asm);
            }
            return t;
        }
        */


        public static IFormatProvider GetFmt(Type pType)
        {
            return (IFormatProvider)CultureInfo.CurrentCulture.GetFormat(pType);
        }

        public static object CnvChangeType(object pValue, System.Type pType)
        {
            return Convert.ChangeType(pValue, pType,
                (IFormatProvider)CultureInfo.CurrentCulture.GetFormat(pType));
        }

        /// <summary>
        /// Search specified type (by name) in specified assembly
        /// </summary>
        /// <param name="TypeName">Name of type to search</param>
        /// <param name="asm">Assembly to search type in</param>
        /// <returns>Found type of null if type name is not found</returns>
        public static Type SearchType(string TypeName, Assembly asm)
        {
            TypeName = TypeName.ToLower();
            Type[] types = asm.GetTypes();
            foreach (Type t in types)
            {
                if (t.FullName.ToLower() == TypeName)
                    return t;
            }
            return null;
        }


        /// <summary>
        /// Retrieve custom attribute of specified type.
        /// </summary>
        /// <param name="AType">Type to retrieve custom attribute from</param>
        /// <param name="inherit"></param>
        /// <param name="AAttrType">Type of attribute to retrieve</param>
        /// <returns></returns>
        public static object GetCustomAttribute(Type AType, bool inherit, Type AAttrType)
        {
            object[] attrs = AType.GetCustomAttributes(inherit);
            foreach (Attribute attr in attrs)
            {
                if (attr.GetType().Equals(AAttrType))
                    return attr;
            }
            return null;
        }


        /// <summary>
        /// Create instance of specified type.
        /// Pass to type constructor specified parameters.
        /// </summary>
        /// <param name="AType">Type of instance to create</param>
        /// <param name="prms">Parameters to pass into type constructor. Use null if not parameters expected</param>
        /// <returns>Instance of created object or null if no public default constructor found</returns>
        public static object CreateDefaultInstance(Type AType, params object[] prms)
        {
            ConstructorInfo ci = AType.GetConstructor(
              BindingFlags.Instance | BindingFlags.Public,
              null, new Type[] { }, null);
            if (ci != null)
            {
                return ci.Invoke(prms);
            }
            return null;
        }


        /// <summary>
        /// Returns the path to direcory with main application executable 
        /// </summary>
        public static string ApplicationHomePath
        {
            get 
            {
                return ".";
                //return XService.Utils.IO.PathUtils.IncludeTrailingSlash(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)); 
            }
        }


        /// <summary>
        /// Returns the path to direcory with calling assembly (XService)
        /// </summary>
        public static string AssemblyHomePath
        {
            get 
            {
                return ".";
                //return XService.Utils.IO.PathUtils.IncludeTrailingSlash(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)); 
            }
        }


        /// <summary>
        /// Returns the path to executable
        /// </summary>
        public static string ApplicationName
        {
            get
            {
                //string s = Path.ChangeExtension(Path.GetFileName(Assembly.GetEntryAssembly().Location), "");
                string s = Assembly.GetExecutingAssembly().FullName;
                s = StrUtils.GetToPattern(s, ",");
                s = s.Trim(" .".ToCharArray());
                return s;
            }
        }

    } /* TypeUtils */
}

namespace XService.Utils
{
    /// <summary>
    /// StrUtils - utilities to manipulate strings.
    /// </summary>
    public sealed class StrUtils
    {
        public static string STR_SPACES = " \t\r\n";

        public static string NameToPhrase(string s)
        {
            int i = s.Length - 1;
            char ch = '\0', prevCh = '\0';
            while (i >= 0)
            {
                ch = s[i];
                bool isBigL = (ch >= 'A' && ch <= 'Z');
                bool isPrevSmallL = (prevCh >= 'a' && prevCh <= 'z');
                if (isBigL && isPrevSmallL)
                {
                    s = s.Insert(i, " ");
                }

                prevCh = ch;
                i--;
            }
            return s;
        }

        public static bool IsSameText(string s1, string s2)
        {
            return (string.Compare(s1, s2, true) == 0);
        }

        public static bool IsSameStr(string s1, string s2)
        {
            return (string.Compare(s1, s2, false) == 0);
        }

        public static bool IsEmptyStr(string s)
        {
            return (s == null || s == string.Empty);
        }

        public static bool GetAsBool(string s)
        {
            s = s.Trim().ToLower();
            return (s == "yes" || s == "true" || s == "1");
        }

        public static bool GetAsBool(string s, out bool Value)
        {
            Value = false;
            s = s.Trim().ToLower();
            if (s == "yes" || s == "true" || s == "1")
            {
                Value = true;
                return true;
            }
            else if (s == "no" || s == "false" || s == "0")
            {
                Value = false;
                return true;
            }
            return false;
        }

        private static char[] _DIGITS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public static bool GetAsInt(string s, out int Value)
        {
            Value = -1;
            if (string.IsNullOrEmpty(s) || s.Trim(_DIGITS) != string.Empty)
                return false;
            Value = Convert.ToInt32(s);
            return true;
        }

        public static bool GetAsLong(string s, out long Value)
        {
            Value = -1;
            if (string.IsNullOrEmpty(s) || s.Trim(_DIGITS) != string.Empty)
                return false;
            Value = Convert.ToInt64(s);
            return true;
        }

        public static int StrToIntDef(string s, int DefaultValue)
        {
            int n = DefaultValue;
            if (GetAsInt(s, out n))
                return n;
            return DefaultValue;
        }

        public static long StrToLongDef(string s, long DefaultValue)
        {
            long n = DefaultValue;
            if (GetAsLong(s, out n))
                return n;
            return DefaultValue;
        }

        public static string Right(string s, int n)
        {
            return s.Substring(s.Length - n, n);
        }

        // *** Starting patter
        public static string IncludeStarting(string str, char ch)
        {
            return IncludeStarting(str, "" + ch);
        }

        public static string IncludeStarting(string str, string pattern)
        {
            if (!str.StartsWith(pattern))
                return pattern + str;
            return str;
        }

        public static string ExcludeStarting(string str, char ch)
        {
            return ExcludeStarting(str, "" + ch);
        }

        public static string ExcludeStarting(string str, string pattern)
        {
            if (str.StartsWith(pattern))
                return str.Remove(0, pattern.Length);
            return str;
        }

        // *** Trailing patter
        public static string IncludeTrailing(string str, char ch)
        {
            return IncludeTrailing(str, "" + ch);
        }

        public static string IncludeTrailing(string str, string pattern)
        {
            if (!str.EndsWith(pattern))
                return str + pattern;
            return str;
        }

        public static string ExcludeTrailing(string str, char ch)
        {
            return ExcludeTrailing(str, "" + ch);
        }

        public static string ExcludeTrailing(string str, string pattern)
        {
            if (str.EndsWith(pattern))
                return str.Remove(str.Length - pattern.Length, pattern.Length);
            return str;
        }

        // ***
        public static bool ParseSingleNameValueItem(string pStr, string pDelimiters, out string pName, out string pValue)
        {
            pName = null;
            pValue = null;

            int p = pStr.IndexOfAny(pDelimiters.ToCharArray());
            if (p < 0) return false;

            pName = pStr.Substring(0, p).Trim(STR_SPACES.ToCharArray());
            pValue = pStr.Remove(0, p + 1).Trim(STR_SPACES.ToCharArray());

            return true;
        }

        public static string GetToPattern(string pStr, string pPattern)
        {
            int p = pStr.IndexOf(pPattern);
            if (p < 0) return null;

            return pStr.Substring(0, p);
        }

        public static string GetAfterPattern(string pStr, string pPattern)
        {
            int p = pStr.IndexOf(pPattern);
            if (p < 0) return null;

            return pStr.Remove(0, p + 1);
        }

        public static string Join(string[] pItems, string pSeparator)
        {
            string txt = null;
            foreach (string item in pItems)
            {
                if (string.IsNullOrEmpty(txt))
                {
                    txt = item;
                }
                else
                    txt += (pSeparator + item);
            }
            return txt;
        }

        public static string Join(Dictionary<string, string> pItems, string pSeparator, string pKeyValueSeparator)
        {
            string txt = null;
            foreach (KeyValuePair<string, string> item in pItems)
            {
                if (string.IsNullOrEmpty(txt))
                {
                    txt = (item.Key + pKeyValueSeparator + item.Value);
                }
                else
                    txt += (pSeparator + (item.Key + pKeyValueSeparator + item.Value));
            }
            return txt;
        }

        public static string ExpandParameters(string pStr, Dictionary<string, string> pParams, bool pExpandEnvironmentVars)
        {
            if (pParams != null)
            {
                foreach (KeyValuePair<string, string> prm in pParams)
                {
                    string id = "$(" + prm.Key.ToUpper() + ")";
                    int p = pStr.ToUpper().IndexOf(id);
                    if (p >= 0)
                    {
                        pStr = pStr.Remove(p, id.Length);
                        pStr = pStr.Insert(p, prm.Value);
                    }
                }
            }

            if (pExpandEnvironmentVars)
                pStr = StrUtils.ExpandEnvironmentVariables(pStr);

            return pStr;
        }

        public static string ExpandEnvironmentVariables(string pStr)
        {
            return pStr;
        }

        /// <summary>
        /// Converts specified DateTime value into NSK-timestamp string
        /// </summary>
        /// <param name="pDT">DateTime value to convert</param>
        /// <returns>NSK-timestamp string</returns>
        public static string NskTimestampOf(DateTime pDT)
        {
            return string.Format("{0:0###}-{1:0#}-{2:0#}:{3:0#}:{4:0#}:{5:0#}.{6:0#####}",
                pDT.Year, pDT.Month, pDT.Day, pDT.Hour, pDT.Minute, pDT.Second, pDT.Millisecond * 1000);
        }

        
    } /* StrUtils */
}


namespace XService.Utils.IO
{
    /// <summary>
    /// PathUtils - utilities to manipulate file pathes and file names
    /// </summary>
    public sealed class PathUtils
    {
        public static string IncludeTrailingSlash(string str)
        {
            int n1 = str.IndexOf(Path.DirectorySeparatorChar);
            int n2 = str.IndexOf(Path.AltDirectorySeparatorChar);
            if (n1 >= 0 || (n1 < 0 && n2 < 0))
                return StrUtils.IncludeTrailing(str, Path.DirectorySeparatorChar);
            else
                return StrUtils.IncludeTrailing(str, Path.AltDirectorySeparatorChar);
        }

        public static string ExcludeTrailingSlash(string str)
        {
            int n1 = str.IndexOf(Path.DirectorySeparatorChar);
            int n2 = str.IndexOf(Path.AltDirectorySeparatorChar);
            if (n1 >= 0 || (n1 < 0 && n2 < 0))
                return StrUtils.ExcludeTrailing(str, Path.DirectorySeparatorChar);
            else
                return StrUtils.ExcludeTrailing(str, Path.AltDirectorySeparatorChar);
        }

        public static string FixPath(string path)
        {
            /*if (path.StartsWith("~/"))
                path = path.Replace("~/", IncludeTrailingSlash(AppDomain.CurrentDomain.BaseDirectory));
            else
                if (path.StartsWith(@"~\"))
                    path = path.Replace(@"~\", IncludeTrailingSlash(AppDomain.CurrentDomain.BaseDirectory));
            */

            return FixDirSeparators(path);
        }

        public static string FixDirSeparators(string name)
        {
            // if both types of directory separators found ...
            if (name.IndexOf(Path.DirectorySeparatorChar) >= 0 && name.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
                name = name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return name;
        }

        /// <summary>
        /// Load list of files rom specified path of specified filespec.
        /// </summary>
        /// <param name="path">path to search files in</param>
        /// <param name="mask">filespec (files mask)</param>
        /// <returns></returns>
        public static List<FileInfo> LoadFilesList(string path, string filespec)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles(filespec);
            List<FileInfo> list = new List<FileInfo>(files.Length);
            foreach (FileInfo item in files)
            {
                list.Add(item);
            }
            return list;
        }

    } /* PathUtils */
    
}

namespace System.Configuration
{
    public class ConfigurationManager : ConfigurationSettings
    {
    }

    public class ConfigurationSettings
    { 
        public class Settings
        {
            public string this[string pName]
            {
                get
                {
                    return "";
                }
            }
        }

        private static Settings appSettings = null;
        public static Settings AppSettings
        {
            get
            {
                if (appSettings == null)
                {
                    appSettings = new Settings();
                }
                return appSettings;
            }
        }
    }
}


namespace System.Diagnostics
{
    public class TraceSwitch
    {
        public TraceSwitch(string pName1, string pName2)
        {
            ReadTraceLevel(pName1);
        }

        protected void ReadTraceLevel(string pName)
        { 
        }

        public bool TraceError { get { return (this.traceLevel <= 1); } }
        public bool TraceWarning { get { return (this.traceLevel <= 2); } }
        public bool TraceInfo { get { return (this.traceLevel <= 3); } }
        public bool TraceVerbose { get { return (this.traceLevel <= 4); } }

        public int TraceLevel 
        {
            get { return this.traceLevel; }
            set { this.traceLevel = value; } 
        }

        private int traceLevel = 0;
    }

    public class TraceCE
    {
        public delegate void WriteLineMethod(string pMsg);

        public static WriteLineMethod TraceHook = null;

        public static void WriteLine(string pMsg)
        {
            if (TraceHook != null)
                TraceHook(pMsg);
        }

        public static void WriteLineIf(bool pCondition, string pMsg)
        {
            if (pCondition)
                WriteLine(pMsg);
        }
    }
}
