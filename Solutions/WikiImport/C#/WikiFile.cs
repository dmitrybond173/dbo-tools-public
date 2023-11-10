/*
 * Simple/stright-forward solution to parse MediaWiki export file and try to import it via WebDriver
 *
 * MediaWiki export file parser
 *
 * by Dmitry Bond. (November 2023)
 *
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using XService.Utils;

namespace ImportWiki
{
    public class WikiFile
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("TraceLevel", "TraceLevel");

        public WikiFile(string pFilename)
        {
            this.Pages = new List<Page>();

            this.SrcFile = new FileInfo(pFilename);
            if (!this.SrcFile.Exists)
                throw new Exception(string.Format("File [{0}] is not found!", pFilename));

            this.Dom = new XmlDocument();
            this.Dom.Load(this.SrcFile.FullName);
            if (this.Dom.DocumentElement == null)
                throw new Exception(string.Format("Invalid XML file [{0}]!", pFilename));

            this.namespaceManager = new XmlNamespaceManager(this.Dom.NameTable);
            scanForNamespaceRefs(this.Dom.DocumentElement);
        }

        public XmlDocument Dom { get; protected set; }
        public FileInfo SrcFile { get; protected set; }
        public List<Page> Pages { get; protected set; }

        public void Parse()
        {
            if (this.Dom == null || this.namespaceManager == null)
                throw new Exception("Nothing to parse!");

            parse();
        }

        #region Implementation details

        private void parse()
        {            
            // use explicit "def:" prefix to match default XML namespace...
            XmlNodeList pagesNodes = this.Dom.SelectNodes("/def:mediawiki/def:page", this.namespaceManager);
            if (pagesNodes == null)
                throw new Exception("Nothing to parse!");

            foreach (XmlNode node in pagesNodes)
            {
                Page pg = Page.Load(this, (XmlElement)node);
                if (pg != null)
                    this.Pages.Add(pg);
            }
        }

        private void scanForNamespaceRefs(XmlElement pDomNode)
        {
            foreach (XmlNode attr in pDomNode.Attributes)
            {
                if (attr.Name.StartsWith("xmlns") || attr.Name.Length != attr.LocalName.Length)
                {
                    string id = attr.LocalName;
                    // Note: "xml" namespace is reserved thing, so it has no sense to add
                    // if (StrUtils.IsSameText(id, "xml")) { continue; }
                    // Note: it is impossible to add "xmlns" namespace! This is reserved name, so it throws an exception on attempt to add.
                    if (string.IsNullOrEmpty(id) || StrUtils.IsSameText(id, "xmlns")) id = "def";
                    string s;
                    if (this.nsCache.TryGetValue(id, out s)) { continue; }
                    Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format("+ <{0}>: add NS[{1}] = {2}", pDomNode.Name, id, attr.Value) : "");
                    this.namespaceManager.AddNamespace(id, attr.Value);
                    this.nsCache[id] = attr.Value;
                }
            }

            foreach (XmlNode node in pDomNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                scanForNamespaceRefs((XmlElement)node);
            }
        }

        private Dictionary<string, string> nsCache = new Dictionary<string, string>();
        private XmlNamespaceManager namespaceManager;

        #endregion

        public class Page
        {
            public static Page Load(WikiFile pOwner, XmlElement pDomNode)
            {
                Page result = new Page(pOwner, pDomNode);
                foreach (XmlNode node in pDomNode.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;

                    XmlElement element = (XmlElement)node;
                    if (StrUtils.IsSameText(element.Name, "title"))
                        result.Title = XmlUtils.LoadText(element);
                    else if (StrUtils.IsSameText(element.Name, "id"))
                        result.ID = XmlUtils.LoadText(element);
                    else if (StrUtils.IsSameText(element.Name, "revision"))
                    {
                        PageRevision rv = PageRevision.Load(result, element);
                        if (rv != null)
                            result.Revisions.Add(rv);
                    }                        
                }
                return result;
            }

            protected Page(WikiFile pOwner, XmlElement pDomNode)
            {
                this.Owner = pOwner;
                this.Node = pDomNode;
                this.Revisions = new List<PageRevision>();
                this.ID = string.Empty;
                this.Title = string.Empty;
            }

            public override string ToString()
            {
                return String.Format("Page[{0}; title={1}]: {2} revisions", this.ID, this.Title, this.Revisions.Count);
            }

            public WikiFile Owner { get; protected set; }
            public XmlElement Node { get; protected set; }
            public string Title { get; protected set; }
            public string ID { get; protected set; }
            public List<PageRevision> Revisions { get; protected set; }

            public PageRevision LatestRevision { get { return this.Revisions[this.Revisions.Count - 1]; } }
        }

        public class PageRevision
        {
            public static PageRevision Load(Page pOwner, XmlElement pDomNode)
            {
                PageRevision result = new PageRevision(pOwner, pDomNode);
                foreach (XmlNode node in pDomNode.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;

                    XmlElement element = (XmlElement)node;
                    if (StrUtils.IsSameText(element.Name, "id"))
                        result.ID = XmlUtils.LoadText(element);
                    else if (StrUtils.IsSameText(element.Name, "timestamp"))
                        result.Timestamp = XmlUtils.LoadText(element);
                    else if (StrUtils.IsSameText(element.Name, "text"))
                        result.Text = XmlUtils.LoadText(element);                
                }
                return result;
            }

            protected PageRevision(Page pOwner, XmlElement pDomNode) 
            {
                this.Owner = pOwner;
                this.Node = pDomNode;
                this.ID = string.Empty;
                this.Timestamp = string.Empty;
                this.Text = string.Empty;
            }

            public override string ToString()
            {
                return String.Format("PageRev[{0}; ts={1}]: {2} chars in text", this.ID, this.Timestamp, this.Text.Length);
            }

            public Page Owner { get; protected set; }
            public XmlElement Node { get; protected set; }
            public string Timestamp { get; protected set; }
            public string ID { get; protected set; }
            public string Text { get; protected set; }
        }
    }
}
