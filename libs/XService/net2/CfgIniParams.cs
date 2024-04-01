/*
 * IniParameters.
 * A class to load a config section which is describing configuration parameters in a ini-file-like format.
 * 
 * Written by Dmitry Bond. at Apr 2012.
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Xml;

namespace XService.Configuration
{
    /// <summary>
    /// IniParameters
    /// To load a config section which is describing configuration parameters in a ini-file-like format.
    ///   <configSections>
    ///     <section name="RuntimeParameters" type="XService.Configuration.IniParameters, XService.Net2" />
    ///   </configSections>
    /// </summary>
    public class IniParameters : IConfigurationSectionHandler
    {
        private string typeDefs = "";
        public string TypeDefs { get { return this.typeDefs; } }

        public object Create(object parent, object configContext, XmlNode section)
        {
            string text = "";

            bool isCaseSensitive = false;
            XmlNode attr = section.Attributes.GetNamedItem("caseSensitive");
            if (attr != null)
            {
                isCaseSensitive = Convert.ToBoolean(attr.Value);
            }

            foreach (XmlNode node in section.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.CDATA || node.NodeType == XmlNodeType.Text)
                {
                    text += (node.Value + Environment.NewLine);
                }
            }

            Dictionary<string, string> values = new Dictionary<string, string>();
            text = text.Replace('\r', '\n').Replace("\n\n", "\n");
            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                string s = line.Trim(CH_SPACES);

                // skip empty lines
                if (string.IsNullOrEmpty(s)) continue;

                // skip comment lines
                if (s.StartsWith("#") || s.StartsWith(";") || s.StartsWith("//")) continue;

                string pn, pv = null;
                int p = line.IndexOf('=');
                if (p >= 0)
                {
                    pn = line.Substring(0, p).Trim(CH_SPACES);
                    pv = line.Remove(0, p + 1).Trim(CH_SPACES);
                }
                else
                    pn = s;

                values[isCaseSensitive  ? pn : pn.ToLower()] = pv;
            }
            return values;
        }

        private static char[] CH_SPACES = " \t\r\n".ToCharArray();
    }
}
