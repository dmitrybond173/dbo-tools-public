/*
 * Log Facts Extractor: visualization plug-in for CSLMON logs.
 * 
 * Plugin utils
 * 
 * Author: Dmitry Bond (dima_ben@ukr.net)
 * Date: 2023-12-06
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XService.Utils;

namespace Plugin.CslmonClientsAndSessions
{
    public sealed class PluginUtils
    {

        public static void Assert(bool condition, string msg)
        {
            if (!condition)
                throw new Exception(msg);
        }

        public static string DrDump(DbDataReader dr)
        {
            string result = "";
            for (int i = 0; i < dr.FieldCount; i++)
            { 
                string n = dr.GetName(i);
                string v = dr[i].ToString();
                result += string.Format("{0}={1}; ", n, v);
            }
            return result;
        }

        public static void NskTsToUi(string pTimestamp, DateTimePicker pDateUi, DateTimePicker pTimeUi)
        {
            DateTime ts = StrUtils.NskTimestampToDateTime(pTimestamp);
            pDateUi.Value = ts;
            pTimeUi.Value = ts;
            if (pTimestamp.Length > 19)
                pTimeUi.Tag = Convert.ToInt32(pTimestamp.Remove(0, 20));
        }

        public static string UiToNskTs(DateTimePicker pDateUi, DateTimePicker pTimeUi)
        {
            DateTime date = pDateUi.Value;
            DateTime time = pTimeUi.Value;
            DateTime ts = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
            int fraction = 0;
            if (pTimeUi.Tag != null)
                fraction = (int)pTimeUi.Tag;
            string nskTs = StrUtils.NskTimestampOf(ts).Substring(0, 19);
            return nskTs + "." + fraction.ToString("D6");
        }

        public static DateTime ParseLogTime(string s)
        {
            // _123456789_123456789
            // 20190517,000007.43 
            int y = Convert.ToInt32(s.Substring(0, 4));
            int m = Convert.ToInt32(s.Substring(4, 2));
            int d = Convert.ToInt32(s.Substring(6, 2));

            int h = Convert.ToInt32(s.Substring(9, 2));
            int n = Convert.ToInt32(s.Substring(11, 2));
            int sec = Convert.ToInt32(s.Substring(13, 2));
            int frac = Convert.ToInt32(s.Substring(16, 2)) * 10;

            return new DateTime(y, m, d, h, n, sec, frac);
        }

        public static float ParseFloat(string s)
        {
            float result = -1;
            double x;
            if (StrUtils.GetAsDouble(s, out x))
            {
                if (x > 0)
                    x *= 1.0;
                result = (float)Math.Truncate(x * 100.0);
                result /= 100.0f;
            }
            return result;
        }

        public static double ParseDouble(string s)
        {
            double result = -1;
            if (StrUtils.GetAsDouble(s, out result))
            {
                if (result > 0)
                    result *= 1.0;
                result = Math.Truncate(result * 100.0);
                result /= 100.0;
            }
            return result;
        }

        public static int TimeToMs(DateTime pTime)
        {
            return (pTime.Hour * 3600 + pTime.Minute * 60 + pTime.Second) * 1000 + pTime.Millisecond;
        }

        public static int TimeToSec(DateTime pTime)
        {
            return pTime.Hour * 3600 + pTime.Minute * 60 + pTime.Second;
        }

        public static int TimeToSec(DateTime pTime, DateTime pBaseLine)
        {
            return (int)(pTime - pBaseLine).TotalSeconds;
        }

        /// <summary>Validates if specified time (only time, without date) fit specified range</summary>
        public static bool IsTimeInRange(DateTime pTime, DateTime pFrom, DateTime pTo)
        {
            int t = TimeToMs(pTime);
            int tfrom = TimeToMs(pFrom);
            int tto = TimeToMs(pTo);
            return (tfrom <= t && t <= tto);
        }

        public static bool IsTheSameSecond(DateTime pT1, DateTime pT2)
        {
            bool isSame = (
                pT1.Year == pT2.Year && pT1.Month == pT2.Month && pT1.Day == pT2.Day
                && pT1.Hour == pT2.Hour && pT1.Minute == pT2.Minute && pT1.Second == pT2.Second
                );
            return isSame;
        }

        public static DateTime AdjustTimeByBoundOfMinutes(DateTime pTs, int pMinutesBound)
        {
            int mv = TimeToSec(pTs);
            int newMv = BitUtils.AlignTo(mv, pMinutesBound * 60);
            if (newMv > mv)
                newMv -= pMinutesBound * 60;
            mv = newMv;
            return new DateTime(pTs.Year, pTs.Month, pTs.Day, mv / 3600, (mv % 3600) / 60, 0);
        }

        public static DateTime ReplaceDate(DateTime pTime, DateTime pDate)
        {
            return new DateTime(pDate.Year, pDate.Month, pDate.Day, pTime.Hour, pTime.Minute, pTime.Second, pTime.Millisecond);
        }

        public static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (StrUtils.IsSameText(encoders[j].MimeType, mimeType))
                    return encoders[j];
            }
            return null;
        }

        public static void OpenFile(string pFilename)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = pFilename;
            psi.Verb = "open";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }
    }
}
