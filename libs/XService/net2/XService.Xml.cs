/*
 * Some simple utilities to facilitate programming with XML usage in .NET.
 * Written by Dmitry Bond. at Apr 21, 2007
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;
using XService.Utils;
using XService.Security;

namespace XService
{
    /// <summary>
    /// Container for routinues related to DOM/XML processing
    /// </summary>
    public class XmlUtils
    {
        /// <summary>
        /// Massive assign of XML element attributes
        /// </summary>
        /// <param name="pNode">Source XML element to set attributes for it</param>
        /// <param name="pAttrDefs">Text defining a set of attributes to assign and values. Text format expected to be like this: {attrName}={value}{;}{attrName}={value}{;}. Example: "attr1=123;attr2=test;attr3=abc;"</param>
        public static void SetAttributes(XmlElement pNode, string pAttrDefs)
        {
            string[] defs = pAttrDefs.Split(';');
            foreach (string def in defs)
            {
                string an = def;
                string av = "";

                int p = def.IndexOf('=');
                if (p >= 0)
                {
                    an = def.Substring(0, p).Trim(StrUtils.CH_SPACES);
                    av = def.Substring(p + 1, def.Length - p - 1).Trim(StrUtils.CH_SPACES);
                }

                pNode.SetAttribute(an, av);
            }
        }

        /// <summary>
        /// Returns string describing a path to specified XML element 
        /// </summary>
        /// <param name="pNode">Source XML element to returns path for</param>
        /// <returns>A "/"-separated list of all parent nodes for specified XML element</returns>
        public static string GetNodePath(XmlElement pNode)
        {
            string path = "";
            do
            {
                if (pNode != null)
                {
                    if (path != "") path = path.Insert(0, pNode.Name + "/");
                    else path = pNode.Name;
                }
                pNode = (pNode.ParentNode != null && pNode.ParentNode is XmlElement) ? pNode.ParentNode as XmlElement : null;
            }
            while (pNode != null);
            return path;
        }

        /// <summary>Search for child element which have specified value in specified attribute</summary>
        /// <param name="pRootNode">Root DOM node with childs of what it should do a search</param>
        /// <param name="pNodeName">Name of element to search within. If null-or-empty then we search with all elements</param>
        /// <param name="pAttrName">Name of attribute to search</param>
        /// <param name="pAttrValue">Value of attribute to match</param>
        /// <param name="pRecursive">If it should do recursive search within childs of childs</param>
        /// <returns>Returns 1st found occurence of XML element which match specified conditions</returns>
        public static XmlElement FindElementByAttrValue(XmlElement pRootNode, string pNodeName, string pAttrName, string pAttrValue, bool pRecursive)
        {
            foreach (XmlNode node in pRootNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (!string.IsNullOrEmpty(pNodeName))
                    if (string.Compare(pNodeName, node.Name) != 0) continue;

                XmlElement element = (node as XmlElement);
                XmlNode attr = element.Attributes.GetNamedItem(pAttrName);
                if (attr != null)
                    if (string.Compare(attr.Value, pAttrValue) == 0)
                        return element;
            }
            if (pRecursive)
            {
                foreach (XmlNode node in pRootNode.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;
                    if (!node.HasChildNodes) continue;

                    XmlElement element = FindElementByAttrValue(node as XmlElement, pNodeName, pAttrName, pAttrValue, pRecursive);
                    if (element != null)
                        return element;
                }
            }
            return null;
        }

        /// <summary>Search for child element with specified name</summary>
        /// <param name="pRootNode">Root DOM node with childs of what it should do a search</param>
        /// <param name="pNodeName">Name of element to search</param>
        /// <param name="pRecursive">If it should do recursive search within childs of childs</param>
        /// <returns>Returns 1st found occurence of XML element with specified name</returns>
        public static XmlElement FindElement(XmlElement pRootNode, string pNodeName, bool pRecursive)
        {
            foreach (XmlNode node in pRootNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;

                XmlElement element = (node as XmlElement);
                if (string.Compare(pNodeName, element.Name) == 0)
                    return element;
            }
            if (pRecursive)
            {
                foreach (XmlNode node in pRootNode.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;
                    if (!node.HasChildNodes) continue;

                    XmlElement element = FindElement(node as XmlElement, pNodeName, pRecursive);
                    if (element != null)
                        return element;
                }
            }
            return null;
        }

        /// <summary>Find parent node with specified name</summary>
        /// <param name="pNode">Node to search parent node for</param>
        /// <param name="pParentNodeName">Name of parent node to search</param>
        /// <param name="pIgnoreCase">If need to use case-insensitive search</param>
        /// <returns>Parent node of specified name when found or null when not found</returns>
        public static XmlElement FindParentNode(XmlElement pNode, string pParentNodeName, bool pIgnoreCase)
        {
            string path = pNode.Name;
            if (pIgnoreCase)
                pParentNodeName = pParentNodeName.ToLower();
            while (pNode.ParentNode != null && !(pNode.ParentNode is XmlDocument))
            {
                bool isMatch = (pIgnoreCase 
                    ? (pParentNodeName.CompareTo(pNode.Name.ToLower()) == 0)
                    : (pParentNodeName.CompareTo(pNode.Name) == 0)
                    );
                if (isMatch) 
                    return pNode;
                pNode = (XmlElement)pNode.ParentNode;
            }
            return pNode;
        }

        /// <summary>Remove all kinds of child nodes from specified node</summary>
        public static void CleanupNode(XmlElement pNode)
        {
            while (pNode.ChildNodes.Count > 0)
                pNode.RemoveChild(pNode.ChildNodes[0]);

            while (pNode.Attributes.Count > 0)
                pNode.Attributes.RemoveAt(0);
        }

        /// <summary>Load text from specified node, it could be value from Text-typed node or from CDATA-typed node - all of such will be concatenated</summary>
        public static string LoadText(XmlElement pNode)
        {
            string text = "";
            foreach (XmlNode node in pNode.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.CDATA || node.NodeType == XmlNodeType.Text)
                    text += node.Value;
            }
            return text;
        }

        /// <summary>Search for attribute with specified name (case-sensitive or case-insensitive)</summary>
        public static XmlAttribute FindAttribute(XmlElement pNode, string pAttrName, bool pIgnoreCase)
        {
            if (pIgnoreCase)
            {
                foreach (XmlNode attr in pNode.Attributes)
                {
                    if (string.Compare(attr.Name, pAttrName, true) == 0)
                        return (attr as XmlAttribute);
                }
                return null;
            }
            else
                return (pNode.Attributes.GetNamedItem(pAttrName) as XmlAttribute);
        }

        /// <summary>Load all attributes from specified node into Dictionary<> container</summary>
        public static int LoadAttributes(Dictionary<string, string> pTargetList, XmlElement pNode, bool pForceLowerCase)
        {
            int savedCnt = pTargetList.Count;
            foreach (XmlNode attr in pNode.Attributes)
            {
                pTargetList[pForceLowerCase ? attr.Name.ToLower() : attr.Name] = attr.Value.TrimEnd();
            }
            return pTargetList.Count - savedCnt;
        }

        /// <summary>Load parameters from specified node into Dictionary<> container. It supports text-style and XML-style parameters</summary>
        /// <example>
        ///   <parameters [ format="text" ]>
        ///      Prm1 = value1
        ///      Prm2 = value2
        ///      Prm3 = value3
        ///      <add key="Prm4" value="Value4" />
        ///      <add key="Prm5">Value5</add>
        ///      <add key="Prm6"><![CDATA[Value6]]></add>
        ///      Prm7 = value7
        ///   </parameters>
        /// </example>
        /// <returns>Returns number of new parameters added to specified container</returns>
        public static int LoadParameters(Dictionary<string, string> pTargetContainer, XmlElement pSectionNode, bool pForceLowerCase)
        {
            int savedCnt = pTargetContainer.Count;
            string fmt = pSectionNode.GetAttribute("format");
            bool isForceTextFormat = (!string.IsNullOrEmpty(fmt) && fmt.ToLower() == "text");
            foreach (XmlNode itemNode in pSectionNode.ChildNodes)
            {
                // XML-styled parameters only allowed when it is not a force-text format of section
                if (!isForceTextFormat && itemNode.NodeType == XmlNodeType.Element)
                {
                    string k = ((XmlElement)itemNode).GetAttribute("key");
                    if (string.IsNullOrEmpty(k)) continue;
                    string v = null;
                    XmlNode attr = ((XmlElement)itemNode).GetAttributeNode("value");
                    if (attr == null)
                        v = XmlUtils.LoadText((XmlElement)itemNode);
                    else
                        v = attr.Value;
                    pTargetContainer[pForceLowerCase ? k.ToLower() : k] = v;
                }
                else if (itemNode.NodeType == XmlNodeType.Text || itemNode.NodeType == XmlNodeType.CDATA)
                {
                    string txt = itemNode.Value.Trim(StrUtils.CH_SPACES);
                    StrUtils.ParseIniValuesEx(txt, pTargetContainer, pForceLowerCase);
                }
            }
            return pTargetContainer.Count - savedCnt;
        }

        /// <summary>Method prototype to be used by IterateDomNodes method</summary>
        /// <param name="pDomNode">Current DOM node</param>
        /// <param name="pContext">Context object</param>
        /// <returns>Returns false to stop iterating</returns>
        public delegate bool DomNodeIteratorMethod(XmlNode pDomNode, object pContext);

        /// <summary>Iterate over child nodes within specified DOM node and call specified method for each node (including attributes)</summary>
        /// <param name="pDomNode">DOM node to perform iteration with childs of</param>
        /// <param name="pMethod">Method to call</param>
        /// <param name="pContext">Context object to pass to method</param>
		public static void IterateDomNodes(XmlNode pDomNode, DomNodeIteratorMethod pMethod, object pContext)
		{ 
			foreach (XmlNode node in pDomNode.Attributes)
			{
				if (!pMethod(node, pContext))
					return ;
			}
			if (pDomNode.HasChildNodes)
			{
				foreach (XmlNode node in pDomNode.ChildNodes)
				{
					IterateDomNodes(node, pMethod, pContext);
				}
			}
		}

        /// <summary>
        /// Get specified CONFIG section and load items into NameTable for specified XmlDocument.
        /// </summary>
        /// <param name="pDom">XmlDocument object to load items into NameTable for it</param>
        /// <param name="pConfigSectionName">Name of CONFIG section to load items from</param>
        /// <returns>Returns true when some items loaded; otheriwse false</returns>
        public static bool LoadNameTableFromConfig(XmlDocument pDom, string pConfigSectionName)
        {
            object obj = ConfigurationManager.GetSection(pConfigSectionName);
            if (obj == null || !(obj is Dictionary<string, string>))
                return false;

            int counter = 0;
            Dictionary<string, string> section = (Dictionary<string, string>)obj;
            foreach (KeyValuePair<string, string> n in section)
            {
                pDom.NameTable.Add(n.Key);
                counter++;
            }
            return (counter > 0);
        }

        /// <summary>Calculates hierarchy level of specified DOM node within DOM document</summary>
        /// <param name="pNode">DOM node to calculate hierarchy level for</param>
        /// <returns>Calculated hierarchy level</returns>
        public static int GetNodeLevel(XmlNode pNode)
        {
            int lvl = 0;
            while (pNode.ParentNode != null && !(pNode.ParentNode is XmlDocument))
            {
                pNode = pNode.ParentNode;
                lvl++;
            }
            return lvl;
        }

        /// <summary>Get parent node of specified DOM node, it also works for XmlAttribute</summary>
        /// <param name="pNode">DOM node to obtain parent node for</param>
        /// <returns>Parent node of specified node</returns>
        public static XmlNode GetParentNode(XmlNode pNode)
        {
            XmlNode p = pNode.ParentNode;
            if (pNode.NodeType == XmlNodeType.Attribute)
                p = ((XmlAttribute)pNode).OwnerElement;
            return p;
        }

        /// <summary>Calculates hierarchy path of specified DOM node within DOM document</summary>
        /// <param name="pNode">DOM node to calculate hierarchy path for</param>
        /// <returns>Calculated hierarchy path</returns>
        public static string GetNodePath(XmlNode pNode)
        {
            string path = pNode.Name;
            if (pNode.NodeType == XmlNodeType.Attribute)
                path = "@" + path;
            while (GetParentNode(pNode) != null && !(pNode.ParentNode is XmlDocument))
            {
                pNode = GetParentNode(pNode);
                string name = pNode.Name;
                if (pNode.NodeType == XmlNodeType.Attribute)
                    name = "@" + name;
                path = path.Insert(0, name + "/");
            }
            return path;
        }

        /// <summary>Delegate to be used by XML iterator</summary>
        /// <param name="pNode">Current node</param>
        /// <param name="pContext">Context</param>
        /// <returns>Returns true to continue iterating over XML nodes, returns false to stop</returns>
        public delegate bool XmlIteratorMethod(XmlNode pNode, object pContext);

        /// <summary>Iterator to loop over XML nodes</summary>
        /// <param name="pDomNode">DOM node to start iterating from</param>
        /// <param name="pMethod">Method to call for each node</param>
        /// <param name="pContext">Context object to pass into delegate method</param>
        /// <returns>Returns true when it was processed all nodes, returns false when it stop</returns>
        public static bool IterateXml(XmlElement pDomNode, XmlIteratorMethod pMethod, object pContext)
        {
            // first need to iterate over attributes
            foreach (XmlNode attr in pDomNode.Attributes)
            {
                bool result = pMethod(attr, pContext);
                if (!result)
                    return result;
            }

            // iterate over child nodes
            foreach (XmlNode node in pDomNode.ChildNodes)
            {
                bool result = pMethod(node, pContext);
                if (!result)
                    return result;

                if (node.NodeType == XmlNodeType.Element && node.HasChildNodes)
                {
                    result = IterateXml((XmlElement)node, pMethod, pContext);
                    if (!result)
                        return result;
                }
            }

            return true;
        }

        /// <summary>Calculate CRC32 of specified XML element</summary>
        /// <param name="pDomNode">XML element to calculate CRC32 of it</param>
        /// <returns>CRC32 value (CRC value is XOR'ed with -1)</returns>
        public static uint CrcOfElement(XmlElement pDomNode)
        { 
            string savedCrcStr = null;
            XmlNode attr = pDomNode.Attributes.GetNamedItem("crc");
            if (attr != null)
            {
                savedCrcStr = attr.Value;
                pDomNode.Attributes.RemoveNamedItem("crc");
            }

            string txt = pDomNode.OuterXml;
            StringBuilder sb = new StringBuilder(txt.Length);
            for (int iCh = 0; iCh < txt.Length; iCh++)
            {
                char ch = txt[iCh];
                bool isSpcChar = (ch == ' ' || (ch >= 0 && ch <= 31));
                if (!isSpcChar)
                {
                    sb.Append(ch);
                }
            }
            txt = sb.ToString();

            uint crc = SecurityUtils.CalculateCRC32(0, txt);
            crc ^= 0xFFFFFFFF;
            
            if (savedCrcStr != null)
            {
                pDomNode.SetAttribute("crc", savedCrcStr);
            }

            return crc;
        }

        /// <summary>Calculate CRC32 of current app config</summary>
        /// <param name="pDomNode">App config object to calculate CRC32 for it</param>
        /// <returns>CRC32 value (CRC value is XOR'ed with -1)</returns>
        public static uint CrcOfAppConfig(System.Configuration.Configuration pConfig)
        {
            if (!pConfig.HasFile) return 0;
            if (!File.Exists(pConfig.FilePath)) return 0;

            XmlDocument dom = new XmlDocument();
            dom.Load(pConfig.FilePath);
            XmlNodeList list = dom.SelectNodes(string.Format("/configuration/appSettings/add[@key=\'{0}\']", NAME_ConfigCRC));
            if (list != null)
            {
                foreach (XmlNode node in list)
                {
                    node.ParentNode.RemoveChild(node);
                }
            }

            uint crcValue = XmlUtils.CrcOfElement(dom.DocumentElement);
            return crcValue;
        }

        public static string NAME_ConfigCRC = "CONFIG-CRC";

        /// <summary>Search specified DOM node in node list, return its index or -1 when not found</summary>
        public static int IndexOfNode(XmlNodeList pList, XmlNode pNode)
        {
            for (int i = 0; i < pList.Count; i++)
            { 
                if (pList[i] == pNode)
                    return i;
            }
            return -1;
        }

        /// <summary>Write specified DOM document into memory stream using default text encoding</summary>
        public static MemoryStream DomToMemoryStream(XmlDocument dom)
        {
            MemoryStream strm = new MemoryStream(0x8000);
            XmlWriter writer = new XmlTextWriter(strm, Encoding.Default);
            dom.WriteTo(writer);
            writer.Flush();
            writer = null;
            strm.Seek(0, SeekOrigin.Begin);
            return strm;
        }

        /// <summary>Transform specified XmlDocument using specified XSLT file</summary>
        /// <param name="pDom">XmlDocument to transform</param>
        /// <param name="pXsltFilename">XSLT filename</param>
        /// <param name="result">Output of XSLT transformation</param>
        public static void TransformXml(XmlDocument pDom, string pXsltFilename, out string result)
        {
            result = "";
            XPathDocument xdoc = null;
            XslCompiledTransform xslt = null;
            StringBuilder text = new StringBuilder(0x10000);
            using (StringWriter writer = new StringWriter(text))
            {
                xdoc = new XPathDocument(DomToMemoryStream(pDom));
                xslt = new XslCompiledTransform();
                xslt.Load(pXsltFilename);
                xslt.Transform(xdoc, null, writer);
                writer.Flush();
                result = writer.ToString();
            }
        }

        /// <summary>Transform specified XmlDocument using specified XSLT file</summary>
        /// <param name="pDom">XmlDocument to transform</param>
        /// <param name="pXsltFilename">XSLT filename</param>
        /// <param name="pOutputFilename">Filename where to save output of XSLT transformation</param>
        public static void TransformXml(XmlDocument pDom, string pXsltFilename, string pOutputFilename)
        {
            string result;
            TransformXml(pDom, pXsltFilename, out result);
            using (StreamWriter sw = File.CreateText(pOutputFilename))
            {
                sw.Write(result);
            }
        }
    }
}

namespace XService.Utils
{
	public class XmlUtils : XService.XmlUtils
	{
	}
}
