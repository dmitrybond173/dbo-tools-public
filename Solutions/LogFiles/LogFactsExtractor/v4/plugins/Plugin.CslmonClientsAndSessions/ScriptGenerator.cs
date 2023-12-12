/*
 * Script Generator - engine for generating text files.
 * 
 * Note: I copy-pasted this code from my XService.Net2.dll lib.
 * Because I have to adjust it for some purposes...
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2023-12-06
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using XService.Utils;

namespace Plugin.CslmonClientsAndSessions
{
    /// <summary>
    /// Simple engine to generate scripts
    /// <example>
    ///   <ScriptTemplates>
    ///     <Template name="ConnectDB">
    ///       <Header><![CDATA[
    ///         @echo off
    ///         set BTO_DbName=$(DbName)
    ///         set BTO_DbUser=$(DbUser)
    ///         set BTO_DbPwd=$(DbPassword)
    ///         db2 connect to %BTO_DbName% user %BTO_DbUser% using %BTO_DbPwd%
    ///         echo Connected at %DATE%, %TIME%
    ///       ]]></Header>            
    ///     </Template>
    /// // Then in a code you can do something like this:
    ///   XmlElement domNode = (XmlElement)ConfigurationManager.GetSection("ScriptTemplates");
    ///   this.Scripting = new ScriptGenerator(domNode);
    ///   [...]
    ///   this.Scripting.SetVariable("DbName", this.Descriptor.DbName);
    ///   this.Scripting.SetVariable("DbUser", this.Descriptor.DbUser);
    ///   this.Scripting.SetVariable("DbPassword", this.Descriptor.DbPassword);
    ///   this.Scripting.Start("ConnectDB");
    ///   this.Scripting.AddHeader();
    ///   string connectScript = this.Scripting.Finish();
    /// </example>
    /// </summary>
    public class ScriptGenerator
    {
        public ScriptGenerator(XmlElement pDomNode)
        {
            this.Parent = null;
            this.Variables = new Dictionary<string, object>();
            this.templatesStack = new Stack<Template>();

            loadTemplates(pDomNode);
        }

        public ScriptGenerator(ScriptGenerator pGenerator)
        {
            this.Parent = pGenerator;
            this.Variables = pGenerator.Variables;
            this.templates = pGenerator.templates;
            this.templatesStack = pGenerator.templatesStack;
        }

        public ScriptGenerator Parent { get; protected set; }
        public string Script { get; protected set; }

        /// <summary>Name of current(active) template or null when there is no active template</summary>
        public string TemplateName { get; protected set; }

        public Template CurrentTemplate { get; protected set; }

        public string TemplatePath 
        {
            get
            {
                string txt = this.TemplateName;
                if (txt == null) return txt;

                ScriptGenerator sg = this;
                while (sg.Parent != null)
                { 
                    sg = this.Parent;
                    txt = txt.Insert(0, sg.TemplateName + "/");
                }
                return txt;
            }
        }

        /// <summary>Container of variables. Template names are uppercased. </summary>
        public Dictionary<string, object> Variables { get; protected set; }

        /// <summary>Container of templates. Template names are uppercased.</summary>
        public Dictionary<string, Template> Templates { get { return this.templates; } }

        public delegate bool GetMacroValueMethod(string pTemplate, int pRecLevel, string pPrmName, out object pValue);
        public GetMacroValueMethod OnGetMactoValue { get; set; } // <- deprecated
        public GetMacroValueMethod OnGetMacroValue { get; set; } 

        public Template GetTemplate(string pTemplateName)
        {
            Template t;
            if (!this.templates.TryGetValue(pTemplateName.ToUpper(), out t))
                t = null;
            return t;
        }

        /// <summary>Get value of specified variable. First it will try to get variable using a path of template</summary>
        /// <param name="pVarName">Name of variable</param>
        /// <returns>Value of variable or null when not found</returns>
        public object GetVariable(string pVarName)
        {
            object v;
            string id = pVarName, path = null;
            if (this.Parent != null)
            {
                path = this.TemplatePath;
                if (path != null)
                    id = path + "/" + id;
            }
            if (!this.Variables.TryGetValue(id.ToUpper(), out v))
            {
                // search variable on all levels up from current template recursion
                if (path != null)
                {
                    string path2 = path;
                    while (!string.IsNullOrEmpty(path))
                    {
                        int n = path2.IndexOf('/');
                        if (n >= 0)
                        {
                            path2 = path2.Remove(0, n + 1);
                            id = pVarName;
                            if (!string.IsNullOrEmpty(path2))
                                id = path2 + "/" + id;
                            if (this.Variables.TryGetValue(id.ToUpper(), out v))
                                return v;
                        }

                        n = path.LastIndexOf('/');
                        if (n >= 0)
                            path = path.Substring(0, n);
                        else
                            path = "";
                        id = pVarName;
                        if (!string.IsNullOrEmpty(path))
                            id = path + "/" + id;
                        if (this.Variables.TryGetValue(id.ToUpper(), out v))
                            return v;
                    }
                }
                v = null;
            }
            return v;
        }

        /// <summary>Set value of specified variable. When it is a sub-generator then it will automatically add a path of template in front of variable name</summary>
        public void SetVariable(string pVarName, object pValue)
        {
            string id = pVarName, path = null;
            if (this.Parent != null)
            {
                path = this.TemplatePath;
                if (path != null)
                    id = path + "/" + id;
            }
            this.Variables[id.ToUpper()] = pValue;
        }

        /// <summary>Expand macro values in specified text</summary>
        /// <param name="pText">Text to expand macro values in</param>
        /// <returns>Text with replaced macro values</returns>
        public string ExpandMacroValues(string pText)
        {
            return expandMacroValues(pText);
        }

        /// <summary>Begin script generarion (reset internal structures for new script generation)</summary>
        /// <param name="pTemplateName"></param>
        public void Start(string pTemplateName)
        {
            this.TemplateName = pTemplateName;

            Template t = GetTemplate(pTemplateName);
            if (t == null)
                throw new Exception(string.Format("ScriptTemplate[{0}] is not found!", pTemplateName));

            clearCached();
            this.CurrentTemplate = t;
            this.templatesStack.Push(t);
            SetVariable("Template", pTemplateName);
            this.Script = string.Empty;
            this.itemIndex = 0;
        }

        /// <summary>Add script header to output</summary>
        public void AddHeader()
        {
            if (this.CurrentTemplate == null)
                throw new Exception(string.Format("ScriptTemplate[{0}] is not found!", this.TemplateName));
            this.Script += expandMacroValues(this.CurrentTemplate.Header);
        }

        /// <summary>Add script item to output</summary>
        /// <param name="pItem">Data item</param>
        /// <param name="pItemName">Optional. Name of item template to use (or null to use default)</param>
        public void AddItem(object pItem, string pItemName)
        {
            if (this.CurrentTemplate == null)
                throw new Exception(string.Format("ScriptTemplate[{0}] is not found!", this.TemplateName));

            Template.TemplateItem tmpltItem = this.CurrentTemplate.Item;
            if (!string.IsNullOrEmpty(pItemName))
            {
                if (!this.CurrentTemplate.Items.TryGetValue(pItemName.ToUpper(), out tmpltItem))
                    throw new Exception(string.Format("ScriptTemplate[{0}] - item template[{1}] is not found!", this.TemplateName, pItemName));
            }
            if (!string.IsNullOrEmpty(tmpltItem.Filter))
            {
                if (pItemName == null)
                {
                    tmpltItem = null;
                    // found 1st item which pass the filter
                    foreach (KeyValuePair<string, Template.TemplateItem> it in this.CurrentTemplate.Items)
                    {
                        if (applyItemFilter(ref pItem, it.Value))
                        {
                            tmpltItem = it.Value;
                            break;
                        }
                    }
                    if (tmpltItem == null)
                        return;
                }
                else
                {
                    if (!applyItemFilter(ref pItem, tmpltItem))
                        return;
                }
            }

            SetVariable("Item", pItem);
            SetVariable("ItemIndex", this.itemIndex);
            SetVariable("ItemNumber", this.itemIndex + 1);
            this.Script += expandMacroValues(tmpltItem.Text);
            this.itemIndex++;
        }

        /// <summary>Add item text to output</summary>
        /// <param name="pItemText">item text</param>
        public void AddItemText(string pItemText)
        {
            this.Script += pItemText;
            this.itemIndex++;
        }

        /// <summary>Add script footer to output</summary>
        public void AddFooter()
        {
            if (this.CurrentTemplate == null)
                throw new Exception(string.Format("ScriptTemplate[{0}] is not found!", this.TemplateName));
            this.Script += expandMacroValues(this.CurrentTemplate.Footer);
        }

        /// <summary>Finish script generation, return script output as string</summary>
        public string Finish()
        {
            if (this.CurrentTemplate == null)
                throw new Exception(string.Format("ScriptTemplate[{0}] is not found!", this.TemplateName));

            string txt = this.Script;
            this.Script = string.Empty;
            
            if (!this.templatesStack.Peek().Equals(this.CurrentTemplate))
                throw new Exception(string.Format("ScriptTemplate[{0}] template in stack({1}) is not match({2})!",
                    this.TemplateName, this.templatesStack.Peek(), this.CurrentTemplate));
            this.templatesStack.Pop();

            this.TemplateName = null;
            return txt;
        }

        /// <summary>Generate script for specified data item according to specified template. It will call Start, AddHeader, then make number of calls to AddItem if some items specified then call AddFooter and Finish</summary>
        /// <param name="pTemplateName">Name of script template to use</param>
        /// <param name="pData">Data item to use as data source</param>
        /// <returns>Returns generated script as string</returns>
        public string Generate(string pTemplateName, object pData)
        {
            Start(pTemplateName);
            AddHeader();
            if (pData is IEnumerable)
            {
                SetVariable("ItemsCount", CountItems((IEnumerable)pData));
                foreach (object x in (IEnumerable)pData)
                {
                    AddItem(x, null);
                }
            }
            AddFooter();
            return Finish();
        }

        public static int CountItems(IEnumerable pItems)
        { 
            int cnt = 0;
            foreach (object x in pItems)
                cnt++;
            return cnt;
        }

        #region Implementation Details

        protected enum EMacroCommand
        { 
            Template,
            LOP,
            Count,
        }

        private void loadTemplates(XmlElement pDomNode)
        {
            if (this.templates == null)
                this.templates = new Dictionary<string, Template>();

            foreach (XmlNode node in pDomNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (StrUtils.IsSameText(node.Name, "Template"))
                    loadTemplate((XmlElement)node);
            }
        }

        private string repackScriptText(string pText)
        {
            string result = pText;
            string[] lines = StrUtils.AdjustLineBreaks(pText, "\n").Split('\n');
            // find min len of starting spaced
            int? minSpcLen = null;
            foreach (string ln in lines)
            { 
                if (string.IsNullOrEmpty(ln)) continue;

                int spcLen = 0;
                for (int i = 0; i < ln.Length; i++)
                {
                    if (StrUtils.STR_SPACES.IndexOf(ln[i]) >= 0)
                        spcLen = i;
                    else
                        break;
                }
                if (!minSpcLen.HasValue || (minSpcLen.HasValue && spcLen < minSpcLen))
                    minSpcLen = spcLen;
            }
            if (minSpcLen.HasValue && minSpcLen > 0)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string ln = lines[i].TrimEnd();
                    if (ln.Length >= minSpcLen) 
                        ln = ln.Remove(0, minSpcLen.Value + 1);
                    lines[i] = ln;
                }
                result = StrUtils.Join(lines, Environment.NewLine);
            }
            return result;
        }

        private void loadTemplate(XmlElement pDomNode)
        {
            XmlNode attr = pDomNode.GetAttributeNode("name");
            if (attr == null) return;

            Template t = new Template() { Name = attr.Value };
            foreach (XmlNode node in pDomNode.ChildNodes)
            { 
                if (node.NodeType != XmlNodeType.Element) continue;
                if (StrUtils.IsSameText(node.Name, "Header"))
                    t.Header = repackScriptText(XmlUtils.LoadText((XmlElement)node)); // TextUtils.RepackText(
                else if (StrUtils.IsSameText(node.Name, "Item"))
                {
                    string itemText = repackScriptText(XmlUtils.LoadText((XmlElement)node));
                    
                    attr = ((XmlElement)node).GetAttributeNode("filter");                    
                    string filter = (attr != null ? attr.Value : null);

                    attr = ((XmlElement)node).GetAttributeNode("name");
                    string id = (attr != null ? attr.Value : string.Format("#{0}", (t.Items != null ? t.Items.Count : 0)));

                    Template.TemplateItem item = null;
                    if (t.Items == null)
                        t.Items = new Dictionary<string, Template.TemplateItem>();
                    item = new Template.TemplateItem() { Name = id, Text = itemText, Filter = filter };
                    t.Items[id.ToUpper()] = item;
                }
                else if (StrUtils.IsSameText(node.Name, "Footer"))
                    t.Footer = repackScriptText(XmlUtils.LoadText((XmlElement)node));
            }
            this.templates[t.Name.ToUpper()] = t;
        }

        private void clearCached()
        {
            this.lopCache.Clear();
        }

        /// <summary>Return true to include item into output, return false to skip item</summary>
        private bool applyItemFilter(ref object pData, Template.TemplateItem pTemplateItem)
        {
            string[] items = pTemplateItem.Filter.Split(';');
            object value = pData;
            foreach (string it in items)
            {
                string filter = it;
                bool isNegation = false;
                bool isOk = true;
                if (filter.StartsWith("!")) 
                {
                    filter = filter.Remove(0, 1);
                    isNegation = true;
                }
                
                if (StrUtils.IsSameText(filter, "trim")) 
                    value = value.ToString().Trim();
                else if (StrUtils.IsSameText(filter, "isNullOrEmpty")) 
                    isOk = string.IsNullOrEmpty(value.ToString());
                else if (StrUtils.IsSameText(filter, "isEven")) 
                    isOk = ((this.itemIndex % 2) == 0);
                else if (StrUtils.IsSameText(filter, "isOdd")) 
                    isOk = ((this.itemIndex % 2) != 0);

                if (isNegation) isOk = !isOk;
                if (!isOk)
                    return false;
            }
            if (!pData.Equals(value))
                pData = value;
            return true;
        }

        private bool handleMacroCommand(string pPrmName, out string pPrmValue, object pContext)
        {
            pPrmValue = null;
            string cmdName = StrUtils.GetToPattern(pPrmName, ":");
            string prm = StrUtils.GetAfterPattern(pPrmName, ":");
            EMacroCommand cmd = (EMacroCommand)Enum.Parse(typeof(EMacroCommand), cmdName, true);
            switch (cmd)
            {
                // $(template:
                case EMacroCommand.Template:
                    {
                        string templateName = StrUtils.GetToPattern(prm, ",");
                        if (templateName == null)
                            templateName = prm;
                        Template t = GetTemplate(templateName);
                        if (t == null)
                            throw new Exception(string.Format("ScriptTemplate[{0}] is not found!", templateName));
                        ScriptGenerator subGen = new ScriptGenerator(this);
                        subGen.Start(templateName);
                        object v = subGen.GetVariable("Data");
                        if (v != null)
                        {
                            subGen.AddHeader();
                            if (v is IEnumerable)
                            {
                                SetVariable("ItemsCount", CountItems((IEnumerable)v));
                                foreach (object x in (IEnumerable)v)
                                {
                                    subGen.AddItem(x, null);
                                }
                            }
                            subGen.AddFooter();
                            pPrmValue = subGen.Finish();
                        }
                    }
                    break;

                case EMacroCommand.LOP:
                    {
                        object v = GetVariable("Item");
                        if (v != null)
                        {
                            string s = v.ToString();
                            Dictionary<string, string> lop;
                            if (!this.lopCache.TryGetValue(s, out lop))
                            {
                                lop = new Dictionary<string,string>();
                                this.lopCache[s] = lop;
                                CollectionUtils.ParseParametersStrEx(lop, s, true, ';', ":=");
                            }
                            else
                                lop = this.lopCache[s];
                            if (lop.TryGetValue(prm.ToLower(), out s))
                                pPrmValue = s;
                        }
                    }
                    break;

                case EMacroCommand.Count:
                    break;
            }
            return (pPrmValue != null);
        }

        private bool getPrmValue(string pPrmName, out string pPrmValue, object pContext)
        {
            if (pPrmName.IndexOf(':') > 0)
                return handleMacroCommand(pPrmName, out pPrmValue, pContext);

            pPrmValue = null;
            object v = GetVariable(pPrmName); //bool isOk = this.Variables.TryGetValue(pPrmName.ToUpper(), out v);
            bool isOk = (v != null);
            if (isOk)
            {
                pPrmValue = v.ToString();
            }
            else if (this.OnGetMactoValue != null || this.OnGetMacroValue != null)
            {
                Template t = null;
                if (this.templatesStack.Count > 0)
                    t = this.templatesStack.Peek();
                currentTemplate = t;
                string tmpltName = (t != null ? t.Name : null);
                if (this.OnGetMacroValue != null)
                    isOk = this.OnGetMacroValue(tmpltName, this.templatesStack.Count, pPrmName, out v);
                else
                    isOk = this.OnGetMactoValue(tmpltName, this.templatesStack.Count, pPrmName, out v);
                if (isOk)
                    pPrmValue = (v != null ? v.ToString() : null);
            }
            return isOk;
        }

        private string expandMacroValues(string pText)
        {
            StrUtils.ExpandParameters(ref pText, "$(", ")", this.getPrmValue, this);
            return pText;
        }

        public class Template
        {
            public string Name;
            public string Header;
            public Dictionary<string, TemplateItem> Items;
            public string Footer;

            /// <summary>Return 1st item in collection of items</summary>
            public TemplateItem Item
            {
                get
                {
                    TemplateItem item = null;
                    if (this.Items.Count > 0)
                    {
                        foreach (KeyValuePair<string, TemplateItem> it in this.Items)
                        {
                            item = it.Value;
                            break;
                        }
                    }
                    return item;
                }
            }

            public override string ToString()
            {
                return String.Format("Template[{0}]", this.Name);
            }

            public class TemplateItem
            {
                public string Name;
                public string Filter;
                public string Text;

                public override string ToString()
                {
                    return String.Format("TemplateItem[{0}/{1}]", this.Name, this.Filter);
                }
            }
        }

        private Template currentTemplate = null;
        private Stack<Template> templatesStack = null;
        private Dictionary<string, Template> templates = null;
        private int itemIndex;
        private Dictionary<string, Dictionary<string, string>> lopCache = new Dictionary<string,Dictionary<string,string>>();

        #endregion // Implementation Details
    }
}
