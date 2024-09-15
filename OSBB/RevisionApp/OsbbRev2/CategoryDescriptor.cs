/* 
 * App for OSBB Revision.
 *
 * Handler of Category in Classificator.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Feb, 2024
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using XService.Utils;

namespace OsbbRev2
{
    public class CategoryDescriptor
    {
        public static CategoryDescriptor Load(XmlElement pCfgNode)
        {
            CategoryDescriptor result = new CategoryDescriptor(pCfgNode);
            return result;
        }

        public CategoryDescriptor(XmlElement pCfgNode)
        { 
            this.CfgNode = pCfgNode;
            this.AccountNo = 0;
            this.Patterns = new List<Pattern>();
            this.HasExclusionPattern = false;

            loadConfiguration();
        }

        public override string ToString()
        {
            return String.Format("Category[{0}; {1} patterns]", this.Caption, this.Patterns.Count);
        }

        public XmlElement CfgNode { get; protected set; }

        public string Caption { get; protected set; }
        public int AccountNo { get; protected set; }

        public List<Pattern> Patterns { get; protected set; }
        public bool HasExclusionPattern { get; protected set; }

        public bool IsMatch(DataItem pItem)
        {
            if (this.AccountNo != 0 && pItem.AccountNo != this.AccountNo)
                return false;

            if (this.HasExclusionPattern)
            {
                foreach (Pattern p in this.Patterns)
                {
                    // skip all normal patterns - they will be checked later
                    if (!p.IsExcluded) continue;

                    // check exclusion pattern
                    if (p.IsMatch(pItem))
                        return false;
                }
            }

            foreach (Pattern p in this.Patterns) 
            {
                if (p.IsMatch(pItem))
                {
                    return true;
                }
            }
            return (this.Patterns.Count == 0); // when no patterns - it is always match
        }

        #region Implementation details

        private void loadConfiguration()
        {
            XmlNode attr = AppUtils.RequiredAttr("caption", this.CfgNode);
            if (attr != null)
                this.Caption = attr.Value;

            int n;
            attr = AppUtils.OptionalAttr("accountNo", this.CfgNode);
            if (attr != null && StrUtils.GetAsInt(attr.Value, out n))
                this.AccountNo = n;
            
            foreach (XmlNode node in this.CfgNode.ChildNodes) 
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (StrUtils.IsSameText(node.Name, "Pattern"))
                {
                    Pattern p = Pattern.Load((XmlElement)node);
                    if (p != null)
                    {
                        this.Patterns.Add(p);
                        if (p.IsExcluded)
                            this.HasExclusionPattern = true;
                    }
                }
            }
        }

        #endregion // Implementation details

        public class Pattern
        {
            public static Pattern Load(XmlElement pCfgNode) 
            {
                Pattern result = new Pattern();

                XmlNode attr = pCfgNode.GetAttributeNode("field");
                if (attr != null)
                {
                    try { result.Field = (EField)Enum.Parse(typeof(EField), attr.Value, true); }
                    catch { }
                }

                foreach (XmlNode node in pCfgNode.Attributes)
                {
                    if (StrUtils.IsSameText(node.Name, "exclude"))
                        result.IsExcluded = StrUtils.GetAsBool(node.Value);
                    if (StrUtils.IsSameText(node.Name, "startsWith"))
                        result.StartsWith = node.Value;
                    if (StrUtils.IsSameText(node.Name, "contains"))
                        result.Contains = node.Value;
                    if (StrUtils.IsSameText(node.Name, "endsWith"))
                        result.EndsWith = node.Value;
                    if (StrUtils.IsSameText(node.Name, "regexp") || StrUtils.IsSameText(node.Name, "rexp"))
                        result.RegexExpression = node.Value;
                }
                return result;
            }

            internal Pattern() 
            {
                this.IsExcluded = false;
            }

            public override string ToString()
            {
                string s = string.Format("{0}Pattern[field={1}; ", (this.IsExcluded ? "Not-" : ""), this.Field);
                if (this.StartsWith != null)
                    s += string.Format("starts={0}; ", this.StartsWith);
                if (this.Contains != null)
                    s += string.Format("contains={0}; ", this.Contains);
                if (this.EndsWith != null)
                    s += string.Format("ends={0}; ", this.EndsWith);
                if (this.RegexExpression != null)
                    s += string.Format("rexp={0}; ", this.RegexExpression);
                s += "]";
                return s;
            }

            public enum EField
            { 
                Description,
                CounterParty
            }

            public EField Field = EField.Description;

            public bool IsExcluded { get; protected set; }
            public string StartsWith { get; protected set; }
            public string Contains { get; protected set; }
            public string EndsWith { get; protected set; }
            public string RegexExpression { get; protected set; }

            public Regex Regex 
            {
                get
                { 
                    if (this.regex == null)
                        this.regex = new Regex(this.RegexExpression, 
                            RegexOptions.IgnoreCase 
                            | RegexOptions.CultureInvariant
                            | RegexOptions.Multiline
                            );
                    return this.regex;
                }
            }

            public bool IsMatch(DataItem pItem)
            {
                string text = pItem.Description;
                if (this.Field == EField.CounterParty)
                    text = pItem.CounterParty;

                bool result = false;
                if (this.StartsWith != null)
                {
                    result = text.ToUpper().StartsWith(this.StartsWith.ToUpper());
                }
                if (this.Contains != null)
                {
                    result = text.ToUpper().Contains(this.Contains.ToUpper());
                }
                if (this.EndsWith != null)
                {
                    result = text.ToUpper().EndsWith(this.EndsWith.ToUpper());
                }
                if (this.RegexExpression != null)
                {
                    result = this.Regex.IsMatch(text);
                }
                return result;
            }

            #region Implementation details

            private Regex regex = null;

            #endregion // Implementation details
        }


    }
}
