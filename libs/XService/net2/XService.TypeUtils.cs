/*
 * Simple utilities to work with run-time data type information in .NET.
 * Written by Dmitry Bond. at Jun 14, 2006
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace XService.Utils
{
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

        /// <summary>
        /// Returns type by specified identifier.
        /// Type identifier should contain full name of type.
        /// Optionally it can contains name of assembly in which type if declared.		
        /// Type identifier format is:
        ///   FullTypeName [, AssemblyName]
        /// </summary>
        /// <param name="pTypeId">Type identifier. Example: "System.Diagnistics.MyTracer,Tracer"</param>
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
        /// Check if specified .NET assembly loaded or not.
        /// </summary>
        /// <param name="pAssemblyName">Assembly name or filename</param>
        /// <param name="pAssembly">out: will return Assembly object for the found assembly or null</param>
        /// <returns>Return true if specified .NET assembly is loaded.</returns>
        public static bool IsAssemblyLoaded(string pAssemblyName, out Assembly pAssembly)
        {
            pAssembly = null;
            string asmName = Path.GetFileNameWithoutExtension(pAssemblyName);
            if (asmName != null && asmName.Length > 0)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    AssemblyName an = assembly.GetName();
                    if (string.Compare(asmName, an.Name, true) == 0)
                    {
                        pAssembly = assembly;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Search specified type within all types in all loaded assemblies.
        /// </summary>
        /// <param name="pFullTypeName">Name of type to search</param>
        /// <returns>Returns an instance of found Type or null if not found</returns>
        public static Type FindType(string pFullTypeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly asm in assemblies)
            {
                Type[] types = asm.GetTypes();
                foreach (Type t in types)
                {
                    if (string.Compare(pFullTypeName, t.FullName, true) == 0)
                        return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Search specified assembly within all loaded assemblies.
        /// </summary>
        /// <param name="pAsmName">Name of assembly to search</param>
        /// <returns>Returns an instance of found Assembly or null if not found</returns>
        public static Assembly FindAssembly(string pAsmName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly asm in assemblies)
            {
                string name = asm.ManifestModule.Name;
                if (name.ToLower().EndsWith(".dll")) name = name.Remove(name.Length - 4, 4);
                if (string.Compare(pAsmName, name, true) == 0)
                    return asm;
            }
            return null;
        }

        private static Assembly _actualAssembly = null;
        /// <summary>Determing which Assembly should be used for getting path, name, version, etc. You can assign your Assembly object to this property</summary>
        public static Assembly ActualAssembly
        {
            get 
            {
                if (_actualAssembly == null)
                    _actualAssembly = Assembly.GetEntryAssembly();
                if (_actualAssembly == null)
                    _actualAssembly = Assembly.GetCallingAssembly();
                if (_actualAssembly == null)
                    _actualAssembly = Assembly.GetExecutingAssembly();
                return _actualAssembly;
            }
            set { _actualAssembly = value; }
        }

        /// <summary>
        /// Returns the path to direcory with main application executable 
        /// </summary>
        public static string ApplicationHomePath
        {
            get
            {
                Assembly asm = ActualAssembly;
                if (asm != null)
                {
                    return XService.Utils.IO.PathUtils.IncludeTrailingSlash(Path.GetDirectoryName(asm.Location));
                }
                return ConfigurationSettings.AppSettings["AppHomePath"];
            }
        }

        /// <summary>Return value specified in [AssemblyVersion] for EntryAssembly</summary>
        public static string ApplicationVersion
        {
            get
            {
                return ActualAssembly.GetName().Version.ToString();
            }
        }

        /// <summary>Return value specified in [AssemblyInformationalVersion] for EntryAssembly</summary>
        public static string InformationalVersion
        {
            get
            {
                Assembly asm = ActualAssembly;
                object[] attrs = asm.GetCustomAttributes(false);
                foreach (object attr in attrs)
                {
                    if (attr is AssemblyInformationalVersionAttribute)
                    {
                        AssemblyInformationalVersionAttribute a = (AssemblyInformationalVersionAttribute)attr;
                        return a.InformationalVersion;
                    }
                }
                return null;
            }
        }

        /// <summary>Return value specified in [AssemblyFileVersion] for EntryAssembly</summary>
        public static string ApplicationFileVersion
        {
            get
            {
                Assembly asm = ActualAssembly;
                object[] attrs = asm.GetCustomAttributes(false);
                foreach (object attr in attrs)
                {
                    if (attr is AssemblyFileVersionAttribute)
                    {
                        AssemblyFileVersionAttribute a = (AssemblyFileVersionAttribute)attr;
                        return a.Version;
                    }
                }
                return null;
            }
        }

        /// <summary>Return version string for XService.Net2 lib</summary>
        public static string XServiceLibVersionStr
        {
            get 
            { 
                Assembly asm = typeof(TypeUtils).Assembly;
                return GetVersionStrOf(asm); 
            }
        }


        /// <summary>Return value specified in [AssemblyVersion] for specified Assembly</summary>
        public static string GetVersionOf(Assembly pAsm)
        {
            return pAsm.GetName().Version.ToString();
        }

        /// <summary>Return value specified in [AssemblyFileVersion] for specified Assembly</summary>
        public static AssemblyFileVersionAttribute GetFileVersionOf(Assembly pAsm)
        {
            if (pAsm == null)
                pAsm = ActualAssembly;
            object[] attrs = pAsm.GetCustomAttributes(false);
            foreach (object attr in attrs)
            {
                if (attr is AssemblyFileVersionAttribute)
                {
                    AssemblyFileVersionAttribute a = (AssemblyFileVersionAttribute)attr;
                    return a;
                }
            }
            return null;
        }

        /// <summary>Returns string {AsmVersion}/{FileVersion} for specified Assembly</summary>
        public static string GetVersionStrOf(Assembly pAsm)
        {
            if (pAsm == null)
                pAsm = ActualAssembly;
            string info = GetVersionOf(pAsm);
            AssemblyFileVersionAttribute fv = GetFileVersionOf(pAsm);
            if (fv != null)
            {
                if (string.Compare(info, fv.Version, true) != 0)
                    info += ("/" + fv.Version);
            }
            return info;
        }

        /// <summary>Return full version string ApplicationVersion + "/" + ApplicationFileVersion</summary>
        public static string ApplicationVersionStr
        {
            get
            {
                string info = ApplicationVersion;
                string fileVer = ApplicationFileVersion;
                if (!string.IsNullOrEmpty(fileVer))
                {
                    if (string.Compare(info, fileVer, true) != 0)
                        info += ("/" + fileVer);
                }
                return info;
            }
        }

        /// <summary>
        /// Returns the path to direcory with calling assembly (XService)
        /// </summary>
        public static string AssemblyHomePath
        {
            get
            {
                return XService.Utils.IO.PathUtils.IncludeTrailingSlash(Path.GetDirectoryName(ActualAssembly.Location));
            }
        }

        /// <summary>Returns full path to application executable</summary>
        public static string ApplicationExecutableFullPath
        {
            get
            {
                Assembly asm = ActualAssembly;
                if (asm != null)
                {
                    return asm.Location;
                }
                return null;
            }
        }

        /// <summary>Returns name of application executable (without path and extension)</summary>
        public static string ApplicationName
        {
            get
            {
                Assembly asm = ActualAssembly;
                if (asm != null)
                {
                    string s = Path.ChangeExtension(Path.GetFileName(asm.Location), "");
                    s = s.Trim(" .".ToCharArray());
                    return s;
                }
                return ConfigurationSettings.AppSettings["AppName"];
            }
        }

        public static Assembly CurrentApplicationAssembly
        {
            get
            {
                Assembly asm = Assembly.GetEntryAssembly();
                if (asm == null)
                    asm = Assembly.GetCallingAssembly();
                return asm;
            }
        }

        /// <summary>Compare version numbers (expected to be in N1.N2.N3.N4 format). '*' wildcard is supported for any of N1..N4 fields for both version values</summary>
        /// <returns>Returns -1 when Version1 is less than Version2, +1 when Version1 is higher than Version2, 0 when both are equal</returns>
        public static int CompareVersions(string pVersion1, string pVersion2)
        {
            string[] items1 = pVersion1.Replace(',', '.').Split('.');
            string[] items2 = pVersion2.Replace(',', '.').Split('.');
            for (int i = 0; i < items1.Length; i++)
            { 
                string s1 = items1[i];
                string s2 = (i < items2.Length ? items2[i] : "-1");
                if (s1 == "*" || s2 == "*") continue;

                int n1, n2;
                // wrong numeric field in pVersion1 means - it is less than pVersion2
                if (!StrUtils.GetAsInt(s1, out n1)) return -1;

                // wrong numeric field in pVersion2 means - it is less than pVersion1 (so, Version1 is higher)
                if (!StrUtils.GetAsInt(s2, out n2)) return 1;

                if (n1 < n2)
                    return -1;
                if (n1 > n2)
                    return 1;
            }
            return 0;
        }

        /// <summary>
        /// Returns values of all version-info Assembly attributes for specified Assembly.
        /// </summary>
        /// <param name="pProps">Target dictionary object to return values to</param>
        /// <param name="pAsm">Assembly to scan for version-info attributes</param>
        /// <returns>Number of newly added items to a dictionary</returns>
        public static int CollectVersionInfoAttributes(Dictionary<string, string> pProps, Assembly pAsm)
        {
            pProps["imageruntimeversion"] = pAsm.ImageRuntimeVersion;
            pProps["codebase"] = pAsm.CodeBase;
            pProps["fullname"] = pAsm.FullName;
            pProps["location"] = pAsm.Location;
            int counter = pProps.Count;
            object[] attrs = pAsm.GetCustomAttributes(false);
            foreach (object attr in attrs)
            {
                if (attr is AssemblyVersionAttribute)
                {
                    AssemblyVersionAttribute a = (AssemblyVersionAttribute)attr;
                    pProps["version"] = a.Version;
                }
                if (attr is AssemblyFileVersionAttribute)
                {
                    AssemblyFileVersionAttribute a = (AssemblyFileVersionAttribute)attr;
                    pProps["fileversion"] = a.Version;
                }
                if (attr is AssemblyInformationalVersionAttribute)
                {
                    AssemblyInformationalVersionAttribute a = (AssemblyInformationalVersionAttribute)attr;
                    pProps["productversion"] = a.InformationalVersion;
                }
                else if (attr is AssemblyTitleAttribute)
                {
                    AssemblyTitleAttribute a = (AssemblyTitleAttribute)attr;
                    pProps["title"] = a.Title;
                }
                else if (attr is AssemblyDescriptionAttribute)
                {
                    AssemblyDescriptionAttribute a = (AssemblyDescriptionAttribute)attr;
                    pProps["description"] = a.Description;
                }
                else if (attr is AssemblyConfigurationAttribute)
                {
                    AssemblyConfigurationAttribute a = (AssemblyConfigurationAttribute)attr;
                    pProps["configuration"] = a.Configuration;
                }
                else if (attr is AssemblyCompanyAttribute)
                {
                    AssemblyCompanyAttribute a = (AssemblyCompanyAttribute)attr;
                    pProps["company"] = a.Company;
                }
                else if (attr is AssemblyProductAttribute)
                {
                    AssemblyProductAttribute a = (AssemblyProductAttribute)attr;
                    pProps["product"] = a.Product;
                }
                else if (attr is AssemblyCopyrightAttribute)
                {
                    AssemblyCopyrightAttribute a = (AssemblyCopyrightAttribute)attr;
                    pProps["copyright"] = a.Copyright;
                }
                else if (attr is AssemblyTrademarkAttribute)
                {
                    AssemblyTrademarkAttribute a = (AssemblyTrademarkAttribute)attr;
                    pProps["trademark"] = a.Trademark;
                }
                else if (attr is AssemblyCultureAttribute)
                {
                    AssemblyCultureAttribute a = (AssemblyCultureAttribute)attr;
                    pProps["culture"] = a.Culture;
                }
            }
            if (!pProps.ContainsKey("version"))
            {
                AssemblyName nm = pAsm.GetName();
                pProps["version"] = nm.Version.ToString();
            }
            return (pProps.Count - counter);
        }

        public static Exception AssemblyLoadingError = null;

        /// <summary>
        /// LoadAssembly
        /// Try to load a .NET assembly with specified name.
        /// The name could be a filename or name of assembly.
        /// </summary>
        /// <param name="pAssemblyName"></param>
        /// <param name="pTryLoadViaStream">If true then it will try to read a {pAssemblyName} file and try to load assembly from a stream. Anyway it will do rest of attempts to load assembly.</param>
        /// <returns></returns>
        public static Assembly LoadAssembly(string pAssemblyName, bool pTryLoadViaStream)
        {
            AssemblyLoadingError = null;
            Assembly result = null;

            if (pTryLoadViaStream)
            {
                if (File.Exists(pAssemblyName))
                {
                    try
                    {
                        Trace.WriteLineIf(CommonUtils.TrcLvl.TraceVerbose, CommonUtils.TrcLvl.TraceVerbose ? string.Format(
                            "LoadAssembly.FromFile->Loading file( {0} )...", pAssemblyName) : "");
                        using (FileStream strm = File.OpenRead(pAssemblyName))
                        {
                            byte[] data = new byte[strm.Length];
                            strm.Read(data, 0, (int)strm.Length);
                            result = Assembly.Load(data);
                        }
                        if (result != null)
                        {
                            Trace.WriteLineIf(CommonUtils.TrcLvl.TraceInfo, CommonUtils.TrcLvl.TraceInfo ? string.Format(
                                "LoadAssembly.FromFile->SUCCESS: Assembly ({0}) was loaded from raw stream!", pAssemblyName) : "");
                            return result;
                        }
                    }
                    catch (Exception e0)
                    {
                        AssemblyLoadingError = e0;
                        Trace.WriteLineIf(CommonUtils.TrcLvl.TraceWarning, CommonUtils.TrcLvl.TraceWarning ? string.Format(
                            "LoadAssembly.FromFile->FAILED: {0}", ErrorUtils.FormatErrorMsg(e0)) : "");
                    }
                }
                else
                    Trace.WriteLineIf(CommonUtils.TrcLvl.TraceWarning, CommonUtils.TrcLvl.TraceWarning ? string.Format(
                        "LoadAssembly.ERROR: unable to find file ({0})!", pAssemblyName) : "");
            }

            // try to load assembly via AppDomain
            if (result == null)
            {
                try
                {
                    Trace.WriteLineIf(CommonUtils.TrcLvl.TraceVerbose, CommonUtils.TrcLvl.TraceVerbose ? string.Format(
                        "LoadAssembly.LoadingViaApDomain( {0} )...", pAssemblyName) : "");
                    result = AppDomain.CurrentDomain.Load(pAssemblyName);
                    if (result != null)
                    {
                        Trace.WriteLineIf(CommonUtils.TrcLvl.TraceInfo, CommonUtils.TrcLvl.TraceInfo ? string.Format(
                            "LoadAssembly.DomainLoad->SUCCESS: {0} is loaded by Domain", pAssemblyName) : "");
                        return result;
                    }
                }
                catch (Exception e1)
                {
                    AssemblyLoadingError = e1;
                    Trace.WriteLineIf(CommonUtils.TrcLvl.TraceWarning, CommonUtils.TrcLvl.TraceWarning ? string.Format(
                        "LoadAssembly.DomainLoad->FAILED: {0}", ErrorUtils.FormatErrorMsg(e1)) : "");
                }
            }

            // try to load assembly 
            if (result == null)
            {
                try
                {
                    Trace.WriteLineIf(CommonUtils.TrcLvl.TraceVerbose, CommonUtils.TrcLvl.TraceVerbose ? string.Format(
                        "LoadAssembly.ExplicitLoading( {0} )...", pAssemblyName) : "");
                    result = Assembly.Load(pAssemblyName);
                    if (result != null)
                    {
                        Trace.WriteLineIf(CommonUtils.TrcLvl.TraceInfo, CommonUtils.TrcLvl.TraceInfo ? string.Format(
                            "LoadAssembly.Load->SUCCESS: {0} was loaded by Assembly.Load", pAssemblyName) : "");
                        return result;
                    }
                }
                catch (Exception e2)
                {
                    AssemblyLoadingError = e2;
                    Trace.WriteLineIf(CommonUtils.TrcLvl.TraceWarning, CommonUtils.TrcLvl.TraceWarning ? string.Format(
                        "LoadAssembly.Load->FAILED: {0}", ErrorUtils.FormatErrorMsg(e2)) : "");
                    try
                    {
                        result = Assembly.LoadFrom(pAssemblyName);
                        if (result != null)
                        {
                            Trace.WriteLineIf(CommonUtils.TrcLvl.TraceInfo, CommonUtils.TrcLvl.TraceInfo ? string.Format(
                                "LoadAssembly.LoadFrom->SUCCESS: {0} was loaded by Assembly.LoadFrom", pAssemblyName) : "");
                            return result;
                        }
                    }
                    catch (Exception e3)
                    {
                        AssemblyLoadingError = e3;
                        Trace.WriteLineIf(CommonUtils.TrcLvl.TraceWarning, CommonUtils.TrcLvl.TraceWarning ? string.Format(
                            "LoadAssembly.LoadFrom->FAILED: {0}", ErrorUtils.FormatErrorMsg(e3)) : "");
                    }
                }
            }

            // try to load assembly with partial name
            if (result == null)
            {
                try
                {
                    Trace.WriteLineIf(CommonUtils.TrcLvl.TraceVerbose, CommonUtils.TrcLvl.TraceVerbose ? string.Format(
                        "LoadAssembly.LoadingWithPartialName( {0} )...", pAssemblyName) : "");
                    result = Assembly.LoadWithPartialName(pAssemblyName);
                    if (result != null)
                    {
                        Trace.WriteLineIf(CommonUtils.TrcLvl.TraceInfo, CommonUtils.TrcLvl.TraceInfo ? string.Format(
                            "LoadAssembly.LoadWithPartialName->SUCCESS: {0} was loaded by Assembly.LoadWithPartialName", pAssemblyName) : "");
                        return result;
                    }
                }
                catch (Exception e4)
                {
                    AssemblyLoadingError = e4;
                    Trace.WriteLineIf(CommonUtils.TrcLvl.TraceWarning, CommonUtils.TrcLvl.TraceWarning ? string.Format(
                        "LoadAssembly.LoadWithPartialName->FAILED: {0}", ErrorUtils.FormatErrorMsg(e4)) : "");
                }
            }
            return null;
        }

        /// <summary>Extact Version Info from specified Assembly</summary>
        /// <param name="pAsm">Assembly to extract version info from</param>
        /// <param name="pInfo">Output. Version Info for specified Assembly</param>
        /// <returns>Returns true when extracted Version Info block is valid (so, it should contains at least Company, Copyright, Version, FileVersion attributes)</returns>
        public static bool ExtractVersionInfo(Assembly pAsm, out VersionInfoBlock pInfo)
        {
            pInfo = new VersionInfoBlock();

            object[] attrs = pAsm.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Title = (AssemblyTitleAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Description = (AssemblyDescriptionAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Configuration = (AssemblyConfigurationAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Company = (AssemblyCompanyAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Product = (AssemblyProductAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Copyright = (AssemblyCopyrightAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyTrademarkAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Trademark = (AssemblyTrademarkAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyCultureAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.Culture = (AssemblyCultureAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(GuidAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.GUID = (GuidAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyVersionAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.VersionAttr = (AssemblyVersionAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.InformationalVersionAttr = (AssemblyInformationalVersionAttribute)attrs[0];

            attrs = pAsm.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);
            if (attrs != null && attrs.Length > 0)
                pInfo.FileVersionAttr = (AssemblyFileVersionAttribute)attrs[0];

            pInfo.Version = pAsm.GetName().Version;
            pInfo.FileVersion = FileVersionInfo.GetVersionInfo(pAsm.Location);

            return pInfo.IsValid;
        }

    } /* TypeUtils */
     

    /// <summary>
    /// Dispatcher interface to work with .NET object using dynamic binding via reflection
    /// </summary>
    public class TypeDispatcher
    {
        public static bool CaseSensitive = true; 

        public TypeDispatcher(Type pBaseType)
        {
            this.baseType = pBaseType;
            loadObjectInfo();
        }

        public TypeDispatcher(Type pBaseType, BindingFlags pBindingFlags)
        {
            this.baseType = pBaseType;

            // initialize default BindingFlags
            this.bindFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
                | BindingFlags.GetProperty | BindingFlags.SetProperty
                | BindingFlags.GetField | BindingFlags.SetField;

            // append specified BindingFlags to default
            this.bindFlags |= pBindingFlags;
            loadObjectInfo();
        }

        public TypeDispatcher(string pTypeName, Assembly pAsm)
        {
            Type[] types = pAsm.GetTypes();
            foreach (Type t in types)
            {
                bool isMatch = (CaseSensitive ? StrUtils.IsSameStr(t.Name, pTypeName) : StrUtils.IsSameText(t.Name, pTypeName));
                if (isMatch)
                {
                    baseType = t;
                    break;
                }
            }
            if (this.baseType == null)
                throw new Exception(string.Format("Cannot find ({0}) type in ({1}) assembly", pTypeName, pAsm.GetName().ToString()));

            loadObjectInfo();
        }

        public Type BaseType { get { return this.baseType; } }

        public object Instance
        {
            get { return this.instance; }
            set { this.instance = value; }
        }

        public void New(params object[] pParams)
        {
            this.instance = Activator.CreateInstance(this.baseType, pParams);
        }

        public object CallMethod(string pMethodName, params object[] pParams)
        {
            MemberInfo mbr;
            if (this.methods.TryGetValue(pMethodName, out mbr))
            {
                MethodInfo mi = (MethodInfo)mbr;
                if (mi.IsStatic)
                    return mi.Invoke(null, pParams);
                else
                    return mi.Invoke(this.instance, pParams);
            }
            else
                throw new Exception(string.Format("Method ({0}) is not found in ({1})", pMethodName, this.baseType));
        }

        public object GetValue(string pName)
        {
            MemberInfo mbr;
            if (this.props.TryGetValue(pName, out mbr))
            {
                PropertyInfo fld = (PropertyInfo)mbr;
                return fld.GetValue(this.instance, null);
            }
            else if (this.fields.TryGetValue(pName, out mbr))
            {
                FieldInfo fld = (FieldInfo)mbr;
                return fld.GetValue(this.instance);
            }
            else
                throw new Exception(string.Format("Property or field ({0}) is not found in ({1})", pName, this.baseType));
        }

        public void SetValue(string pName, object pValue)
        {
            MemberInfo mbr;
            if (this.props.TryGetValue(pName, out mbr))
            {
                PropertyInfo fld = (PropertyInfo)mbr;
                fld.SetValue(this.instance, pValue, null);
            }
            else if (this.fields.TryGetValue(pName, out mbr))
            {
                // Info: http://technico.qnownow.com/how-to-set-property-value-using-reflection-in-c/
                FieldInfo fld = (FieldInfo)mbr;
                fld.SetValue(this.instance, pValue);
            }
            else
                throw new Exception(string.Format("Property or field ({0}) is not found in ({1})", pName, this.baseType));
        }

        #region Implementation details

        private void loadObjectInfo()
        {
            MemberInfo[] members = null;
            if (this.bindFlags != BindingFlags.Default)
                members = this.baseType.GetMembers(this.bindFlags);
            else
                members = this.baseType.GetMembers();
            foreach (MemberInfo mbr in members)
            {
                if (mbr.MemberType == MemberTypes.Constructor)
                    this.fields[mbr.Name] = mbr;
                else if (mbr.MemberType == MemberTypes.Field)
                    this.fields[mbr.Name] = mbr;
                else if (mbr.MemberType == MemberTypes.Property)
                    this.props[mbr.Name] = mbr;
                else if (mbr.MemberType == MemberTypes.Method)
                    this.methods[mbr.Name] = mbr;
            }
        }

        private Type baseType = null;
        private BindingFlags bindFlags = BindingFlags.Default;
        private object instance = null;
        private Dictionary<string, MemberInfo> constructors = new Dictionary<string, MemberInfo>();
        private Dictionary<string, MemberInfo> props = new Dictionary<string, MemberInfo>();
        private Dictionary<string, MemberInfo> fields = new Dictionary<string, MemberInfo>();
        private Dictionary<string, MemberInfo> methods = new Dictionary<string, MemberInfo>();

        #endregion // Implementation details
    }

    public class VersionInfoBlock
    { 
        public AssemblyTitleAttribute Title;
        public AssemblyDescriptionAttribute Description;
        public AssemblyConfigurationAttribute Configuration;
        public AssemblyCompanyAttribute Company;
        public AssemblyProductAttribute Product;
        public AssemblyCopyrightAttribute Copyright;
        public AssemblyTrademarkAttribute Trademark;
        public AssemblyCultureAttribute Culture;
        public GuidAttribute GUID;
        public AssemblyVersionAttribute VersionAttr;
        public Version Version;
        public AssemblyFileVersionAttribute FileVersionAttr;
        public AssemblyInformationalVersionAttribute InformationalVersionAttr;
        public FileVersionInfo FileVersion;

        public bool IsValid
        {
            get { return (this.Company != null && this.Copyright != null && (this.Version != null && this.FileVersion != null)); }
        }
    }

    /// <summary>Special class to enable store custom version info in Assembly</summary>
    [AttributeUsage(AttributeTargets.Assembly,  AllowMultiple = true)]
    public class CustomVersionInfoAttribute : Attribute 
    {
        public string AttrName { get; protected set; }
        
        public string AttrValue { get; protected set; }

        public CustomVersionInfoAttribute () : this("", "") { }
        public CustomVersionInfoAttribute (string pName, string pValue) { this.AttrName = pName; this.AttrValue = pValue; }
    }

}
