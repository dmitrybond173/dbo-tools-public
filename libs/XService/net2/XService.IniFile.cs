/*
 * .NET implementation of INI file component.
 * Written by Dmitry Bond. at Feb 12, 2012
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using XService.Utils;

namespace PAL
{
    /// <summary>
    /// The IniFile is standalone, platform-independent engine handling INI files with comments.
    /// </summary>
    public class IniFile
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("IniFile", "IniFile");

        protected const char INI_PATH_DELIMITER = '\\';

        /// <summary>
        /// Create a IniFile object. 
        /// INI file content is not loaded. Use the Reload() method to load it.
        /// </summary>
        /// <param name="pFilename">Filename of INI file to use</param>
        public IniFile(string pFilename)
        {
            this.FileEncoding = Encoding.Default;
            this.fileinfo = new FileInfo(pFilename);
            this.ForceFixNames = true;
            this.AutoFlush = false;

            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "+ {0}.ctor(fn={1}): /enc={2}; fix={3}; flush={4}", 
                CommonUtils.ObjectToStr(this),
                pFilename, this.FileEncoding, this.ForceFixNames, this.AutoFlush) : "");
        }

        /// <summary>
        /// Create a IniFile object. 
        /// INI file content is not loaded. Use the Reload() method to load it.
        /// </summary>
        /// <param name="pFilename">Filename of INI file to use</param>
        /// <param name="pEncoding">Text encoding of INI file</param>
        public IniFile(string pFilename, Encoding pEncoding)
        {
            this.FileEncoding = pEncoding;
            this.fileinfo = new FileInfo(pFilename);
            this.ForceFixNames = true;
            this.AutoFlush = false;

            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "+ {0}.ctor(fn={1}; enc={2}): fix={3}; flush={4}",
                CommonUtils.ObjectToStr(this),
                pFilename, this.FileEncoding, this.ForceFixNames, this.AutoFlush) : "");
        }

        #region Public Interface

        /// <summary>Force reload an INI file content</summary>
        public void Reload()
        {
            this.sections.Clear();
            this.lines.Clear();
            loadIniFile();
        }

        /// <summary>During load operation it will force upper-case all names of sections and items</summary>
        public bool ForceFixNames { get; set; }

        /// <summary>Set if it should automatically flush all changes made by WriteXxx() methods</summary>
        public bool AutoFlush { get; set; }

        /// <summary>FileInfo object describing source INI file</summary>
        public bool IsWritable
        {
            get { return this.isWritable; }
            set { this.isWritable = value; }
        }

        /// <summary>Encoding object describing text encoding of INI file</summary>
        public Encoding FileEncoding { get; set; }

        /// <summary>FileInfo object describing source INI file</summary>
        public FileInfo SourceFileInfo { get { return this.fileinfo; } }

        /// <summary>Accessing specified item in INI file.</summary>
        /// <param name="pSection">Section name to address</param>
        /// <param name="pKey">Item name to address</param>
        /// <returns>Value of specified Item in specified Section or null string if not found</returns>
        public string this[string pSection, string pKey]
        {
            get { return ReadString(pSection, pKey, null); }
            set { WriteString(pSection, pKey, value); }
        }

        /// <summary>
        /// Check if specified section is exists in loaded INI file content.
        /// </summary>
        /// <param name="pSectionName">Section name to check</param>
        /// <returns></returns>
        public bool IsSectionExists(string pSectionName)
        {
            if (ForceFixNames)
                pSectionName = pSectionName.ToUpper();
            return this.sections.ContainsKey(pSectionName);
        }

        /// <summary>
        /// Check if specified Item is exists in specified Section in loaded INI file content.
        /// </summary>
        /// <param name="pSectionName">Section name to check</param>
        /// <param name="pValueName">Item name to check</param>
        /// <returns>Returns true if specified section exists in INI file</returns>
        public bool IsValueExists(string pSectionName, string pValueName)
        {
            if (ForceFixNames)
                pSectionName = pSectionName.ToUpper();
            IniSection section;
            if (!this.sections.TryGetValue(pSectionName, out section))
                return false;
            return section.Items.ContainsKey(pValueName.ToUpper());
        }

        /// <summary>
        /// Populate specified list-object with the list of all Sections in a loaded INI file content.
        /// </summary>
        /// <param name="pTargetList">list-object to populate</param>
        /// <returns>Returns a number of newly added items to a list-object</returns>
        public int ReadSections(List<string> pTargetList)
        {
            EnsureLoaded();

            int savedCount = pTargetList.Count;
            foreach (KeyValuePair<string, IniSection> item in this.sections)
            {
                pTargetList.Add(item.Key);
            }
            return pTargetList.Count - savedCount;
        }

        /// <summary>
        /// Populate specified list-object with the list of all Items in specified Section in a loaded INI file content.
        /// </summary>
        /// <param name="pSectionName">Section to scan for names</param>
        /// <param name="pTargetList">list-object to populate</param>
        /// <returns>Returns a number of newly added items to a list-object</returns>
        public int ReadSectionNames(string pSectionName, List<string> pTargetList)
        {
            EnsureLoaded();

            int savedCount = pTargetList.Count;
            if (ForceFixNames)
                pSectionName = pSectionName.ToUpper();
            if (this.sections.ContainsKey(pSectionName))
            {
                IniSection section = this.sections[pSectionName];
                foreach (KeyValuePair<string, IniValue> item in section.Items)
                {
                    string key = item.Key;
                    if (ForceFixNames)
                        key = key.ToUpper();
                    pTargetList.Add(key);
                }
            }
            return pTargetList.Count - savedCount;
        }

        /// <summary>
        /// Populate specified dictionary object with name+value pairs from specified Section in a loaded INI file content.
        /// </summary>
        /// <param name="pSectionName">Section to scan for values</param>
        /// <param name="pTargetDictionary">dictionary object to populate with data</param>
        /// <returns>Number of newly added items to a dictionary object</returns>
        public int ReadSection(string pSectionName, Dictionary<string, string> pTargetDictionary)
        {
            EnsureLoaded();

            int savedCount = pTargetDictionary.Count;
            if (ForceFixNames)
                pSectionName = pSectionName.ToUpper();
            if (this.sections.ContainsKey(pSectionName))
            {
                IniSection section = this.sections[pSectionName];
                foreach (KeyValuePair<string, IniValue> item in section.Items)
                {
                    string key = item.Key;
                    if (ForceFixNames)
                        key = key.ToUpper();
                    pTargetDictionary[key] = item.Value.Value;
                }
            }
            return pTargetDictionary.Count - savedCount;
        }

        /// <summary>
        /// Reads a value of specified Item in specified Section from a loaded INI file content.
        /// </summary>
        /// <param name="pSection">Section to read</param>
        /// <param name="pKey">INI item to read</param>
        /// <param name="pDefaultValue">Default value to return if INI item or Section is not found</param>
        /// <returns>Value of specified Item in specified Section of default value</returns>
        public string ReadString(string pSection, string pKey, string pDefaultValue)
        {
            EnsureLoaded();

            if (ForceFixNames)
                pSection = pSection.ToUpper();
            if (this.sections.ContainsKey(pSection))
            {
                IniSection section = this.sections[pSection];
                IniValue item;
                if (section.Items.TryGetValue(pKey.ToUpper(), out item))
                    return item.Value;
            }
            return pDefaultValue;
        }

        /// <summary>Reads a comment for specified Section or Item in section.</summary>
        /// <param name="pSection">Section to read comment for</param>
        /// <param name="pKey">INI item to read comment for, when null it will read comment only for section</param>
        /// <returns>Value of specified Item in specified Section of default value</returns>
        public string ReadComment(string pSection, string pKey)
        {
            EnsureLoaded();

            if (ForceFixNames)
                pSection = pSection.ToUpper();
            if (this.sections.ContainsKey(pSection))
            {
                IniSection section = this.sections[pSection];
                if (string.IsNullOrEmpty(pKey))
                    return section.Comment;

                IniValue item;
                if (section.Items.TryGetValue(pKey.ToUpper(), out item))
                    return item.Comment;
            }
            return null;
        }

        /// <summary>Change value of specified item in INI file</summary>
        /// <param name="pSectionName">Section name</param>
        /// <param name="pValueName">INI item name</param>
        /// <param name="pValue">New value to write to specified INI item</param>
        public virtual void WriteString(string pSectionName, string pValueName, string pValue)
        {
            if (!this.isWritable)
                throw new XServiceError(string.Format("IniFile({0}) is read-only!", this.fileinfo.FullName));

            IniSection section;
            if (!this.sections.TryGetValue(pSectionName.ToUpper(), out section))
            {
                if (this.lines.Count > 0 && this.lines[this.lines.Count - 1] != "")
                    this.lines.Add(""); // need to add empty line before a new section!
                section = new IniSection(this, pSectionName, this.lines.Count);
                this.sections.Add(pSectionName.ToUpper(), section);
                this.lines.Add(string.Format("[{0}]", pSectionName));
            }

            IniValue item;
            if (!section.Items.TryGetValue(pValueName.ToUpper(), out item))
            {
                int lineNo = section.MaxLineNo + 1;
                if (lineNo < (this.lines.Count - 1))
                    shiftLines(lineNo, 1);
                else
                {
                    while (lineNo > (this.lines.Count - 1))
                        this.lines.Add("");
                }
                item = new IniValue(section, pValueName, pValue, lineNo);
                section.Items[pValueName.ToUpper()] = item;
            }
            else
                item.Value = pValue;

            this.lines[item.LineNo] = string.Format("{0}={1}", item.Name, item.Value);

            if (this.AutoFlush)
                Save();
        }

        /// <summary>Remove whole section from INI file</summary>
        /// <param name="pSectionName">Section name</param>
        public virtual void DeleteSection(string pSectionName)
        {
            if (!this.isWritable)
                throw new XServiceError(string.Format("IniFile({0}) is read-only!", this.fileinfo.FullName));

            IniSection section;
            if (!this.sections.TryGetValue(pSectionName.ToUpper(), out section))
                return;

            int diff = section.MaxLineNo - section.LineNo;
            for (int iLine = section.LineNo; iLine <= section.MaxLineNo; iLine++)
            {
                this.lines.RemoveAt(section.LineNo);
            }
            if (section.LineNo < this.lines.Count && this.lines[section.LineNo].Trim() == "")
                this.lines.RemoveAt(section.LineNo);
            shiftLines(section.LineNo, -diff);

            this.sections.Remove(pSectionName.ToUpper());

            if (this.AutoFlush)
                Save();
        }

        /// <summary>Delete specified INI item from INI file</summary>
        /// <param name="pSectionName">Section name</param>
        /// <param name="pValueName">INI item name</param>
        public virtual void DeleteItem(string pSectionName, string pValueName)
        {
            if (!this.isWritable)
                throw new XServiceError(string.Format("IniFile({0}) is read-only!", this.fileinfo.FullName));

            IniSection section;
            if (!this.sections.TryGetValue(pSectionName.ToUpper(), out section))
                return;

            IniValue item;
            if (!section.Items.TryGetValue(pValueName.ToUpper(), out item))
                return;

            this.lines.RemoveAt(item.LineNo);
            shiftLines(item.LineNo, -1);

            if (this.AutoFlush)
                Save();
        }

        /// <summary>Delegate to be called on saving INI file</summary>
        public delegate void OnSavingMethod(IniFile pSender);

        /// <summary>
        /// Called when saving INI file
        /// </summary>
        public OnSavingMethod OnSaving { get; set; }

        /// <summary>
        /// Save changes to INI file
        /// </summary>
        public void Save()
        {
            if (!this.isWritable)
                throw new XServiceError(string.Format("IniFile({0}) is read-only!", this.fileinfo.FullName));

            if (this.OnSaving != null)
                this.OnSaving(this);

            using (FileStream strm = this.fileinfo.OpenWrite())
            {
                string text = "";
                foreach (string line in this.lines)
                    text += (line + Environment.NewLine);

                byte[] data = this.FileEncoding.GetBytes(text);
                strm.Seek(0, SeekOrigin.Begin);
                strm.Write(data, 0, data.Length);
                strm.SetLength(strm.Position);
                strm.Close();
            }
        }

        /// <summary>
        /// Save INI file into file with different name
        /// </summary>
        /// <param name="pFilename"></param>
        public void SaveAs(string pFilename)
        {
            FileInfo fi = new FileInfo(pFilename);
            if (!fi.Exists)
                fi.Create();
            this.fileinfo = fi;
            Save();
        }

        /// <summary>
        /// Ensure content of INI file loaded.
        /// </summary>
        protected void EnsureLoaded()
        {
            if (this.isLoaded) return;

            this.isLoaded = true;
            Reload();
        }

        #endregion // Public Interface

        #region Implementation details

        /// <summary>Shift line# ref for all sections and lines by specified delta</summary>
        protected void shiftLines(int pLineNo, int pDelta)
        {
            foreach (KeyValuePair<string, IniSection> section in this.sections)
            {
                if (section.Value.LineNo >= pLineNo)
                    section.Value.LineNo += pDelta;
                foreach (KeyValuePair<string, IniValue> item in section.Value.Items)
                {
                    if (item.Value.LineNo >= pLineNo)
                        item.Value.LineNo += pDelta;
                }
            }
        }

        /// <summary>Load and parse INI file, build data structures for it</summary>
        protected virtual void loadIniFile()
        {
            this.fileinfo.Refresh();

            if (!this.fileinfo.Exists)
            {
                Trace.WriteLineIf(TrcLvl.TraceError, TrcLvl.TraceError ? string.Format(
                    "* {0}.loadIniFile: file not found [{1}]",
                    CommonUtils.ObjectToStr(this), this.fileinfo.FullName) : "");
                return;
            }

            IniSection section = null;
            string lastComment = null;
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "* {0}.loadIniFile: opening: [{1}]; sz={2}",
                CommonUtils.ObjectToStr(this), this.fileinfo.FullName, this.fileinfo.Length) : "");
            using (StreamReader sr = this.fileinfo.OpenText())
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim(StrUtils.STR_SPACES.ToCharArray());
                    if (string.IsNullOrEmpty(line)) { this.lines.Add(line); continue; }

                    if (line.StartsWith("#") || line.StartsWith(";")) 
                    {
                        if (lastComment == null)
                            lastComment = line;
                        else
                            lastComment += (Environment.NewLine + line);
                        this.lines.Add(line); 
                        continue; 
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        string sectionName = line.Remove(line.Length - 1, 1).Remove(0, 1).Trim(StrUtils.STR_SPACES.ToCharArray());
                        section = new IniSection(this, sectionName, this.lines.Count);
                        section.Comment = lastComment;
                        lastComment = null;
                        this.sections[sectionName.ToUpper()] = section;
                        this.lines.Add(line);
                        continue;
                    }

                    if (section == null)
                    {
                        section = new IniSection(this, "", this.lines.Count);
                        this.sections[""] = section;
                    }

                    int p = line.IndexOf('=');
                    if (p < 0) { this.lines.Add(line); continue; }

                    string name = line.Substring(0, p).Trim(StrUtils.STR_SPACES.ToCharArray());
                    string value = line.Remove(0, p + 1).Trim(StrUtils.STR_SPACES.ToCharArray());

                    IniValue item = new IniValue(section, name, value, this.lines.Count); ;
                    item.Comment = lastComment;
                    lastComment = null;
                    section.Items[name.ToUpper()] = item;
                    this.lines.Add(line);
                }
            }

            if (lines.Count > 0)
            {
                // cut off empty lines at the end
                while (this.lines[lines.Count - 1].Trim(StrUtils.CH_SPACES) == "")
                    this.lines.RemoveAt(lines.Count - 1);
            }
        }

        /// <summary>If INI file was loaded</summary>
        protected bool isLoaded = false;
        /// <summary>If INI file is writable</summary>
        protected bool isWritable = true;
        /// <summary>FileInfo object for INI file</summary>
        protected FileInfo fileinfo = null;
        /// <summary>all lines of loaded INI file</summary>
        protected List<string> lines = new List<string>();

        /// <summary>Data structure which store data items from INI file</summary>
        protected Dictionary<string, IniSection> sections = new Dictionary<string, IniSection>();


        /// <summary>IniSection - holder of INI section</summary>
        protected class IniSection
        {
            /// <summary>Construct IniSection object</summary>
            public IniSection(IniFile pOwner, string pName, int pLineNo)
            {
                this.Name = pName;
                this.Owner = pOwner;
                this.LineNo = pLineNo;
                this.Items = new Dictionary<string, IniValue>();
            }

            /// <summary>Ref to Owner object</summary>
            public IniFile Owner { get; protected set; }

            /// <summary>Name of section</summary>
            public string Name { get; protected set; }

            /// <summary>Data items in section</summary>
            public Dictionary<string, IniValue> Items { get; protected set; }

            /// <summary>Line# where this section starts in INI file</summary>
            public int LineNo { get; set; }

            /// <summary>Comment text for section</summary>
            public string Comment { get; set; }

            /// <summary>Max line# for INI items in section</summary>
            public int MaxLineNo
            {
                get
                {
                    int line = this.LineNo; // +1; // 1 line is reserved for section name
                    foreach (KeyValuePair<string, IniValue> item in this.Items)
                    {
                        if (line < item.Value.LineNo)
                            line = item.Value.LineNo;
                    }
                    return line;
                }
            }
        }

        /// <summary>IniValue - holder of INI item</summary>
        protected class IniValue
        {
            /// <summary>Construct IniSection object</summary>
            public IniValue(IniSection pSection, string pName, string pValue, int pLineNo)
            {
                this.Name = pName;
                this.Section = pSection;
                this.LineNo = pLineNo;
                this.Value = pValue;
            }

            /// <summary>Ref to Owner section object</summary>
            public IniSection Section { get; protected set; }

            /// <summary>Name of INI item</summary>
            public string Name { get; protected set; }

            /// <summary>Value of INI item</summary>
            public string Value { get; set; }

            /// <summary>Line# where this section starts in INI file</summary>
            public int LineNo { get; set; }

            /// <summary>Comment for INI item</summary>
            public string Comment { get; set; }
        }

        #endregion // Implementation details
    }


    /// <summary>
    /// The ReadonlyInifile is descendant of IniFile class which does not allow changing its content, 
    /// even if nternal flag was changed to allow INI file content changes.
    /// </summary>
    public class ReadonlyInifile : IniFile
    {
        /// <summary>
        /// Create a ReadonlyInifile object. 
        /// INI file content is not loaded. Use the Reload() method to load it.
        /// </summary>
        /// <param name="pFilename">Filename of INI file to use</param>
        public ReadonlyInifile(string pFilename)
            : base(pFilename)
        {
            this.isWritable = false;
        }

        /// <summary>
        /// Create a ReadonlyInifile object. 
        /// INI file content is not loaded. Use the Reload() method to load it.
        /// </summary>
        /// <param name="pFilename">Filename of INI file to use</param>
        /// <param name="pEncoding">Text encoding of INI file</param>
        public ReadonlyInifile(string pFilename, Encoding pEncoding)
            : base(pFilename, pEncoding)
        {
            this.isWritable = false;
        }

        /// <summary>Change value of specified item in INI file</summary>
        public override void WriteString(string pSectionName, string pValueName, string pValue)
        {
            throw new XServiceError(string.Format("IniFile({0}) is read-only!", this.fileinfo.FullName));
        }

        /// <summary>Remove whole section from INI file</summary>
        public override void DeleteSection(string pSectionName)
        {
            throw new XServiceError(string.Format("IniFile({0}) is read-only!", this.fileinfo.FullName));
        }

        /// <summary>Delete specified INI item from INI file</summary>
        public override void DeleteItem(string pSectionName, string pValueName)
        {
            throw new XServiceError(string.Format("IniFile({0}) is read-only!", this.fileinfo.FullName));
        }
    }

}
