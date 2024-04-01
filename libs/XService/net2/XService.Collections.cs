/*
 * Simple utlitity classes to work with arrays & collections in .NET.
 * Written by Dmitry Bond. at June 14, 2006
 */

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
        /// <summary>
        /// Concatenate all items in specified Array using specified delimiter. 
        /// </summary>
        /// <param name="pList">Array with items to concatenate</param>
        /// <param name="pDelimiter">Delimiter to use</param>
        /// <returns>String of concatenated array items</returns>
        public static string Join(Array pList, string pDelimiter)
        {
            StringBuilder sb = new StringBuilder(pList.Length * 16);
            foreach (object item in pList)
            {
                if (sb.Length > 0) sb.Append(pDelimiter);
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Concatenate all items in specified list using specified delimiter. 
        /// </summary>
        /// <param name="pList">List with items to concatenate</param>
        /// <param name="pDelimiter">Delimiter to use</param>
        /// <returns>String of concatenated list items</returns>
        public static string Join(IList pList, string pDelimiter)
		{
			StringBuilder sb = new StringBuilder(pList.Count * 16);
			foreach (object item in pList)
			{
				if (sb.Length > 0) sb.Append(pDelimiter);
				sb.Append(item.ToString());
			}
			return sb.ToString();
		}

        /// <summary>
        /// Concatenate all items in specified string[] array using specified delimiter. 
        /// </summary>
        /// <param name="pItems">string[] array with items to concatenate</param>
        /// <param name="pDelimiter">Delimiter to use</param>
        /// <returns>String of concatenated array items</returns>
        public static string Join(string[] pItems, string pDelimiter)
        {
            StringBuilder sb = new StringBuilder(pItems.Length * 16);
            foreach (string item in pItems)
            {
                if (sb.Length > 0)
                    sb.Append(pDelimiter);
                sb.Append(item);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Concatenate all items in specified Dictionary using specified delimiter and key-value separator. 
        /// </summary>
        /// <param name="pItems">Dictionary with items to concatenate</param>
        /// <param name="pDelimiter">Delimiter to use</param>
        /// <param name="pKeyValueSeparator">Separator for key+value</param>
        /// <returns>String of concatenated dictionary items</returns>
        public static string Join(Dictionary<string, string> pItems, string pDelimiter, string pKeyValueSeparator)
        {
            StringBuilder sb = new StringBuilder(pItems.Count * 32);
            foreach (KeyValuePair<string, string> item in pItems)
            {
                if (sb.Length > 0)
                    sb.Append(pDelimiter);
                sb.Append(item.Key + pKeyValueSeparator + item.Value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Concatenate all items in specified Dictionary using specified item-template as a Format-template.
        /// </summary>
        /// <param name="pItems">Dictionary with items to concatenate</param>
        /// <param name="pItemTemplate">The string to be used as a Format-template (is to use use by string.Format(...) method)</param>
        /// <returns>String of concatenated dictionary items</returns>
        public static string JoinFormated(Dictionary<string, string> pItems, string pItemTemplate)
        {
            StringBuilder sb = new StringBuilder(pItems.Count * 32);
            foreach (KeyValuePair<string, string> item in pItems)
            {
                sb.AppendFormat(pItemTemplate, item.Key, item.Value);
            }
            return sb.ToString();
        }

        public static void DisposeObjects(ArrayList AList)
        {
            CommonUtils.DisposeObjects(AList);
        }

        public static void DisposeObjects<T>(List<T> AList)
        {
            CommonUtils.DisposeObjects(AList);
        }

        public static void Merge(Dictionary<string, string> pTargetValues, Dictionary<string, string> pAdditionalValues, bool pForceLowerCaseNames)
        {
            if (pTargetValues == pAdditionalValues) 
                return;
            foreach (KeyValuePair<string, string> prm in pAdditionalValues)
            {
                pTargetValues[pForceLowerCaseNames ? prm.Key.ToLower() : prm.Key] = prm.Value;
            }
        }

        /// <summary>Case-insensitive search of string in a list</summary>
        /// <returns>Returns index of string found or -1 when not found</returns>
        public static int IndexOfCI(List<string> pList, string pItem, int pStartFrom)
        {
            if (pStartFrom < 0)
                pStartFrom = 0;
            for (int i = pStartFrom; i < pList.Count; i++)
            {
                if (StrUtils.IsSameText(pItem, pList[i]))
                    return i;
            }
            return -1;
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
                if (item.Trim(StrUtils.CH_SPACES) == string.Empty) continue;

				// search name-value delimiter
				int p = item.IndexOfAny(nv_delims);
				string key = "";
				string val = "";
				if (p >= 0) // if found ...
				{
					key = item.Substring(0, p).Trim(StrUtils.CH_SPACES);
                    val = item.Remove(0, p + 1);
                    if (StrUtils.AUTO_TRIM_SPACES)
                        val = val.TrimEnd(StrUtils.CH_SPACES);
				}
				else
					key = item;

                bool isNeedCompile = key.StartsWith("!");
                if (isNeedCompile)
                {
                    key = key.Remove(0, 1);
                    val = StrUtils.CompileStr(val, "", true);
                }

				// put new item to collection
				if (forceLowercaseKeys) key = key.ToLower();
				result[key] = val;
			}
			return result;
		}

        public static int ParseParametersStrEx(Dictionary<string, string> pTargetStorage, string pValues, bool pForceLowercaseKeys, char pItemsDelim, string pNameValueDelims)
        {
            int savedCount = pTargetStorage.Count;
            string[] items = pValues.Split(pItemsDelim);
            char[] nvDelims = pNameValueDelims.ToCharArray();
            foreach (string item in items)
            {
                if (item.Trim(StrUtils.CH_SPACES) == string.Empty) continue;

                string sn = item;
                string sv = "";

                int p = item.IndexOfAny(nvDelims);
                if (p >= 0)
                {
                    sn = item.Substring(0, p).Trim(StrUtils.CH_SPACES); // <- for sure need to trim a name!
                    sv = item.Substring(p + 1, item.Length - p - 1); // for sure need to trim spaces in value!
                    if (StrUtils.AUTO_TRIM_SPACES)
                        sv = sv.Trim(StrUtils.CH_SPACES);

                    bool isNeedCompile = sn.StartsWith("!");
                    if (isNeedCompile)
                    {
                        sn = sn.Remove(0, 1);
                        sv = StrUtils.CompileStr(sv, "", true);
                    }
                }

                pTargetStorage[pForceLowercaseKeys ? sn.ToLower() : sn] = sv;
            }
            return (pTargetStorage.Count - savedCount);
        }

        public static Dictionary<string, string> ParseParametersStrEx(string pValues, bool pForceLowercaseKeys)
        {
            Dictionary<string, string> storage = new Dictionary<string, string>();
            ParseParametersStrEx(storage, pValues, pForceLowercaseKeys, ';', "=");
            return storage;
        }

        public delegate string GetParameterValue(string pParamName);

        public static string ReplaceParameters(string pText, string pMarkers, GetParameterValue pGetValue)
        {
            string[] markers = pMarkers.Split(',');
            if (markers.Length < 1) return pText;

            string marker_Start = markers[0];
            string marker_End = markers.Length > 1 ? markers[1] : markers[0];
            // _123456789_123456789_123456789
            // pwd=$(pwd);abc=$(abc);
            int ps = -1, pe = -1;
            int offset = 0;
            do
            {
                ps = pText.IndexOf(marker_Start, offset);
                if (ps < 0) break;

                pe = pText.IndexOf(marker_End, ps + 1);
                if (pe < 0) break;

                string pn = pText.Substring(ps, pe - ps + 1);
                if (!pn.StartsWith(marker_Start) || !pn.EndsWith(marker_End))
                {
                    offset = pe;
                    continue;
                }
                pn = pn.Remove(pn.Length - marker_End.Length, marker_End.Length).Remove(0, marker_Start.Length); 

                string pv = (pGetValue != null ? pGetValue(pn) : string.Empty);
                if (pv == null) pv = string.Empty; 
                pText = pText.Remove(ps, pe - ps + 1).Insert(ps, pv);

                offset = ps + pv.Length;
            }
            while (ps >= 0 && pe >= 0);

            return pText; 
        }

        public static string AsQuotedList(List<string> pItems, char pQuote)
        {
            string result = "";
            foreach (string it in pItems)
                result += ((result.Length > 0 ? "," : "") + string.Format("{0}{1}{0}", pQuote, it));
            return result;
        }

    } /* CollectionUtils */


	/// <summary>
	/// NameObjectCollection class (implementation took from MSDN)
	/// </summary>
	public class NameObjectCollection : NameObjectCollectionBase
	{
		private DictionaryEntry _de = new DictionaryEntry();

		// Creates an empty collection.
		public NameObjectCollection()
		{
		}

		// Adds elements from an IDictionary into the new collection.
		public NameObjectCollection(IDictionary d, Boolean bReadOnly)
		{
			foreach (DictionaryEntry de in d)
			{
				this.BaseAdd((String)de.Key, de.Value);
			}
			this.IsReadOnly = bReadOnly;
		}

		// Gets a key-and-value pair (DictionaryEntry) using an index.
		public DictionaryEntry this[int index]
		{
			get
			{
				_de.Key = this.BaseGetKey(index);
				_de.Value = this.BaseGet(index);
				return (_de);
			}
		}

		// Gets or sets the value associated with the specified key.
		public Object this[String key]
		{
			get
			{
				return (this.BaseGet(key));
			}
			set
			{
				this.BaseSet(key, value);
			}
		}

		// Gets a String array that contains all the keys in the collection.
		public String[] AllKeys
		{
			get { return (this.BaseGetAllKeys()); }
		}

		// Gets an Object array that contains all the values in the collection.
		public Array AllValues
		{
			get { return (this.BaseGetAllValues()); }
		}

		// Gets a String array that contains all the values in the collection.
		public String[] AllStringValues
		{
			get
			{
				return ((String[])this.BaseGetAllValues(Type.GetType("System.String")));
			}
		}

		// Gets a value indicating if the collection contains keys that are not null.
		public Boolean HasKeys
		{
			get { return (this.BaseHasKeys()); }
		}

		// Adds an entry to the collection.
		public void Add(String key, Object value)
		{
			this.BaseAdd(key, value);
		}

		// Removes an entry with the specified key from the collection.
		public void Remove(String key)
		{
			this.BaseRemove(key);
		}

		// Removes an entry in the specified index from the collection.
		public void Remove(int index)
		{
			this.BaseRemoveAt(index);
		}

		// Clears all the elements in the collection.
		public void Clear()
		{
			this.BaseClear();
		}

	} /* NameObjectCollection */

}
