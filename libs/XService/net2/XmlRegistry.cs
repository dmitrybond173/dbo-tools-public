/*
 * XmlRegistry
 * ------------ 
 * the engine maintaining a hierarchical data items storage (kind of Windows system registry)
 * in the XML file.
 * 
 * Version 1.10
 * Written by Dmitry Bond. (dima_ben@ukr.net)
 * 
 * Usage sample:
 
    XmlRegistry reg = new XmlRegistry("sample.reg", true);
  
    XmlRegistryKey key = reg.OpenKey("default", false);
    string x = key.Path; 
    key.WriteInteger("i_num", 2985);
    key.WriteDouble("f_num", 3.45683467);
    key.WriteString("text", "hello, world!");
    key.WriteBytes("data", Encoding.Default.GetBytes("just some data"));
    double f = key.ReadDouble("f_num");
    key.Close();

    key = reg.OpenKey("/travian/village/info", true);
    if (!key.ValueExists("text"))
        key.WriteString("text", "17,3,4,5,6");
    else
        x = key["text"].Value.ToString();
    key.Close();

    key = reg.OpenKey("/travian/village", true);
    if (!key.ValueExists("name"))
        key.WriteString("name", "Dmitrovka");
    else
        x = key["name"].Value.ToString();

    if (!key.ValueExists("sampleText"))
        key.WriteText("sampleText", new StringBuilder("begin\r\ninit();\r\ntest();\r\n5;\r\nend."));
    else
        x = key["sampleText"].Value.ToString();            
  
    key.Close();
 
    string[] keys = reg.KeyNamesAt("/travian/village");

    reg.Flush();
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using XService.Utils;

// PAL is short from "Platform Abstraction Layer"
namespace PAL
{
    /// <summary>
    /// XmlRegistry - the object simulating behaviour of 'windows system registry' but use an XML file as storage.
    /// For example - it could be used on platforms where 'system registry' does not exists but need a similar approach.
    /// Also it could be used to store some specific configuration in a form of hierarchy like 'windiws system registry' does.
    /// </summary>
    public class XmlRegistry
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("XmlRegistry", "XmlRegistry");

        public const string NAME_NODE_Key = "Key";
        public const string NAME_NODE_Item = "Item";

        public const string NAME_ATTR_Name = "name";
        public const string NAME_ATTR_Created = "created";
        public const string NAME_ATTR_Modified = "modified";
        public const string NAME_ATTR_Type = "type";
        public const string NAME_ATTR_Attributes = "attr";
        public const string NAME_ATTR_Tags = "tags";
        public const string NAME_ATTR_NameEx = "__name";
        public const string NAME_ATTR_AttributesEx = "__attr";
        public const string NAME_ATTR_TagsEx = "__tags";

        public XmlRegistry()
        {
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistry(): hash={0}", this.GetHashCode()) : "");
            
            InitDefault();
        }

        public XmlRegistry(string pFilename, EOptions pOptions)
        {
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistry({0}; options={1}): hash={2}", pFilename, pOptions, this.GetHashCode()) : "");

            this.filename = pFilename;
            this.options = pOptions;
            if (File.Exists(this.filename))
                xmlDom.Load(pFilename);
            else
                InitDefault();
        }

        public XmlRegistry(string pFilename, bool pAutoCreate)
        {
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistry({0}; autoCreate={1}): hash={2}", pFilename, pAutoCreate, this.GetHashCode()) : "");

            this.filename = pFilename;
            if (File.Exists(this.filename))
                xmlDom.Load(pFilename);
            else
            {
                if (pAutoCreate)
                    InitDefault();
                else
                    throw new ToolConfigError(string.Format(
                        "XmlRegistry file [{0}] not found!", this.filename));
            }
        }

        ~XmlRegistry()
        {
            Flush();
        }

        public override string ToString()
        {
            return String.Format("xml-reg[{0}]", this.FileName);
        }

        #region Interface

        [Flags]
        public enum EOptions
        {
            None = 0,
            Modified = 0x0001,
            AutoSaveOnCloseKey = 0x0002,
        }

        /// <summary>Get XmlDocument object for this registry storage</summary>
        public XmlDocument XmlDom { get { return this.xmlDom; } }

        /// <summary>Get filename of this registry</summary>
        public string FileName { get { return this.filename; } }

        /// <summary>Get or set options for this registry</summary>
        public EOptions Options
        {
            get { return this.options; }
            set { this.options = value; }
        }

        /// <summary>Get or set Is-Modified flag for this registry</summary>
        public bool IsModified
        {
            get { return ((this.options & EOptions.Modified) == EOptions.Modified); }
            set
            {
                if (value)
                    this.options |= EOptions.Modified;
                else
                    this.options &= ~EOptions.Modified;
            }
        }

        /// <summary>Flush changes if Is-Modified flag is set</summary>
        public void Flush()
        {
            try
            {
                Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                    "XmlRegistry[{0}]->Flush(isModified={1})", this.GetHashCode(), this.IsModified) : "");
            }
            catch { }

            if (this.IsModified)
                Save();
        }

        /// <summary>Force save changes</summary>
        public void Save()
        {
            SaveAs(null);
        }

        /// <summary>Save registry into specified file</summary>
        public void SaveAs(string pFilename)
        {
            bool isDefaultSave = string.IsNullOrEmpty(pFilename);

            if (string.IsNullOrEmpty(pFilename)) pFilename = this.filename;
            if (string.IsNullOrEmpty(pFilename))
                throw new ToolConfigError("Xml registry filename not specified");

            xmlDom.DocumentElement.SetAttribute("savedAt", StrUtils.NskTimestampOf(DateTime.Now));
            xmlDom.DocumentElement.SetAttribute("filename", pFilename);
            xmlDom.DocumentElement.SetAttribute("application", TypeUtils.ApplicationName);

            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? string.Format(
                "XmlRegistry[{0}]->SaveAs({1})", this.GetHashCode(), pFilename) : "");
            xmlDom.Save(pFilename);

            if (isDefaultSave)
                this.IsModified = false;

            if (this.filename != pFilename)
                this.filename = pFilename;
        }

        /// <summary>Create and return registry key with specified path</summary>
        public XmlRegistryKey CreateKey(string pKeyPath)
        {
            XmlElement pathRoot = CreateKeyNode(pKeyPath);
            XmlRegistryKey rk = XmlRegistryKey.Open(this, pathRoot);
            rk.Refresh();
            List<XmlRegistryKey> keysList = new List<XmlRegistryKey>();
            if (this.openedKeys.TryGetValue((XmlElement)pathRoot.ParentNode, out keysList))
                foreach (XmlRegistryKey it in keysList)
                    it.Refresh();
            return rk;
        }

        /// <summary>Try to open registry key with specified path</summary>
        public XmlRegistryKey OpenKey(string pKeyPath, bool pAutoCreate)
        {
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistry[{0}]->OpenKey({1}, autoCreate={2})", this.GetHashCode(), pKeyPath, pAutoCreate) : "");

            string path = pKeyPath;
            XmlElement lastNode, pathRoot = FindKey(ref path, out lastNode);
            if (pathRoot == null)
            {
                if (pAutoCreate)
                {
                    Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                        "XmlRegistry[{0}]->CreateKey({1})", this.GetHashCode(), pKeyPath) : "");
                    pathRoot = CreateKeyNode(pKeyPath);
                }
                else
                    throw new ToolConfigError(string.Format(
                        "XmlRegistry key [{0}] not found", pKeyPath));
            }
            return XmlRegistryKey.Open(this, pathRoot);
        }

        /// <summary>Returns true when registry key with specified path exists</summary>
        public bool KeyExists(string pKeyPath)
        {
            XmlElement lastNode;
            string path = pKeyPath;
            return (this.FindKey(ref path, out lastNode) != null);
        }

        /// <summary>Returns true when registry value with specified path exists</summary>
        public bool ValueExists(string pValuePath)
        {
            object v = this[pValuePath];
            return (v != null);
        }

        /// <summary>Returns array of registry key names at specified path</summary>
        public string[] KeyNamesAt(string pKeyPath)
        {
            XmlElement lastNode, pathRoot;
            string path = pKeyPath;
            if (string.IsNullOrEmpty(path) || path == "\\")
                pathRoot = pathRoot = xmlDom.DocumentElement;
            else
                pathRoot = FindKey(ref path, out lastNode);
            if (pathRoot == null)
                return null;

            List<string> names = new List<string>();
            foreach (XmlNode node in pathRoot.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (string.Compare(node.Name, NAME_NODE_Key) != 0) continue;                

                XmlElement element = (node as XmlElement);
                XmlNode attr = element.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Name);
                if (attr == null)
                    attr = element.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_NameEx);
                if (attr == null) continue;
                names.Add(attr.Value);
            }
            return names.ToArray(); 
        }

        /// <summary>Returns array of registry value names at specified path</summary>
        public string[] ItemNamesAt(string pKeyPath)
        {
            string path = pKeyPath;
            XmlElement lastNode, pathRoot = FindKey(ref path, out lastNode);
            if (pathRoot == null)
                return null;

            List<string> names = new List<string>();
            foreach (XmlNode node in pathRoot.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (string.Compare(node.Name, NAME_NODE_Item) != 0) continue;                

                XmlElement element = (node as XmlElement);
                XmlNode attr = element.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Name);
                if (attr == null)
                    attr = element.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_NameEx);
                if (attr == null) continue;
                names.Add(attr.Value);
            }
            return names.ToArray();
        }

        /// <summary>Access registry item</summary>
        /// <param name="pRegPath">Full path of registry item</param>
        /// <returns>
        /// If pRegPath is a path of registry key it will return string[] - list of key and value names in specified registry key. Key names will have "\" as suffix.
        /// If pRegPath is a path of registry value it will return a value.
        /// </returns>
        public object this[string pRegPath]
        {
            get 
            {
                string path = pRegPath;
                XmlElement lastNode, pathRoot = FindKey(ref path, out lastNode);
                if (pathRoot != null)
                {
                    List<string> list = new List<string>();
                    XmlRegistryKey key = XmlRegistryKey.Open(this, pathRoot);
                    foreach (string k in key.KeyNames)
                        list.Add(k + @"\");
                    list.AddRange(key.ValueNames);
                    key.Close();
                    return list.ToArray();
                }
                else if (lastNode != null)
                {
                    pRegPath = FixPath(pRegPath);
                    if (pRegPath.ToLower().StartsWith(path.ToLower()))
                        pRegPath = pRegPath.Remove(0, path.Length + 1);
                    XmlRegistryKey key = XmlRegistryKey.Open(this, lastNode);
                    if (key.ValueExists(pRegPath))
                        return key[pRegPath].Value;
                }
                return null;
            }
        }

        /// <summary>Remove specified key from registry</summary>
        public void RemoveKey(string pKeyPath)
        {
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistry[{0}]->RemoveKey({1})", this.GetHashCode(), pKeyPath) : "");

            XmlElement lastNode;
            string path = pKeyPath;
            XmlElement keyNode = this.FindKey(ref path, out lastNode);
            if (keyNode != null)
            {
                keyNode.RemoveAll();
                keyNode.ParentNode.RemoveChild(keyNode);
                this.IsModified = true;
            }
        }

        #endregion // Interface

        #region Implementation Details

        internal void registerOpenedKey(XmlRegistryKey pKey)
        {
            List<XmlRegistryKey> list;
            if (!this.openedKeys.TryGetValue(pKey.DomNode, out list))
            {
                list = new List<XmlRegistryKey>();
                this.openedKeys.Add(pKey.DomNode, list);
            }
            list.Add(pKey);
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistry[{0}]->registerOpenedKey({1}): count={2}", this.GetHashCode(), pKey.Path, list.Count) : "");
        }

        internal void registerKeyClosed(XmlRegistryKey pKey)
        {
            int count = 0;
            List<XmlRegistryKey> list;
            if (this.openedKeys.TryGetValue(pKey.DomNode, out list))
            {
                if (list.Contains(pKey))
                    list.Remove(pKey);
                count = list.Count;
            }
            Trace.WriteLineIf(TrcLvl.TraceVerbose, TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistry[{0}]->registerKeyClosed({1}): count={2}", this.GetHashCode(), pKey.Path, count) : "");
        }

        private void InitDefault()
        {
            xmlDom.LoadXml(
                "<?xml version=\"1.0\" encoding=\"Unicode\"?>\n" +
                "<Registry>\n" +
                "   <Key name=\"default\">\n" +
                "       <Item name=\"default\" type=\"string\">@</Item>\n" +
                "   </Key>\n" +
                "</Registry>"
                );
        }

        private string FixPath(string pPath)
        {
            if (pPath.IndexOf("/") >= 0)
                pPath = pPath.Replace("/", @"\");
            if (pPath.StartsWith("\\")) 
                pPath = pPath.Remove(0, 1);
            return pPath;
        }

        private XmlElement CreateKey(XmlElement pRoot, string pKeyName)
        {
            XmlElement element = pRoot.OwnerDocument.CreateElement(NAME_NODE_Key);
            element.SetAttribute(NAME_ATTR_Name, pKeyName);
            element.SetAttribute(NAME_ATTR_Created, StrUtils.NskTimestampOf(DateTime.Now));
            pRoot.AppendChild(element);
            this.IsModified = true;
            return element;
        }

        internal XmlElement CreateKeyNode(string pKeyPath)
        {
            pKeyPath = FixPath(pKeyPath);
            string[] items = pKeyPath.Split('\\');
            XmlElement pathRoot = xmlDom.DocumentElement;
            foreach (string item in items)
            {
                XmlElement element = FindKey(pathRoot, item);
                if (element == null)
                    element = CreateKey(pathRoot, item);
                pathRoot = element;
            }
            return pathRoot;
        }

        private XmlElement FindKey(ref string pKeyPath, out XmlElement pLastValueKeyNode)
        {
            pLastValueKeyNode = null;
            pKeyPath = FixPath(pKeyPath);
            string[] items = pKeyPath.Split('\\');
            XmlElement pathRoot = xmlDom.DocumentElement;
            pKeyPath = "";
            foreach (string item in items)
            {
                XmlElement element = FindKey(pathRoot, item);
                if (element == null)
                    return null;

                if (!string.IsNullOrEmpty(pKeyPath))
                    pKeyPath += @"\";
                pKeyPath += item;
                pathRoot = element;
                pLastValueKeyNode = element;
            }
            return pathRoot;
        }

        private XmlElement FindKey(XmlElement pRoot, string pKeyName)
        {
            foreach (XmlNode node in pRoot.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (string.Compare(node.Name, NAME_NODE_Key) != 0) continue;

                XmlElement element = (node as XmlElement);
                XmlNode attr = element.Attributes.GetNamedItem(NAME_ATTR_Name);
                if (attr == null)
                    attr = element.Attributes.GetNamedItem(NAME_ATTR_NameEx);
                if (attr == null) continue;
                if (string.Compare(attr.Value, pKeyName, true) == 0)
                    return element; 
            }
            return null;
        }

        private XmlElement FindValue(XmlElement pRoot, string pValueName)
        {
            foreach (XmlNode node in pRoot.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (string.Compare(node.Name, NAME_NODE_Item) != 0) continue;

                XmlElement element = (node as XmlElement);
                XmlNode attr = element.Attributes.GetNamedItem(NAME_ATTR_Name);
                if (attr == null)
                    attr = element.Attributes.GetNamedItem(NAME_ATTR_NameEx);
                if (attr == null) continue;
                if (string.Compare(attr.Value, pValueName, true) == 0)
                    return element;
            }
            return null;
        }

        private EOptions options = EOptions.None;
        private XmlDocument xmlDom = new XmlDocument();
        private string filename = null;
        private Dictionary<XmlElement, List<XmlRegistryKey>> openedKeys = new Dictionary<XmlElement, List<XmlRegistryKey>>();

        #endregion // Implementation Details
    }


    public interface IXmlRegistryItem
    {
        XmlElement DomNode { get; }
        string Name { get; set; }
        string Path { get; }
        uint Attributes { get; set; }
        List<string> Tags { get; }
        bool IsContainer { get; }
        Type ValueType { get; }
        object Value { get; set; }
    }

    
    /// <summary>
    /// XmlRegistryKey
    /// </summary>
    public class XmlRegistryKey : IXmlRegistryItem
    {
        internal static XmlRegistryKey Open(XmlRegistry pOwner, XmlElement pKeyNode)
        {
            XmlRegistryKey key = new XmlRegistryKey(pOwner, pKeyNode);
            key.Load();
            return key;
        }

        internal XmlRegistryKey(XmlRegistry pOwner, XmlElement pKeyNode)
        {
            this.owner = pOwner;
            this.keyNode = pKeyNode;
            pOwner.registerOpenedKey(this);
        }

        public override string ToString()
        {
            return String.Format("key[{0}]: {1} items", this.Name, this.ItemsCount);
        }

        #region Interface : IXmlRegistryItem

        /// <summary>Get DOM node for this XmlRegistryKey object</summary>
        public XmlElement DomNode { get { return this.keyNode; } }

        /// <summary>Get name of this XmlRegistryKey object</summary>
        public string Name
        {
            get { return this.keyNode.GetAttribute(XmlRegistry.NAME_ATTR_Name); }
            set 
            {
                this.keyNode.SetAttribute(XmlRegistry.NAME_ATTR_Name, value); 
                this.Owner.IsModified = true; 
            }
        }

        /// <summary>Get full path of this XmlRegistryKey object</summary>
        public string Path
        {
            get
            {
                string path = "";
                XmlElement node = this.keyNode;
                while (node.ParentNode != null && !node.ParentNode.Equals(node.OwnerDocument))
                {
                    if (!string.IsNullOrEmpty(path)) path = path.Insert(0, "\\");
                    path = path.Insert(0, node.GetAttribute(XmlRegistry.NAME_ATTR_Name));
                    node = (node.ParentNode as XmlElement);
                }
                return path;
            }
        }

        /// <summary>Get or set attibutes of this XmlRegistryKey object</summary>
        public uint Attributes
        {
            get { return this.keyAttrs; }
            set
            {
                if (this.keyAttrs != value)
                {
                    this.keyAttrs = value;
                    this.keyNode.SetAttribute(XmlRegistry.NAME_ATTR_Attributes, this.keyAttrs.ToString("X8"));
                }                
            }
        }

        public List<string> Tags { get { return this.tagsList; } }

        /// <summary>Returns true when this registry object is container for values</summary>
        public bool IsContainer { get { return true; } }

        /// <summary>Get type of this value object</summary>
        public Type ValueType { get { return this.GetType(); } }

        /// <summary>Get or set value for this registry item</summary>
        public object Value
        {
            get { return this; }
            set
            {
                throw new ToolConfigError(string.Format("XmlRegistryKey [{0}] - set value for Key is not supported!", this.Path));
            }
        }

        #endregion // Interface : IXmlRegistryItem

        #region Interface

        /// <summary>Get owner XmlRegistry object</summary>
        public XmlRegistry Owner { get { return this.owner; } }

        /// <summary>Returns true when this XmlRegistryKey object is opened (valid for usage)</summary>
        public bool IsOpened { get { return (this.keyNode != null); } }

        /// <summary>Returns number of items in this XmlRegistryKey object (Keys and Values)</summary>
        public int ItemsCount { get { return this.items.Count; } }

        /// <summary>Returns array of key names in this XmlRegistryKey object</summary>
        public string[] KeyNames { get { return this.keyNames.ToArray(); } }

        /// <summary>Returns array of value names in this XmlRegistryKey object</summary>
        public string[] ValueNames 
        { 
            get 
            {
                List<string> list = new List<string>();
                foreach (KeyValuePair<string, XmlRegistryItem> item in this.items)
                {
                    list.Add(item.Key);
                }
                return list.ToArray(); 
            } 
        }

        public bool HasKey(string pKeyName)
        {
            return (this.keyNames.IndexOf(pKeyName.ToLower()) >= 0);
        }

        public bool HasItem(string pItemName)
        {
            XmlRegistryItem item;
            return this.items.TryGetValue(pItemName.ToLower(), out item);
        }

        public bool HasTag(string pTag)
        {
            return this.Tags.Contains(pTag);
        }

        public IEnumerator NamesEnumerator { get { return this.items.Keys.GetEnumerator(); } }

        /// <summary>Close this XmlRegistryKey object</summary>
        public void Close()
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryKey[{0}]->Close({1})", this.Owner.GetHashCode(), this.Path) : "");

            if ((this.owner.Options & XmlRegistry.EOptions.AutoSaveOnCloseKey) == XmlRegistry.EOptions.AutoSaveOnCloseKey)
            {
                this.owner.Save();
            }

            this.owner.registerKeyClosed(this);

            this.keyNode = null;
            this.items.Clear();
        }

        /// <summary>Access specified XmlRegistryItem in this XmlRegistryKey object (by its name)</summary>
        public XmlRegistryItem this[string pItemName] { get { return this.items[pItemName.ToLower()]; } }

        /// <summary>Returns true when value with specified name is exists in this XmlRegistryKey object</summary>
        public bool ValueExists(string pItemName)
        {
            return this.items.ContainsKey(pItemName.ToLower());
        }

        /// <summary>Read string value with specified name from this XmlRegistryKey object</summary>
        public string ReadString(string pItemName)
        {
            XmlRegistryItem item;
            if (!FindItem(pItemName, out item, false))
                throw new ToolConfigError(string.Format("XmlRegistry item ({0}) not found", pItemName));
            return item.Value.ToString();
        }

        /// <summary>Read text value with specified name from this XmlRegistryKey object</summary>
        public string ReadText(string pItemName)
        {
            XmlRegistryItem item;
            if (!FindItem(pItemName, out item, false))
                throw new ToolConfigError(string.Format("XmlRegistry item ({0}) not found", pItemName));
            return item.Value.ToString();
        }

        /// <summary>Read integer value with specified name from this XmlRegistryKey object</summary>
        public int ReadInteger(string pItemName)
        {
            XmlRegistryItem item;
            if (!FindItem(pItemName, out item, false))
                throw new ToolConfigError(string.Format("XmlRegistry item ({0}) not found", pItemName));
            if (item.ValueType != typeof(int))
                throw new ToolConfigError(string.Format("XmlRegistry.ReadInteger item ({0}) - wrong data type requested ({1})",
                    pItemName, item.ValueType.Name));
            return (int)item.Value;
        }

        /// <summary>Read double value with specified name from this XmlRegistryKey object</summary>
        public double ReadDouble(string pItemName)
        {
            XmlRegistryItem item;
            if (!FindItem(pItemName, out item, false))
                throw new ToolConfigError(string.Format("XmlRegistry item ({0}) not found", pItemName));
            if (item.ValueType != typeof(double))
            {
                if (item.ValueType == typeof(int))
                    return (double)Convert.ChangeType(item.Value, typeof(double));
                throw new ToolConfigError(string.Format("XmlRegistry.ReadDouble item ({0}) - wrong data type requested ({1})", 
                    pItemName, item.ValueType.Name));
            }
            return (double)item.Value;
        }

        /// <summary>Read raw data value (array of bytes) with specified name from this XmlRegistryKey object</summary>
        public byte[] ReadBytes(string pItemName)
        {
            XmlRegistryItem item;
            if (!FindItem(pItemName, out item, false))
                throw new ToolConfigError(string.Format("XmlRegistry item ({0}) not found", pItemName));
            if (item.ValueType != typeof(byte[]))
                throw new ToolConfigError(string.Format("XmlRegistry.ReadBytes item ({0}) - wrong data type requested ({1})",
                    pItemName, item.ValueType.Name));
            return (item.Value as byte[]);
        }

        /// <summary>Write string value with specified name into this XmlRegistryKey object</summary>
        public void WriteString(string pItemName, string pValue)
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryKey[{0}]->WriteSting(key={1}; item={2}; value={3})", this.Owner.GetHashCode(), this.Path, pItemName, pValue) : "");

            XmlRegistryItem item;
            FindItem(pItemName, out item, true);
            item.Value = pValue;
        }

        /// <summary>Write text value with specified name into this XmlRegistryKey object</summary>
        public void WriteText(string pItemName, StringBuilder pValue)
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryKey[{0}]->WriteText(key={1}; item={2}; value={3})", this.Owner.GetHashCode(), this.Path, pItemName, pValue) : "");

            XmlRegistryItem item;
            FindItem(pItemName, out item, true);
            item.Value = pValue;
        }

        /// <summary>Write integer value with specified name into this XmlRegistryKey object</summary>
        public void WriteInteger(string pItemName, int pValue)
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryKey[{0}]->WriteInteger(key={1}; item={2}; value={3})", this.Owner.GetHashCode(), this.Path, pItemName, pValue) : "");

            XmlRegistryItem item;
            FindItem(pItemName, out item, true);
            item.Value = pValue;
        }

        /// <summary>Write double value with specified name into this XmlRegistryKey object</summary>
        public void WriteDouble(string pItemName, double pValue)
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryKey[{0}]->WriteDouble(key={1}; item={2}; value={3})", this.Owner.GetHashCode(), this.Path, pItemName, pValue) : "");

            XmlRegistryItem item;
            FindItem(pItemName, out item, true);
            item.Value = pValue;
        }

        /// <summary>Write raw data value with specified name into this XmlRegistryKey object</summary>
        public void WriteBytes(string pItemName, byte[] pValue)
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryKey[{0}]->WriteBytes(key={1}; item={2}; value=[{3} bytes])", this.Owner.GetHashCode(), this.Path, pItemName, 
                (pValue != null ? pValue.Length.ToString() : "(null)")) : "");

            XmlRegistryItem item;
            FindItem(pItemName, out item, true);
            item.Value = pValue;
        }

        /// <summary>Write raw data value with specified name into this XmlRegistryKey object</summary>
        public void RemoveValue(string pItemName)
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryKey[{0}]->RemoveValue(key={1}; item={2})", this.Owner.GetHashCode(), this.Path, pItemName) : "");

            XmlRegistryItem item;
            string fixedName = pItemName.ToLower();
            if (this.items.TryGetValue(fixedName, out item))
            {
                item.DomNode.RemoveAll();
                item.DomNode.ParentNode.RemoveChild(item.DomNode);

                this.items.Remove(fixedName);

                this.Owner.IsModified = true;
            }
        }

        /// <summary>Reload and merge registry key definitions</summary>
        public void Refresh()
        {
            Load();
        }

        /// <summary>Ensure specified tag defined for this registry key</summary>
        public void EnsureTag(string pTag)
        {
            if (!this.Tags.Contains(pTag))
                this.Tags.Add(pTag);
            this.DomNode.SetAttribute(XmlRegistry.NAME_ATTR_Tags, StrUtils.Join(this.Tags.ToArray(), ","));
        }

        /// <summary>Ensure specified tag defined for this registry key</summary>
        public void EnsureNoTag(string pTag)
        {
            if (this.Tags.Contains(pTag))
                this.Tags.Remove(pTag);
            this.DomNode.SetAttribute(XmlRegistry.NAME_ATTR_Tags, StrUtils.Join(this.Tags.ToArray(), ","));
        }

        #endregion // Interface

        #region Implementation Details

        private bool FindItem(string pItemName, out XmlRegistryItem item, bool pAutoCreate)
        {
            item = null;
            string fixedName = pItemName.ToLower();
            if (!this.items.TryGetValue(fixedName, out item))
            { 
                if (pAutoCreate)
                    item = CreateItem(pItemName);
            }
            return (item != null);
        }

        private void Load() 
        {
            this.tagsList.Clear();

            XmlNode attrNode = keyNode.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Attributes);
            if (attrNode != null)
            {
                this.keyAttrs = Convert.ToUInt32(attrNode.Value, 16);
            }

            attrNode = keyNode.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Tags);
            if (attrNode != null)
            {
                string s = attrNode.Value.Replace(',', ';');
                this.tagsList.AddRange(s.Split(';'));
            }

            List<string> keysList = new List<string>();
            List<string> valuesList = new List<string>();
            foreach (XmlNode node in this.keyNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (string.Compare(node.Name, XmlRegistry.NAME_NODE_Key, true) == 0)
                {
                    attrNode = node.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Name);
                    if (attrNode == null) continue;
                    string name = attrNode.Value;
                    string id = name.ToLower();
                    keysList.Add(id);
                    if (!this.keyNames.Contains(id))
                        this.keyNames.Add(id);
                }
                else if (string.Compare(node.Name, XmlRegistry.NAME_NODE_Item, true) == 0)
                {
                    attrNode = node.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Name);
                    if (attrNode == null) continue;
                    string name = attrNode.Value;
                    string id = name.ToLower();
                    valuesList.Add(id);
                    XmlRegistryItem rItem;
                    if (!this.items.TryGetValue(id, out rItem))
                        this.items.Add(id, rItem = new XmlRegistryItem(this, node as XmlElement, name));
                }
            }

            List<string> toDelete = new List<string>();
            foreach (string kn in this.keyNames)
            {
                if (!keysList.Contains(kn))
                    toDelete.Add(kn);
            }
            foreach (string kn in toDelete)
            {
                this.Owner.RemoveKey(this.Path + "\\" + kn);
            }

            toDelete.Clear();
            foreach (KeyValuePair<string, XmlRegistryItem> it in this.items)
            {
                if (!valuesList.Contains(it.Key))
                    toDelete.Add(it.Key);
            }
            foreach (string kn in toDelete)
            {
                this.RemoveValue(kn);
            }
        }

        private XmlRegistryItem CreateItem(string pItemName)
        {
            XmlElement element = this.keyNode.OwnerDocument.CreateElement(XmlRegistry.NAME_NODE_Item);
            this.keyNode.AppendChild(element);
            element.SetAttribute(XmlRegistry.NAME_ATTR_Name, pItemName);
            pItemName = pItemName.ToLower();
            XmlRegistryItem item = new XmlRegistryItem(this, element, pItemName);
            this.items.Add(pItemName.ToLower(), item);
            return item;
        }

        private XmlRegistry owner;
        private XmlElement keyNode;
        private uint keyAttrs = 0;
        private List<string> keyNames = new List<string>();
        private List<string> tagsList = new List<string>();
        private Dictionary<string, XmlRegistryItem> items = new Dictionary<string, XmlRegistryItem>();

        #endregion // Implementation Details
    }


    /// <summary>
    /// XmlRegistryItem
    /// </summary>
    public class XmlRegistryItem : IXmlRegistryItem
    {
        internal XmlRegistryItem(XmlRegistryKey pOwner, XmlElement pItemNode, string pName)
        {
            this.owner = pOwner;
            this.itemNode = pItemNode;
            this.name = pName;

            LoadValue();
        }

        public override string ToString()
        {
            return String.Format("item[{0}]:{1}={2}", this.Name, this.type, this.Value.ToString());
        }

        #region Interface : IXmlRegistryItem

        /// <summary>Get DOM node for this value object</summary>
        public XmlElement DomNode { get { return this.itemNode; } }

        /// <summary>Get name of this value object</summary>
        public string Name 
        { 
            get { return this.name; }
            set 
            {
                this.name = value;
                this.itemNode.SetAttribute("name", this.name);
                this.Owner.Owner.IsModified = true;
            }
        }

        public string Path { get { return this.Owner.Path + "/@" + this.Name; } }

        /// <summary>Get or set attributes of this value object</summary>
        public uint Attributes
        {
            get { return this.valueAttr; }
            set
            {
                if (this.valueAttr != value)
                {
                    this.valueAttr = value;
                    this.itemNode.SetAttribute("attr", this.valueAttr.ToString("X8"));
                }
            }

        }

        public List<string> Tags { get { return this.tagsList; } }

        public bool IsContainer { get { return false; } }

        /// <summary>Get type of this value object</summary>
        public Type ValueType { get { return this.type; } }

        /// <summary>Get or set value for this registry item</summary>
        public object Value
        {
            get { return this.itemValue; }
            set
            {
                Type t = value.GetType();
                bool isOk = (
                    t == typeof(string)
                    || t == typeof(StringBuilder)
                    || t == typeof(int)
                    || t == typeof(float)
                    || t == typeof(double)
                    || t == typeof(byte[]));
                if (!isOk)
                    throw new ToolConfigError(string.Format("XmlRegistry item [{0}] - unsupported data type ({1})",
                        this.Name, t.ToString()));

                this.typeChanged = (value.GetType() != this.type);
                this.Owner.Owner.IsModified = true;

                this.itemValue = value;
                this.type = t;

                SaveValue();
            }
        }

        #endregion // Interface : IXmlRegistryItem

        #region Interface

        /// <summary>Get owner XmlRegistryKey object</summary>
        public XmlRegistryKey Owner { get { return this.owner; } }

        /// <summary>Returns true when this value object is a text (String or StringBuilder)</summary>
        public bool IsText { get { return (this.type == typeof(string) || this.type == typeof(StringBuilder)); } }

        /// <summary>Returns true when this value object is a number (int, float or double)</summary>
        public bool IsNumber { get { return (this.type == typeof(int) || this.type == typeof(float) || this.type == typeof(double)); } }

        /// <summary>Returns true when this value object is a raw data (array of bytes)</summary>
        public bool IsRawData { get { return (this.type == typeof(byte[])); } }

        #endregion // Interface

        #region Implementation Details

        private void LoadValue()
        {
            XmlNode attr = this.itemNode.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Attributes);
            if (attr != null)
            {
                this.valueAttr = Convert.ToUInt32("0x" + attr.Value);
            }

            attr = this.itemNode.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Tags);
            if (attr != null)
            {
                string s = attr.Value.Replace(',', ';');
                this.tagsList.AddRange(s.Split(';'));
            }

            attr = this.itemNode.Attributes.GetNamedItem(XmlRegistry.NAME_ATTR_Type);
            if (attr != null)
            {
                string t = attr.Value.ToLower();
                if (t.Equals("string")) this.type = typeof(string);
                else if (t.Equals("int") || t.Equals("integer")) this.type = typeof(int);
                else if (t.Equals("float") || t.Equals("double")) this.type = typeof(double);
                else if (t.Equals("raw") || t.Equals("bytes")) this.type = typeof(byte[]);
                else if (t.Equals("text")) this.type = typeof(StringBuilder);
            }

            if (this.type == typeof(string))
                this.itemValue = "";
            else if (this.type == typeof(int))
                this.itemValue = 0;
            else if (this.type == typeof(double))
                this.itemValue = 0.0;
            else if (this.type == typeof(StringBuilder))
                this.itemValue = new StringBuilder();

            foreach (XmlNode node in this.itemNode.ChildNodes)
            {
                bool isOk = (node.NodeType == XmlNodeType.CDATA
                    || node.NodeType == XmlNodeType.Text);
                if (!isOk) continue;

                this.valueNode = node;
                this.itemValue = this.valueNode.Value.ToString();
                break;
            }
            if (this.valueNode != null)
            {
                if (!this.IsText)
                {
                    if (this.IsRawData)
                        this.itemValue = Convert.FromBase64String(this.itemValue.ToString());
                    else
                        this.itemValue = Convert.ChangeType(this.itemValue, this.type);
                }
            }
        }

        private void SaveValue()
        {
            Trace.WriteLineIf(XmlRegistry.TrcLvl.TraceVerbose, XmlRegistry.TrcLvl.TraceVerbose ? string.Format(
                "XmlRegistryItem[{0}]->SaveValue(key={1}; item={2})", this.Owner.Owner.GetHashCode(), this.Owner.Path, this.Name, this.itemValue) : "");

            if (this.type == typeof(string))
            {
                if (this.valueNode == null || this.valueNode.NodeType != XmlNodeType.CDATA)
                {
                    if (this.valueNode != null) this.itemNode.RemoveChild(this.valueNode);
                    this.valueNode = this.itemNode.OwnerDocument.CreateCDataSection(this.itemValue.ToString());
                    this.itemNode.AppendChild(this.valueNode);
                }
                else
                    this.valueNode.Value = this.itemValue.ToString();
                if (this.typeChanged)
                    this.itemNode.SetAttribute(XmlRegistry.NAME_ATTR_Type, "string");
            }
            if (this.type == typeof(StringBuilder))
            {
                if (this.valueNode == null || this.valueNode.NodeType != XmlNodeType.CDATA)
                {
                    if (this.valueNode != null) this.itemNode.RemoveChild(this.valueNode);
                    this.valueNode = this.itemNode.OwnerDocument.CreateCDataSection(this.itemValue.ToString());
                    this.itemNode.AppendChild(this.valueNode);
                }
                else
                    this.valueNode.Value = this.itemValue.ToString();
                if (this.typeChanged)
                    this.itemNode.SetAttribute(XmlRegistry.NAME_ATTR_Type, "text");
            }
            else if (this.type == typeof(byte[]))
            {
                if (this.valueNode == null || this.valueNode.NodeType != XmlNodeType.CDATA)
                {
                    if (this.valueNode != null) this.itemNode.RemoveChild(this.valueNode);
                    this.valueNode = this.itemNode.OwnerDocument.CreateCDataSection(Convert.ToBase64String((byte[])this.itemValue));
                    this.itemNode.AppendChild(this.valueNode);
                }
                else
                    this.valueNode.Value = Convert.ToBase64String((byte[])this.itemValue);
                if (this.typeChanged)
                    this.itemNode.SetAttribute(XmlRegistry.NAME_ATTR_Type, "raw");
            }
            else
            {
                if (this.valueNode == null || this.valueNode.NodeType != XmlNodeType.Text)
                {
                    if (this.valueNode != null) this.itemNode.RemoveChild(this.valueNode);
                    this.valueNode = this.itemNode.OwnerDocument.CreateTextNode(this.itemValue.ToString());
                    this.itemNode.AppendChild(this.valueNode);
                }
                else
                    this.valueNode.Value = this.itemValue.ToString();
                if (this.typeChanged)
                {
                    if (this.type == typeof(int))
                        this.itemNode.SetAttribute(XmlRegistry.NAME_ATTR_Type, "int");
                    else if (this.type == typeof(double))
                        this.itemNode.SetAttribute(XmlRegistry.NAME_ATTR_Type, "double");
                }
            }

            this.itemNode.SetAttribute(XmlRegistry.NAME_ATTR_Modified, StrUtils.NskTimestampOf(DateTime.Now));
        }

        private XmlRegistryKey owner;
        private XmlElement itemNode;
        private XmlNode valueNode;
        private string name;
        private uint valueAttr = 0;
        private bool typeChanged = false;
        private Type type = typeof(string);
        private object itemValue = null;
        private List<string> tagsList = new List<string>();

        #endregion // Implementation Details
    }
}
