/*
 * CONFIG file components: CfgExtraAssemblies.
 * CfgExtraAssemblies is a class which is loading from specified section in App.Config file.
 * It is loading defined .NET assemblies into memory as part of this application.
 * Written by Dmitry Bond. at Dec 2011.
 *
 * Example of definitions in app.config:
 *   <configSections>
 * 	   <section name="Assemblies" type="XService.Configuration.CfgExtraAssemblies, XService.Net2" />
 *     [...]
 *   </configSections>
 *   [...]
 *   <Assemblies options="">
 *     <add file="MonitoringDM.Wmi.dll" />
 *   </Assemblies>
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using XService.Utils;

namespace XService.Configuration
{
    public class CfgExtraAssemblies : IConfigurationSectionHandler
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("ExtraAssemblies", "ExtraAssemblies");

        // interface: IConfigurationSectionHandler
        public object Create(object parent, object configContext, XmlNode section)
        {
            string savedCd = Directory.GetCurrentDirectory();
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("<CfgExtraAssemblies currentDir={0} ...>", savedCd) : "");

            bool isDirChanged = false;
            string asmId = string.Empty;
            List<Assembly> assemblies = new List<Assembly>();
            try
            {
                bool throwIfLoadFail = true;

                XmlNode attr = section.Attributes.GetNamedItem("options");
                if (attr != null)
                {
                    string[] items = attr.Value.Trim().Replace(',', ';').Replace(' ', ';').Split(';');
                    foreach (string item in items)
                    {
                        if (StrUtils.IsSameText(item, "IgnoreLoadError"))
                            throwIfLoadFail = false;
                    }
                }

                attr = section.Attributes.GetNamedItem("directory");
                if (attr != null)
                {
                    string s = attr.Value;
                    s = StrUtils.ReplaceCI(s, "$(HomePath)", TypeUtils.ApplicationHomePath);
                    s = StrUtils.ReplaceCI(s, "$(AppName)", TypeUtils.ApplicationName);
                    s = StrUtils.ReplaceCI(s, "$(AppVersion)", TypeUtils.ApplicationVersion);
                    Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("* CfgExtraAssemblies.SetDirectory: {0}", s) : "");
                    Directory.SetCurrentDirectory(s);
                    isDirChanged = true;
                }
                else
                {
                    string s = TypeUtils.ApplicationHomePath;
                    if (PathUtils.ExcludeTrailingSlash(savedCd).ToLower() != PathUtils.ExcludeTrailingSlash(s).ToLower())
                    {
                        Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("* CfgExtraAssemblies.SetDirectory(auto): {0}", s) : "");
                        Directory.SetCurrentDirectory(s);
                        isDirChanged = true;
                    }
                }

                foreach (XmlNode node in section.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;

                    if (string.Compare("assembly", node.Name, true) == 0 || string.Compare("add", node.Name, true) == 0)
                    {
                        Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("= Trying cfg node: {0}", node.OuterXml) : "");

                        string name = "", file = "";
                        attr = node.Attributes.GetNamedItem("name");
                        if (attr != null) name = attr.Value;

                        attr = node.Attributes.GetNamedItem("file");
                        if (attr != null) file = attr.Value;

                        Assembly asm = null;
                        if (!string.IsNullOrEmpty(name))
                        {
                            asmId = name;
                            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("+ Loading assembly by name: {0}", asmId) : "");
                            asm = LoadAssembly(asmId, false);
                        }
                        else
                        {
                            asmId = file;
                            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("+ Loading assembly by filename: {0}", asmId) : "");
                            asm = LoadAssembly(asmId, true);
                        }
                        Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(" * Assembly[{0}] - {1}", asmId, (asm == null ? "LOAD FAILURE!" : "successfully loaded.")) : "");
                        if (asm == null)
                        {
                            if (throwIfLoadFail)
                                throw new ToolConfigError(string.Format("Unable to load assembly ({0})", asmId));
                            continue;
                        }
                        assemblies.Add(asm);
                    }
                }
                checkForCustomInitializers(assemblies);
            }
            finally
            {
                if (isDirChanged)
                {
                    Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("* CfgExtraAssemblies.RestoreDirectory: {0}", savedCd) : "");
                    Directory.SetCurrentDirectory(savedCd);
                }
            }
            return assemblies;
        }

        internal void checkForCustomInitializers(List<Assembly> pList)
        {
            foreach (Assembly asm in pList)
            {
                string asmName = asm.GetName().Name;
                string typeId = "ModuleInitializer";
                Type t = asm.GetType(typeId, false, true);
                if (t == null)
                {
                    typeId = asmName + "." + typeId;
                    t = asm.GetType(typeId, false, true);
                }
                if (t == null)
                {
                    Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("Assembly[{0}] - no module initializers found!", asmName) : "");
                    continue;
                }

                ConstructorInfo ctor = t.GetConstructor(new Type[] { });
                if (ctor == null)
                {
                    Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("Assembly[{0}] - not found proper constructor for module initializer!", asmName) : "");
                    continue;
                }

                Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("Assembly[{0}] - calling module initializer...", asmName) : "");
                ctor.Invoke(new object[] { });
                Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format("Assembly[{0}] - module initialized.", asmName) : "");
            }
        }

        internal static Assembly LoadAssembly(string assemblyName, bool pLoadViaStream)
        {
            return TypeUtils.LoadAssembly(assemblyName, pLoadViaStream);
        }

    }
}
