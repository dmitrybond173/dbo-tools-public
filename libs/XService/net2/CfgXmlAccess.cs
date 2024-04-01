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
    /// ConfigXml 
    /// To access XML of a config section directly.
    /// <example>
    ///   <configSections>
    ///     <section name="TestCaseDescriptor" type="XService.Configuration.ConfigXml, XService.Net2" />
    ///   </configSections>
    ///   <TestCaseDescriptor>
    ///     <node1 attr="123">some text</node1>
    ///     <node2>
    ///       <node3>some other text</node3>
    ///     </node2>
    ///   </TestCaseDescriptor>
    /// // Then in a code you can do something like this:
    ///   XmlElement cfgSection = (XmlElement)ConfigurationManager.GetSection("TestCaseDescriptor");
    ///   if (cfgSection != null)
    ///   {
    ///     // ... here you can directly access DOM/XML section in config file ...
    ///   }
    /// </example>
    /// </summary>
    public class ConfigXml : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return section;
        }
    }
}
