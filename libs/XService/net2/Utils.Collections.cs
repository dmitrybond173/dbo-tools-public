using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;

namespace XService.Utils
{
    /// <summary>
    /// StrUtils - utilities to manipulate strings.
    /// </summary>
    public sealed class CollectionUtils
    {
        public static string Join(Array list, string delimiter)
        {
            StringBuilder sb = new StringBuilder(list.Length * 16);
            foreach (object item in list)
            {
                if (sb.Length > 0) sb.Append(delimiter);
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Search specified key in specified collection.
        /// </summary>
        /// <param name="coll">Collection to search key in.</param>
        /// <param name="key">Key to search.</param>
        /// <param name="ignoreCase">If true search is case-insensitive.</param>
        /// <returns>Returns true if specified key found in collection.</returns>
        public static bool IsContains(NameValueCollection coll, string key, bool ignoreCase)
        {
            bool qMatch;
            string[] keys = coll.AllKeys;
            foreach (string k in keys)
            {
                if (ignoreCase)
                    qMatch = (k.ToLower() == key.ToLower());
                else
                    qMatch = (k == key);
                if (qMatch)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Parse string of format "key1=value1; key2=value2; ... keyN=valueN;"
        /// To split key and value instead of '=' you could also use ':'.
        /// </summary>
        /// <param name="parameters">String to parse</param>
        /// <param name="forceLowerCaseKeys">If true all key names will be lowercased</param>
        /// <returns>Hastable object that contains parsed parameters.</returns>
        private static char[] nv_delims = new char[] { '=', ':' };
        public static Hashtable ParseParametersStr(string parameters, bool forceLowercaseKeys)
        {
            Hashtable result = new Hashtable();
            string[] items = parameters.Split(';');
            foreach (string item in items)
            {
                // skip empty items
                if (item.Trim() == string.Empty) continue;

                // search name-value delimiter
                int p = item.IndexOfAny(nv_delims);
                string key = "";
                string val = "";
                if (p >= 0) // if found ...
                {
                    key = item.Substring(0, p).Trim();
                    val = item.Remove(0, p + 1);
                }
                else
                    key = item;

                // put new item to collection
                if (forceLowercaseKeys) key = key.ToLower();
                result[key] = val;
            }
            return result;
        }

    } /* CollectionUtils */

}
