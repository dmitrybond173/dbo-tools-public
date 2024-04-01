/*
 * Simple utlitities  to work with ADO.NET objects.
 * Written by Dmitry Bond. at June 19, 2007
 */

using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using XService.Utils;
using XService.Security;

namespace XService.Data
{
    public static class DacService
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("DacService", "DacService");

        /// <summary>Name of connection string to search in App.Config file and use it as 'main db connection'</summary>
        public static string ConnectionStringName = "SiteDatabase";

        private static bool? _debugMode;

        /// <summary>Debug mode to show in log all actual params for db connection</summary>
        public static bool DebugMode
        {
            get 
            {
                if (!_debugMode.HasValue)
                {
                    bool q;
                    string s = ConfigurationManager.AppSettings["DacService.DebugMode"];
                    if (s != null && Boolean.TryParse(s, out q))
                        _debugMode = q;
                }
                if (!_debugMode.HasValue)
                    _debugMode = false;
                return _debugMode.Value;
            }
            set { _debugMode = value; }
        }

        /// <summary>
        /// Create DbProviderFactory object for connection string defined in application config file.
        /// By default it is seaching for connection string called "SiteDatabase".
        /// But name of connection string to search could be changed by specifying other value in *DacService.ConnectionStringName*.
        /// </summary>
        /// <returns>Instance of DbProviderFactory object</returns>
        public static ConnectionStringSettings GetConnectionStr()
        {
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("DAC->GetConnectionStr[ {0} ]", ConnectionStringName) : "");
            ConnectionStringSettings cns = ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (cns == null)
                throw new Exception(string.Format(
                    "Connection string ({0}) is not found!", ConnectionStringName));
            return cns;
        }

        /// <summary>
        /// Create DbProviderFactory object for connection string defined in application config file.
        /// By default it is seaching for connection string called "SiteDatabase".
        /// But name of connection string to search could be changed by specifying other value in *DacService.ConnectionStringName*.
        /// </summary>
        /// <returns>Instance of DbProviderFactory object</returns>
        public static DbProviderFactory GetDbFactory()
        {
            string provName = GetConnectionStr().ProviderName;
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("DAC->GetFactory[ {0} ]", provName) : "");
            return DbProviderFactories.GetFactory(provName);
        }

        private static Regex rexpCssPassw1 = new Regex(@"(PWD\s*=\s*).*\;", RegexOptions.IgnoreCase);
        private static Regex rexpCssPassw2 = new Regex(@"(Password=\s*=\s*).*\;", RegexOptions.IgnoreCase);

        public static string CS_ValueDelimiter = ":";

        /// <summary>
        /// Create, initialize and open DbConnection object using connection string defined in application config file.
        /// By default it is seaching for connection string called "SiteDatabase".
        /// But name of connection string to search could be changed by specifying other value in *DacService.ConnectionStringName*.
        /// Please note: it supports $(...) parameters in connection string. So, you can specify in your connection string something like $(DbName) 
        /// and then add DbName parameters into <appSettings> section in your App.config file.
        /// When value of such parameter is enclosed with "{" and "}" it will try to decode it as encrypted text using PasswordService.DecryptPassword() method.
        /// </summary>
        /// <returns>Created, initialized and opened DbConnection object</returns>
        public static DbConnection GetConnection()
        {
            DbProviderFactory dbFactory = GetDbFactory();
            DbConnection db_conn = dbFactory.CreateConnection();

            //cs = CollectionUtils.ReplaceParameters(cs, "$(,)", GetCfgPrmValue);

            DbConnectionStringBuilder csBld = dbFactory.CreateConnectionStringBuilder();
            string cs = GetConnectionStr().ConnectionString;

            bool isEncrypted = (SecureText.Instance.IsEcryptedPattern(cs)
                || (cs.StartsWith("{") && cs.StartsWith("}")) 
                );
            if (isEncrypted)
            {
                // check if whole connection string could be encrypted
                if (cs.StartsWith("{") && cs.StartsWith("}"))
                    cs = PasswordService.DecryptPassword(cs);
                else if (SecureText.Instance.IsEcryptedPattern(cs))
                    SecureText.Instance.DecodeSecuredText(cs, out cs);
                if (DebugMode)
                    Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceInfo ? string.Format("= DAC->Decrypted CS: {0}", cs) : "");
            }

            // check if separate parameters in connection string could be encrypted
            csBld.ConnectionString = cs;
            foreach (string key in csBld.Keys)
            {
                object v = csBld[key];
                if (v == null) continue;

                string s = v.ToString();
                if (s.IndexOf("$(") >= 0)
                {
                    s = CollectionUtils.ReplaceParameters(s, "$(,)", GetCfgPrmValue);
                    if (DebugMode)
                        Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceInfo ? string.Format("= DAC->DbPrm[{0}]: {1} => {2}", key, v.ToString(), s) : "");
                    v = s;
                    csBld[key] = v;
                }

                s = v.ToString();
                if (s.IndexOf("%") >= 0)
                {
                    s = Environment.ExpandEnvironmentVariables(s);
                    if (DebugMode)
                        Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceInfo ? string.Format("= DAC->DbPrm[{0}].env: {1} => {2}", key, v.ToString(), s) : "");
                    v = s;
                    csBld[key] = v;
                }
                                
                bool isComposedValue = (!string.IsNullOrEmpty(CS_ValueDelimiter) && s.IndexOf(CS_ValueDelimiter) >= 0);
                string vSuffix = null;
                if (isComposedValue)
                {
                    vSuffix = StrUtils.GetAfterPattern(s, CS_ValueDelimiter);
                    s = StrUtils.GetToPattern(s, CS_ValueDelimiter);
                }

                // Note: decpryption of connection string parameters could be used not only for parameters but also for directly specified values!
                string openValue = null;
                if (s.StartsWith("{") && s.StartsWith("}"))
                    openValue = PasswordService.DecryptPassword(s);
                else if (SecureText.Instance.IsEcryptedPattern(s))
                    SecureText.Instance.DecodeSecuredText(s, out openValue);                
                if (openValue != null)
                {
                    if (isComposedValue)
                        openValue += (CS_ValueDelimiter + vSuffix);
                    if (DebugMode)
                        Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceInfo ? string.Format(" ! DAC->DbPrm[{0}]: decrypted({1}) => {2}", key, s, openValue) : "");
                    s = openValue;
                    csBld[key] = openValue;
                }
            }
            cs = csBld.ConnectionString;

            if (TrcLvl.TraceInfo)
            {
                string csInfo = cs;
                csInfo = rexpCssPassw1.Replace(csInfo, "$1*******");
                csInfo = rexpCssPassw2.Replace(csInfo, "$1*******");
                Trace.WriteLine(string.Format("DAC->ConnectionString: {0}", csInfo));
            }
            db_conn.ConnectionString = cs;

            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("DAC->Opening connection...") : "");
            db_conn.Open();
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("DAC->Connection State: {0}", db_conn.State) : "");

            return db_conn;
        }

        /// <summary>
        /// Custom delegate method to return value of parameter with specified name. 
        /// Only after this method returns null (or if method is not set) it will try to expand parameter as value from <appSetting> section
        /// </summary>
        public static CollectionUtils.GetParameterValue OnGetCfgValue;

        /// <summary>
        /// Read value of parameter with specified name from App.Config file.
        /// When value of parameter is enclosed with "{" and "}" it will try to decode it as encrypted text using PasswordService.DecryptPassword() method.
        /// When value of parameter is match SecuredText pattern then it will try to decode it using SecuredText algorithm.
        /// </summary>
        /// <param name="pPrmName">Name of parameter to read value for</param>
        /// <returns>Value of parameter with specified name or null if parameter not found</returns>
        public static string GetCfgPrmValue(string pPrmName)
        {
            string v = null;
            if (OnGetCfgValue != null)
                v = OnGetCfgValue(pPrmName);

            if (v == null)
                v = ConfigurationManager.AppSettings[pPrmName];

            if (string.IsNullOrEmpty(v)) return v;

            bool isComposedValue = (v.IndexOf(CS_ValueDelimiter) >= 0);
            string vSuffix = null;
            if (isComposedValue)
            {
                vSuffix = StrUtils.GetAfterPattern(v, CS_ValueDelimiter);
                v = StrUtils.GetToPattern(v, CS_ValueDelimiter);
            }

            string openValue = null;
            if (v.StartsWith("{") && v.EndsWith("}"))
                openValue = PasswordService.DecryptPassword(v);
            else if (SecureText.Instance.IsEcryptedPattern(v))
                SecureText.Instance.DecodeSecuredText(v, out openValue);
            if (openValue != null)
                v = openValue;
            if (isComposedValue)
                v += (CS_ValueDelimiter + vSuffix);

            return v;
        }

        /// <summary>Wait until ADO.NET connection established.</summary>
        /// <param name="pConn">Connection to wait for ConnectionState.Open status</param>
        /// <returns>Returns true when connection got ConnectionState.Open status after 100 cycles, otherwise returns false</returns>
        public static bool WaitConnected(DbConnection pConn)
        {
            int n = 0;
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("DAC->WaitConnected( {0}; {1} )", pConn.State, pConn.ConnectionString) : "");
            while (pConn.State == ConnectionState.Connecting)
            {
                Thread.Sleep(100);
                if (pConn.State == ConnectionState.Open) break;
                n++;
                if (n > 100) break;
            }
            return (pConn.State == ConnectionState.Open);
        }

        /// <summary>Wait until ADO.NET connection established.</summary>
        /// <param name="pConn">Connection to wait for ConnectionState.Open status</param>
        /// <param name="pTimeout">Timeout (in seconds) to wait</param>
        /// <returns>Returns true when connection got ConnectionState.Open status during specified timeout, otherwise returns false</returns>
        public static bool WaitConnected(DbConnection pConn, int pTimeoutSec)
        {
            int n = 0;
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("DAC->WaitConnected( {0}; timeout={1}; {2}; )", pConn.State, pTimeoutSec, pConn.ConnectionString) : "");
            int waitQuantMs = 100;
            int waitCount = (pTimeoutSec * 1000) / waitQuantMs;
            while (pConn.State == ConnectionState.Connecting)
            {
                Thread.Sleep(waitQuantMs);
                if (pConn.State == ConnectionState.Open) break;
                waitCount -= waitQuantMs;
                if (waitCount <= 0) break;
            }
            return (pConn.State == ConnectionState.Open);
        }

        /// <summary>Create new DbParameter object and intiailize it</summary>
        /// <param name="pName">Name of parameter to create</param>
        /// <param name="pType">Datatype of parameter</param>
        /// <param name="pValue">Value of parameter</param>
        /// <returns>Instance of new DbParameter object</returns>
        public static DbParameter NewDbPrm(string pName, DbType pType, object pValue)
        {
            DbParameter prm = GetDbFactory().CreateParameter();
            prm.DbType = pType;
            if (pName != null)
                prm.ParameterName = pName;
            prm.Value = pValue;
            return prm;
        }

        /// <summary>Create new DbParameter object and intiailize it</summary>
        /// <param name="pName">Name of parameter to create</param>
        /// <param name="pType">Datatype of parameter</param>
        /// <param name="pValue">Value of parameter</param>
        /// <returns>Instance of new DbParameter object</returns>
        public static DbParameter NewDbPrm(DbProviderFactory pFactory, string pName, DbType pType, object pValue)
        {
            DbParameter prm = pFactory.CreateParameter();
            prm.DbType = pType;
            if (pName != null)
                prm.ParameterName = pName;
            prm.Value = pValue;
            return prm;
        }

        /// <summary>Ensure DataTable object has not more 1 Row</summary>
        /// <param name="pRowset">DataTable object to cutdown to 1 Row</param>
        public static void CutdownToRow(DataTable pRowset)
        {
            while (pRowset.Rows.Count > 1)
            {
                pRowset.Rows.RemoveAt(1);
            }
        }

        /// <summary>Ensure DataTable object has not more 1 Column x 1 Row</summary>
        /// <param name="pRowset">DataTable object to cutdown to 1 Column x 1 Row</param>
        public static void CutdownToValue(DataTable pRowset)
        {
            CutdownToRow(pRowset);

            while (pRowset.Columns.Count > 1)
            {
                pRowset.Columns.RemoveAt(1);
            }
        }

        /// <summary>Extract all available properties of DbConnectionStringBuilder into a DataTable</summary>
        /// <param name="pCsBuilder">DbConnectionStringBuilder to extract properties from</param>
        /// <returns>DataTable with all available properties of DbConnectionStringBuilder</returns>
        public static DataTable CsBuilderPropertiesToTable(DbConnectionStringBuilder pCsBuilder)
        {
            DataTable csProps = new DataTable("DbConnectionStringBuilder");
            csProps.Columns.Add("Key");
            csProps.Columns.Add("Value");

            DbConnectionStringBuilder cb = pCsBuilder;
            int i = 0;
            string[,] matrix = new string[cb.Keys.Count, 2];
            foreach (string k in cb.Keys)
            {
                matrix[i++, 0] = k;
            }
            i = 0;
            foreach (object v in cb.Values)
            {
                matrix[i++, 1] = v.ToString();
            }
            i = 0;
            foreach (string k in cb.Keys)
            {
                csProps.Rows.Add(matrix[i, 0], matrix[i, 1]);
                i++;
            }
            return csProps;
        }

        /// <summary>Trim spaces at right for all values in DataTable</summary>
        /// <param name="pRowset">DataTable object to trim spaces in</param>
        public static void TrimSpaces(DataTable pRowset)
        {
            foreach (DataRow row in pRowset.Rows)
            {
                foreach (DataColumn col in pRowset.Columns)
                {
                    if (col.ReadOnly) continue;

                    object val = row[col];
                    if (val is string)
                    {
                        row[col] = val.ToString().TrimEnd();
                    }
                }
            }
        }

        /// <summary>Serialize list of DataTable objects in single XML file</summary>
        /// <param name="pTables">list of DataTable objects to serialize</param>
        /// <param name="pFilename">XML filename</param>
        public static void SerializeTables(List<DataTable> pTables, string pFilename)
        { 
            SerializeTables(pTables, pFilename, null);
        }

        /// <summary>Serialize list of DataTable objects in single XML file</summary>
        /// <param name="pTables">list of DataTable objects to serialize</param>
        /// <param name="pFilename">XML filename</param>
        /// <param name="pExtraProps">Dictionary with extra properties to save</param>
        public static void SerializeTables(List<DataTable> pTables, string pFilename, Dictionary<string, string> pExtraProps)
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.ConformanceLevel = ConformanceLevel.Fragment;
            xws.Indent = true;
            xws.CloseOutput = false;
            using (XmlWriter xmlW = XmlWriter.Create(pFilename, xws))
            {
                xmlW.WriteStartElement("DocumentBuffer");
                if (pExtraProps != null)
                {
                    foreach (KeyValuePair<string, string> it in pExtraProps)
                    {
                        xmlW.WriteAttributeString(it.Key, it.Value);
                    }
                }
                for (int i = 0; i < pTables.Count; i++)
                {
                    xmlW.WriteStartElement("Table");

                    xmlW.WriteStartElement("Schema");
                    pTables[i].WriteXmlSchema(xmlW);
                    xmlW.WriteEndElement();

                    xmlW.WriteStartElement("Data");
                    pTables[i].WriteXml(xmlW);
                    xmlW.WriteEndElement();

                    xmlW.WriteEndElement();
                }
                xmlW.WriteEndElement();
                xmlW.Close();
            }
        }

        public static string SerializeTables(List<DataTable> pTables)
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.ConformanceLevel = ConformanceLevel.Fragment;
            xws.Indent = true;
            xws.CloseOutput = false;
            StringBuilder sb = new StringBuilder(0x8000);
            using (XmlWriter xmlW = XmlWriter.Create(sb, xws))
            {
                xmlW.WriteStartElement("DocumentBuffer");
                for (int i = 0; i < pTables.Count; i++)
                {
                    xmlW.WriteStartElement("Table");

                    xmlW.WriteStartElement("Schema");
                    pTables[i].WriteXmlSchema(xmlW);
                    xmlW.WriteEndElement();

                    xmlW.WriteStartElement("Data");
                    pTables[i].WriteXml(xmlW);
                    xmlW.WriteEndElement();

                    xmlW.WriteEndElement();
                }
                xmlW.WriteEndElement();
                xmlW.Close();
            }
            return sb.ToString();
        }

        /// <summary>Deserialize list of DataTable objects from single XML file</summary>
        /// <param name="pTables">list object to add deserialized DataTable objects into</param>
        /// <param name="pFilename">XML filename to deserialized DataTable objects from</param>
        public static void DeserializeTables(List<DataTable> pTables, string pFilename)
        {
            DeserializeTables(pTables, pFilename, null);
        }

        /// <summary>Deserialize list of DataTable objects from single XML file</summary>
        /// <param name="pTables">list object to add deserialized DataTable objects into</param>
        /// <param name="pFilename">XML filename to deserialized DataTable objects from</param>
        /// <param name="pExtraProps">Dictionary object to save extra properties (if found). Can be null to ignore</param>
        public static void DeserializeTables(List<DataTable> pTables, string pFilename, Dictionary<string, string> pExtraProps)
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ConformanceLevel = ConformanceLevel.Fragment;
            xrs.CloseInput = false;
            //using (XmlTextReader xmlR = (XmlTextReader)XmlTextReader.Create(XmlReader.Create(pFilename, xrs), xrs))
            using (XmlTextReader xmlR = new XmlTextReader(pFilename))
            {
                if (pExtraProps != null)
                {
                    while (xmlR.NodeType != XmlNodeType.Element)
                        xmlR.Read();
                    if (xmlR.HasAttributes) //  && xmlR.Name.CompareTo("ExtraProperties") == 0
                    {
                        if (xmlR.MoveToFirstAttribute())
                        {
                            for (int iAttr = 0; iAttr < xmlR.AttributeCount; iAttr++)
                            {
                                xmlR.MoveToAttribute(iAttr);
                                pExtraProps[xmlR.Name] = xmlR.Value;
                            }
                        }
                    }
                }
                xmlR.ReadStartElement("DocumentBuffer");
                int xmlLevel = 0;
                do
                {
                    while (xmlR.NodeType != XmlNodeType.Element && xmlR.NodeType != XmlNodeType.None)
                        xmlR.Read();

                    if (xmlR.NodeType == XmlNodeType.Element)
                    {
                        xmlR.ReadStartElement("Table");

                        xmlLevel = xmlR.Depth;
                        xmlR.ReadStartElement("Schema");
                        DataTable tab = new DataTable();
                        tab.ReadXmlSchema(xmlR);
                        pTables.Add(tab);
                        if (xmlLevel == xmlR.Depth)
                            xmlR.ReadEndElement(); // </Schema>

                        xmlLevel = xmlR.Depth;
                        xmlR.ReadStartElement("Data");
                        tab.ReadXml(xmlR);
                        if (xmlLevel == xmlR.Depth)
                            xmlR.ReadEndElement(); // </Data>

                        xmlR.ReadEndElement();
                    }
                }
                while (xmlR.NodeType != XmlNodeType.None);
                //xmlR.ReadEndElement();
            }
        }

        public static void DeserializeTables(List<DataTable> pTables, Stream pStream)
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ConformanceLevel = ConformanceLevel.Fragment;
            xrs.CloseInput = false;
            using (XmlTextReader xmlR = new XmlTextReader(pStream))
            {
                xmlR.ReadStartElement("DocumentBuffer");
                int xmlLevel = 0;
                do
                {
                    while (xmlR.NodeType != XmlNodeType.Element && xmlR.NodeType != XmlNodeType.None)
                        xmlR.Read();

                    if (xmlR.NodeType == XmlNodeType.Element)
                    {
                        xmlR.ReadStartElement("Table");

                        xmlLevel = xmlR.Depth;
                        xmlR.ReadStartElement("Schema");
                        DataTable tab = new DataTable();
                        tab.ReadXmlSchema(xmlR);
                        pTables.Add(tab);
                        if (xmlLevel == xmlR.Depth)
                            xmlR.ReadEndElement();

                        xmlLevel = xmlR.Depth;
                        xmlR.ReadStartElement("Data");
                        tab.ReadXml(xmlR);
                        if (xmlLevel == xmlR.Depth)
                            xmlR.ReadEndElement();

                        xmlR.ReadEndElement();
                    }
                }
                while (xmlR.NodeType != XmlNodeType.None);
                //xmlR.ReadEndElement();
            }
        }

        /// <summary>Create extra columns in specified DataTable object</summary>
        /// <param name="pExtraColumnsDefs">Extra columns definitions. key={Name}, value={dataType} + "\t" + {columnExpression}</param>
        /// <param name="pTable">Target table to create extra columns in it</param>
        public static void EnsureExtraColumns(Dictionary<string, string> pExtraColumnsDefs, DataTable pTable)
        {
            foreach (KeyValuePair<string, string> item in pExtraColumnsDefs)
            {
                if (pTable.Columns.IndexOf(item.Key) < 0)
                {
                    string colName = item.Key;
                    Type colType = typeof(string);
                    string expr = item.Value;
                    if (expr.IndexOf('\t') >= 0)
                    {
                        string[] items = expr.Split('\t');
                        if (items.Length > 0)
                        {
                            string tID = items[0].Trim(StrUtils.CH_SPACES);
                            Type t = Type.GetType(tID);
                            if (t == null)
                                t = Type.GetType("System." + tID);
                            if (t == null)
                            {
                                string tn = items[0].ToLower();
                                t = StrToScalarType(tn);
                            }
                            if (t == null)
                                throw new ToolConfigError(string.Format("Fail to recognize datatype ({0}) for column ({1})",
                                    items[0], colName));
                            colType = t;
                        }
                        if (items.Length > 1)
                        {
                            expr = items[1].Trim(StrUtils.CH_SPACES);
                        }
                    }
                    DataColumn col = pTable.Columns.Add(colName, colType);
                    col.Expression = expr;
                }
            }
            foreach (DataColumn col in pTable.Columns)
            {
                if (col.ColumnName.StartsWith("_"))
                    col.ColumnMapping = MappingType.Attribute;
            }
        }

        /// <summary>Convert datatype name to Type object</summary>
        /// <param name="tn">datatype name</param>
        /// <returns>Type object</returns>
        public static Type StrToScalarType(string tn)
        {
            Type t;
            if (tn.ToLower().StartsWith("system.")) 
                tn = tn.Remove(0, "system.".Length);
            switch (tn)
            {
                case "bool": 
                case "boolean": t = typeof(bool); break;
                case "byte": t = typeof(byte); break;
                case "short": t = typeof(short); break;
                case "ushort": t = typeof(ushort); break;
                case "int16": t = typeof(short); break;
                case "uint16": t = typeof(ushort); break;
                case "int":
                case "int32": t = typeof(int); break;
                case "uint": 
                case "uint32": t = typeof(uint); break;
                case "long": 
                case "int64": t = typeof(long); break;
                case "ulong": 
                case "uint64": t = typeof(ulong); break;
                case "float": t = typeof(float); break;
                case "double": t = typeof(double); break;
                case "string": t = typeof(string); break;
                case "datetime": t = typeof(DateTime); break;
                default: t = null; break;
            }
            return t;
        }

        /// <summary>Setup columns in DataTable object, if column with certain name already exists it will keep it unchanged (even definition is different)</summary>
        /// <param name="pTable">DataTable object to setup columns in</param>
        /// <param name="pColumnDefs">LOP string with {name[,name2[...,nameN]]}:{datatype}; repeating items which are describing columns</param>
        public static void ConfigureStructure(DataTable pTable, string pColumnDefs)
        {
            string[] items = pColumnDefs.Split(';');
            foreach (string item in items)
            {
                if (string.IsNullOrEmpty(item.Trim())) continue;

                // _12345678
                // abc=bool
                string pn, pv = null;
                int p = item.IndexOfAny(":=".ToCharArray());
                if (p >= 0)
                {
                    pn = item.Substring(0, p).Trim();
                    pv = item.Remove(0, p + 1).Trim();
                }
                else
                    pn = item;
                string[] names = null;
                if (pn.IndexOf(',') >= 0)
                    names = pn.Split(',');

                Type ct = typeof(string);
                if (!string.IsNullOrEmpty(pv))
                {
                    ct = StrToScalarType(pv);
                }
                if (names != null)
                {
                    foreach (string nm in names)
                        pTable.Columns.Add(nm.Trim(), ct);
                }
                else
                    pTable.Columns.Add(pn.Trim(), ct);
            }
        }

        /// <summary>Concatenate content of DataTable into string</summary>
        /// <param name="pTable">DataTable to concatenate content of it</param>
        /// <param name="pIncludeHeader">Flag. If to include column captions as 1st line</param>
        /// <param name="pCellsDelimiter">Delimiter for cells. If null it will use current ListSeparator</param>
        /// <param name="pRowsDelimiter">Delimiter for rows. If null it will use Environment.NewLine</param>
        /// <param name="pCsvFormat">If need to use pure CSV format (enclosed values with double quotes when they contains special charactars)</param>
        /// <returns>String which is result of DataTable content concatenation</returns>
        public static string DataTable_ToString(DataTable pTable, bool pIncludeHeader, string pCellsDelimiter, string pRowsDelimiter, bool pCsvFormat)
        {
            if (pCellsDelimiter == null)
                pCellsDelimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            if (pRowsDelimiter == null)
                pRowsDelimiter = Environment.NewLine;

            StringBuilder sb = new StringBuilder(100 * pTable.Rows.Count * pTable.Columns.Count);
            if (pIncludeHeader)
            {
                foreach (DataColumn dc in pTable.Columns)
                    sb.Append(dc.ColumnName + pCellsDelimiter);
                sb.Append(pRowsDelimiter);
            }
            foreach (DataRow row in pTable.Rows)
            {
                foreach (DataColumn dc in pTable.Columns)
                {
                    string v = (row.IsNull(dc) ? "" : row[dc].ToString());
                    if (pCsvFormat && v.IndexOfAny(StrUtils.CH_QUOTABLE_CHARS) >= 0)
                        v = StrUtils.AnsiQuotedStr(v, '\"');
                    sb.Append(v);
                    sb.Append(pCellsDelimiter);
                }
                sb.Append(pRowsDelimiter);
            }            
            return sb.ToString();
        }

    }

    /*
    public class DacServiceEx
    {
        public string ConnectionStringName = "SiteDatabase";

        public ConnectionStringSettings GetConnectionStr()
        {
            ConnectionStringSettings cns = ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (cns == null)
                throw new Exception(string.Format(
                    "Connection string ({0}) is not found!", ConnectionStringName));
            return cns;
        }

        public DbProviderFactory GetDbFactory()
        {
            return DbProviderFactories.GetFactory(GetConnectionStr().ProviderName); ;
        }

        /// <summary>
        /// Create initialize and open DbConnection object using connection string defined in application config file.
        /// The connection name is defined the in *DacService.ConnectionStringName*.
        /// </summary>
        /// <returns></returns>
        public DbConnection GetConnection()
        {
            DbConnection db_conn = GetDbFactory().CreateConnection();

            string cs = GetConnectionStr().ConnectionString;
            cs = CollectionUtils.ReplaceParameters(cs, "$(,)", GetCfgPrmValue);
            db_conn.ConnectionString = cs;

            db_conn.Open();

            return db_conn;
        }

        public static string GetCfgPrmValue(string pPrmName)
        {
            string v = ConfigurationManager.AppSettings[pPrmName];
            if (string.IsNullOrEmpty(v)) return v;
            if (v.StartsWith("{") && v.EndsWith("}"))
                v = PasswordService.DecryptPassword(v);
            return v;
        }

        public DbParameter NewDbPrm(string pName, DbType pType, object pValue)
        {
            DbParameter prm = GetDbFactory().CreateParameter();
            prm.DbType = pType;
            prm.ParameterName = pName;
            prm.Value = pValue;
            return prm;
        }

        public DbParameter NewDbPrm(DbProviderFactory pFactory, string pName, DbType pType, object pValue)
        {
            DbParameter prm = pFactory.CreateParameter();
            prm.DbType = pType;
            prm.ParameterName = pName;
            prm.Value = pValue;
            return prm;
        }
    }
    */

}
