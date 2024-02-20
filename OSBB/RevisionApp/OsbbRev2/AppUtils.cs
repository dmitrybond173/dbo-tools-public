/* 
 * App for OSBB Revision.
 *
 * App utils.
 * 
 * Written by Dmitry Bond. (dima_ben@ukr.net) at Feb, 2024
 */

using System;
using System.Configuration;
using System.Text;
using System.Xml;

namespace OsbbRev2
{
    public static class AppUtils
    {
        public static XmlNode RequiredAttr(string pAttrName, XmlElement pNode)
        {
            XmlNode result = pNode.GetAttributeNode(pAttrName);
            if (result == null)
                throw new ConfigurationErrorsException(string.Format("Required attribute [{0}] is missing!", pAttrName));
            return result;
        }

        public static XmlNode OptionalAttr(string pAttrName, XmlElement pNode)
        {
            XmlNode result = pNode.GetAttributeNode(pAttrName);
            return result;
        }


        /// <summary>
        /// Sometimes the date from Excel is a string, other times it is an OA Date:
        /// Excel stores date values as a Double representing the number of days from January 1, 1900.
        /// Need to use the FromOADate method which takes a Double and converts to a Date.
        /// OA = OLE Automation compatible.
        /// </summary>
        /// <param name="date">a string to parse into a date</param>
        /// <returns>a DateTime value; if the string could not be parsed, returns DateTime.MinValue</returns>
        public static DateTime ParseExcelDate(string date)
        {
            DateTime dt;
            if (DateTime.TryParse(date, out dt))
            {
                return dt;
            }

            double oaDate;
            if (double.TryParse(date, out oaDate))
            {
                return DateTime.FromOADate(oaDate);
            }

            return DateTime.MinValue;
        }

        public static DateTime Compose(DateTime pDate, DateTime pTime)
        {
            return new DateTime(
                pDate.Year, pDate.Month, pDate.Day,
                pTime.Hour, pTime.Minute, pTime.Second, 0
                );
        }
    }
}
