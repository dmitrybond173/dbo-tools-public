/*
 * Simple engine for LicenseKey validation.
 * Written by Dmitry Bond. at July 5, 2012
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml;
using System.Text.RegularExpressions;
using XService.Utils;
using XService.Security;
using System.Reflection;

namespace XService.Security
{
    /// <summary>
    /// The LicenseKey is used to validate LicenseKey value over the current system to see if application is allowed to run on current system.
    /// </summary>
    public class LicenseKey
    {
        // restored from Reflector
        protected static List<string> _validSetsOfLicenseProps;
        public const string PREFIX_License = "license:";

        // restored from Reflector
        public static List<string> ValidSetsOfLicenseProps
        {
            get
            {
                if (_validSetsOfLicenseProps == null)
                {
                    _validSetsOfLicenseProps = new List<string>();
                }
                return _validSetsOfLicenseProps;
            }
        }

        public static bool Check(string pLicenseKeyValue, Dictionary<string, string> pExtraProps, out List<string> pErrorMessaes)
        {
            LicenseKey key = new LicenseKey() {
                Value = pLicenseKeyValue,
                ExtraProperties = pExtraProps
            };
            bool isOk = key.Validate();
            //if (isOk) CollectionUtils.Merge(pExtraProps, key.ExtraProperties, true);
            pErrorMessaes = new List<string>();
            pErrorMessaes.AddRange(key.ErrorMessages);
            return isOk;
        }

        public LicenseKey()
        {
            this.LicenseProperties = new Dictionary<string, string>();
            this.ErrorMessages = new List<string>();
            this.VersionProps = new Dictionary<string, string>();
        }

        /// <summary>
        /// LicenseKey to validate over current system
        /// </summary>
        public string Value { get; set; }

        public Dictionary<string, string> VersionProps { get; protected set; }

        /// <summary>
        /// Extra properties of current system that could match over the LicenseKey properties
        /// </summary>
        public Dictionary<string, string> ExtraProperties { get; set; }

        /// <summary>
        /// Decoded LicenseKey properties - the set of properties that identify a valid system where application is allowed to run
        /// </summary>
        public Dictionary<string, string> LicenseProperties { get; protected set; }

        /// <summary>
        /// List of LicenseKey validation errors
        /// </summary>
        public List<string> ErrorMessages { get; protected set; }

        /// <summary>
        /// Validate LicenseKey value over the current system.
        /// List of predefined license property names: Dummy, Computer, StartDate, ExpireDate, WindowsUser, IpAddress, MacAddress.
        /// Assume that any other license properties should be supplied in the pExtraProperties parameter for LicenseKey.Check static method.
        /// Note: license property name may have special prefixes - '#' to comment/disable property, '?' - to mark property as optional.
        /// If license property is marked as 'optional' it means that - if such property is not found on current system it will not be valdiated over the specified license-key.
        /// Predefined license properties cannot be marked as 'optional'.
        /// </summary>
        /// <returns>Returns true is validation was successfull. Could throw a LicenseKeyError exception in case if it cannot validate LicenseKey.</returns>
        public bool Validate()
        {
            bool isOk = false;

            this.ErrorMessages.Clear();

            if (string.IsNullOrEmpty(this.Value))
                throw new LicenseKeyError("Invalid LicenseKey");

            PasswordService.COMPATIBILITY_MODE = false;
            try
            {
                string licensePropsStr = PasswordService.RoundedDecryptPassword(this.Value, null);

                CollectionUtils.ParseParametersStrEx(this.LicenseProperties, licensePropsStr, true, ';', "=:");
                if (this.LicenseProperties.Count == 0)
                    throw new LicenseKeyError("Invalid LicenseKey");
            }
            catch (Exception exc)
            {
                this.ErrorMessages.Add("Fail to decode LicenseKey");
                throw new LicenseKeyError("Invalid LicenseKey", exc);
            }

            Dictionary<string, string> adjustedExtraProps = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> prop in this.ExtraProperties)
            {
                adjustedExtraProps[prop.Key.ToLower()] = prop.Value;
            }

            int countMatch = 0, countNotMatch = 0;
            foreach (KeyValuePair<string, string> prop in this.LicenseProperties)
            {
                bool isMatch = false;

                string propName = prop.Key.ToLower();
                bool isComment = propName.StartsWith("#");
                if (isComment) continue;

                bool isOptional = propName.StartsWith("?");
                if (isOptional)
                    propName = propName.Remove(0, 1);

                string expectedPropValue = prop.Value;

                switch (propName)
                {
                    case "dummy": isMatch = true; break;

                    case "product":
                        isMatch = IsMatchVerInfo(typeof(AssemblyProductAttribute), expectedPropValue);
                        if (!isMatch)
                            this.ErrorMessages.Add("This license for not for this product!");
                        break;

                    case "productversion":
                        isMatch = this.IsMatchVersionProp("productversion", expectedPropValue);
                        if (!isMatch)
                            this.ErrorMessages.Add("This license for not for this product version!");
                        break;

                    case "version":
                        isMatch = this.IsMatchVersionProp("version", expectedPropValue);
                        if (!isMatch)
                            this.ErrorMessages.Add("This license for not for this version!");
                        break;

                    case "fileversion":
                        isMatch = this.IsMatchVersionProp("fileversion", expectedPropValue);
                        if (!isMatch)
                            this.ErrorMessages.Add("This license for not for this version!");
                        break;

                    case "app":
                        isMatch = this.IsMatchLicenseProp(expectedPropValue, TypeUtils.ApplicationName);
                        if (!isMatch)
                            this.ErrorMessages.Add("This license for not for this application!");
                        break;

                    case "computer":
                        isMatch = IsMatchLicenseProp(expectedPropValue, Environment.MachineName);
                        if (!isMatch) 
                            this.ErrorMessages.Add("Is not licensed to run on this computer!");
                        break;

                    case "startdate":
                        {
                            string ts1 = StrUtils.StripNskTimestamp(expectedPropValue);
                            string ts2 = StrUtils.StripNskTimestamp(StrUtils.NskTimestampOf(DateTime.Now));
                            isMatch = (ts2.CompareTo(ts1) >= 0);
                            if (!isMatch) 
                                this.ErrorMessages.Add("License start date is above of current date!");
                        }
                        break;

                    case "expiredate":
                        {
                            string ts1 = StrUtils.StripNskTimestamp(expectedPropValue);
                            string ts2 = StrUtils.StripNskTimestamp(StrUtils.NskTimestampOf(DateTime.Now));
                            isMatch = (ts2.CompareTo(ts1) <= 0);
                            if (!isMatch) 
                                this.ErrorMessages.Add("License key expired!");
                        }
                        break;

                    case "osuser":
                    case "windowsuser":
                        isMatch = IsMatchLicenseProp(expectedPropValue, Environment.UserName + "@" + Environment.UserDomainName)
                            || IsMatchLicenseProp(expectedPropValue, Environment.UserDomainName + "\\" + Environment.UserName);
                        if (!isMatch) 
                            this.ErrorMessages.Add("Is not licensed to run by current user!");
                        break;

                    case "ipaddress":
                        isMatch = IsMatchIpAddress(expectedPropValue);
                        if (!isMatch) 
                            this.ErrorMessages.Add("Is not licensed to run for current IP address!");
                        break;

                    case "macaddress":
                        isMatch = IsMatchMacAddress(expectedPropValue);
                        if (!isMatch) 
                            this.ErrorMessages.Add("Is not licensed to run for current MAC address!");
                        break;

                    default:
                        {
                            string currentPropValue;
                            if (adjustedExtraProps.TryGetValue(propName, out currentPropValue))
                            {
                                // if optional property is specified and not match we assume - this is failure!
                                isMatch = IsMatchLicenseProp(expectedPropValue, currentPropValue);
                                if (!isMatch) this.ErrorMessages.Add(string.Format("LicenseKey property ({0}) is not match current system!", prop.Key));
                            }
                            else
                            {
                                this.ErrorMessages.Add(string.Format("{0} LicenseKey property ({1}) is missing.", 
                                    (isOptional ? "Optional" : "Mandatory"), prop.Key));
                                // if optional property is not specified in ExtraProperties we can think - it is ok
                                isMatch = isOptional;
                            }
                            break;
                        }
                }
                if (isMatch) countMatch++;
                else countNotMatch++;

                if (isMatch)
                {
                    this.ExtraProperties["license:" + propName] = expectedPropValue;
                }
            }

            isOk = (countNotMatch == 0 && countMatch > 0);

            return isOk;
        }

        // restored from Reflector
        protected virtual bool CheckValidSetsOfLicenseProps()
        {
            bool isOk;
            List<bool> list = new List<bool>();
            foreach (string str in ValidSetsOfLicenseProps)
            {
                string[] items = str.Split(new char[] { ',' });
                bool? nullable = null;
                foreach (string it in items)
                {
                    string propValue;
                    string key = it.ToLower();
                    isOk = this.ExtraProperties.TryGetValue(key, out propValue) || this.ExtraProperties.TryGetValue("license:" + key, out propValue);
                    if (nullable.HasValue)
                    {
                        bool? nullable2 = nullable;
                        nullable = isOk ? nullable2 : false;
                    }
                    else
                    {
                        nullable = new bool?(isOk);
                    }
                }
                list.Add(nullable.Value);
            }
            isOk = false;
            foreach (bool flag2 in list)
            {
                isOk = isOk || flag2;
            }
            return isOk;
        }

        /// <summary>
        /// Check if specified current property value is match expected property value.
        /// </summary>
        /// <param name="pExpectedPropValue">The expected property value. If started with '/' it will be processed as a regular expression, 
        /// so regular expression will be extracted from 1st '/' until last '/'.</param>
        /// <param name="pCurrentPropValue">The current property value.</param>
        /// <returns>Returns true if current property value is match expected property value.</returns>
        protected virtual bool IsMatchLicenseProp(string pExpectedPropValue, string pCurrentPropValue)
        {
            bool isMatch;

            if (pExpectedPropValue.StartsWith("/"))
            {
                string expr = pExpectedPropValue;
                expr = expr.Remove(0, 1);
                int p = expr.LastIndexOf("/");
                if (p >= 0)
                    expr = expr.Remove(p, expr.Length - p);
                Regex rexp = new Regex(expr, RegexOptions.IgnoreCase);
                isMatch = rexp.IsMatch(pCurrentPropValue);
            }
            if (pExpectedPropValue.IndexOfAny("?*".ToCharArray()) >= 0)
            {
                isMatch = StrUtils.FilespecToRexp(pExpectedPropValue).IsMatch(pCurrentPropValue);
            }
            else
                isMatch = (string.Compare(pExpectedPropValue, pCurrentPropValue, true) == 0);

            return isMatch;
        }

        protected virtual bool IsMatchVerInfo(Type pAttrType, string pExpectedPropValue)
        {
            Assembly asm = Assembly.GetEntryAssembly();
            if (asm == null)
                asm = TypeUtils.ActualAssembly;
            if (asm == null) return false;

            object[] attrs = asm.GetCustomAttributes(false);
            foreach (object attr in attrs)
            {
                if (attr.GetType().IsAssignableFrom(pAttrType))
                {
                    if (pAttrType == typeof(AssemblyProductAttribute))
                    {
                        AssemblyProductAttribute p = (AssemblyProductAttribute)attr;
                        return StrUtils.IsSameText(p.Product, pExpectedPropValue);
                    }
                }
            }
            return false;
        }

        protected virtual bool IsMatchIpAddress(string pExpectedPropValue)
        {
            string nm = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(nm);
            IPAddress[] addresses = ipEntry.AddressList;
            for (int i = 0; i < addresses.Length; i++)
            {
                IPAddress addr = addresses[i];
                bool isMatch = IsMatchLicenseProp(pExpectedPropValue, addr.ToString());
                if (isMatch) break;
            }
            return false;
        }
        
        protected virtual bool IsMatchMacAddress(string pExpectedPropValue)
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    byte[] mac = nic.GetPhysicalAddress().GetAddressBytes();
                    string macStr = "";
                    foreach (byte b in mac)
                    {
                        if (!string.IsNullOrEmpty(macStr)) macStr += "-";
                        macStr += b.ToString("X2");
                    }
                    bool isMatch = IsMatchLicenseProp(pExpectedPropValue, macStr);
                    if (isMatch) break;
                }
            }
            return false;
        }

        // restored from Reflector
        protected virtual bool IsMatchVersionProp(string pPropName, string pExpectedPropValue)
        {
            string str;
            if (this.VersionProps.Count == 0)
            {
                TypeUtils.CollectVersionInfoAttributes(this.VersionProps, TypeUtils.ActualAssembly);
                this.VersionProps["$loaded"] = StrUtils.NskTimestampOf(DateTime.Now);
            }
            if (!this.VersionProps.TryGetValue(pPropName.ToLower(), out str))
            {
                return false;
            }
            return this.IsMatchLicenseProp(pExpectedPropValue, str);
        }

    }
}
