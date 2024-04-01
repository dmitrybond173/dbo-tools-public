/*
 * Simple utilities to work with Security objects.
 * Written by Dmitry Bond. at Feb 20, 2007
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using XService.Utils;

namespace XService.Security
{
    public class SecurityUtils
    {
        #region Implementation Details

        private static uint[] crc32tbl = new uint[] 
            {
               0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA,
               0x076DC419, 0x706AF48F, 0xE963A535, 0x9E6495A3,
               0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988, 
               0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91, 
               0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE, 
               0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 
               0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 
               0x14015C4F, 0x63066CD9, 0xFA0F3D63, 0x8D080DF5, 
               0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172, 
               0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B, 
               0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 
               0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 
               0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 
               0x21B4F4B5, 0x56B3C423, 0xCFBA9599, 0xB8BDA50F, 
               0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924, 
               0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D,
               0x76DC4190, 0x01DB7106, 0x98D220BC, 0xEFD5102A, 
               0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433, 
               0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 
               0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01, 
               0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 
               0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457, 
               0x65B0D9C6, 0x12B7E950, 0x8BBEB8EA, 0xFCB9887C, 
               0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65, 
               0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2, 
               0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 
               0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0, 
               0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9, 
               0x5005713C, 0x270241AA, 0xBE0B1010, 0xC90C2086, 
               0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F, 
               0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 
               0x59B33D17, 0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD, 
               0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A, 
               0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683, 
               0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8,
               0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 
               0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 
               0xF762575D, 0x806567CB, 0x196C3671, 0x6E6B06E7, 
               0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC, 
               0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5, 
               0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 
               0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B, 
               0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60, 
               0xDF60EFC3, 0xA867DF55, 0x316E8EEF, 0x4669BE79, 
               0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236, 
               0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 
               0xC5BA3BBE, 0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 
               0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D, 
               0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 
               0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
               0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 
               0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 
               0x86D3D2D4, 0xF1D4E242, 0x68DDB3F8, 0x1FDA836E, 
               0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777, 
               0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 
               0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 
               0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2, 
               0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB, 
               0xAED16A4A, 0xD9D65ADC, 0x40DF0B66, 0x37D83BF0, 
               0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9, 
               0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 
               0xBAD03605, 0xCDD70693, 0x54DE5729, 0x23D967BF, 
               0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94, 
               0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D, 
               0x74726F50, 0x736E6F69, 0x706F4320, 0x67697279, 
               0x28207468, 0x31202963, 0x20393939, 0x48207962, 
               0x6E656761, 0x64655220, 0x6E616D64, 0x6FBBA36E,
            };

        #endregion // Implementation Details

        /// <summary>Timeout range (in minutes) for Generic Login Key (it works in both direction - minus to* and plus to current timestamp)</summary>
        public static int GENERIC_LOGIN_KEY_TIMEOUT = 5;

        /// <summary>Build generic login key</summary>
        /// <param name="pTs">Timestamp to use as basis for key building</param>
        public static string BuildGenericLoginKey(DateTime pTs)
        {
            // key template = MiLayDaHoM
            string result = string.Format(
                "{0}{1}{2}{3}{4}",
                pTs.Minute, StrUtils.Right(pTs.Year.ToString(), 1), pTs.Day,
                pTs.Hour, pTs.Month
                );
            return result;
        }

        /// <summary>Check if specified GenericLoginKey is valid for current timestamp</summary>
        /// <param name="pKey">Key to validate</param>
        public static bool CheckGenericLoginKey(string pKey)
        {
            DateTime ts = DateTime.Now;
            ts -= new TimeSpan(0, GENERIC_LOGIN_KEY_TIMEOUT, 0);
            for (int i = 0; i < GENERIC_LOGIN_KEY_TIMEOUT * 2; i++)
            {
                string keyToTry = BuildGenericLoginKey(ts);
                if (keyToTry.CompareTo(pKey) == 0)
                    return true;
                ts += new TimeSpan(0, 1, 0);
            }
            return false;
        }

        /// <summary>
        /// Calculate CRC32 for specified text
        /// </summary>
        /// <param name="pCrc">Initial CRC32 value to start calculcation from it</param>
        /// <param name="text">Source text to calculate CRC32 for</param>
        /// <returns>CRC32 value of specified text</returns>
        public static uint CalculateCRC32(uint pCrc, string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            return CalculateCRC32(pCrc, data, 0, data.Length);
        }

        /// <summary>
        /// Calculate CRC32 value for specified array of bytes
        /// </summary>
        /// <param name="pCrc">Initial CRC32 value to start calculcation from it</param>
        /// <param name="data">Array of bytes to calculate CRC32 value for</param>
        /// <param name="Offset">Offset in array of bytes to start calculation from it</param>
        /// <param name="Count">Number of bytes to use in calculation</param>
        /// <returns>CRC32 value of specified portion in specified array of bytes</returns>
        public static uint CalculateCRC32(uint pCrc, byte[] data, int Offset, int Count)
        {
            if (Offset < 0 || Count == 0 || data == null)
                return 0;

            for (int i = 0; i < Count; i++)
            {
                byte b = (byte)(pCrc & 0xFF);
                pCrc = pCrc >> 8;
                b = (byte)(b ^ (byte)(data[Offset + i]));
                pCrc = pCrc ^ crc32tbl[b];
            }
            return pCrc;
        }

        /// <summary>Open specified directory to access by Everyone</summary>
        /// <param name="pPath">Path to directory </param>
        public static void OpenDirToEveryone(string pPath)
        {
            // set directory access for "Everyone" 
            DirectorySecurity sec = Directory.GetAccessControl(pPath);
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl | FileSystemRights.Synchronize,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(pPath, sec);
        }

        /// <summary>Open specified file to access by Everyone</summary>
        /// <param name="pFilename">Path to file</param>
        public static void OpenFileToEveryone(string pFilename)
        {
            // set file access for "Everyone" 
            FileSecurity sec = File.GetAccessControl(pFilename);
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl | FileSystemRights.Synchronize, AccessControlType.Allow));
            File.SetAccessControl(pFilename, sec);
        }

        /// <summary>Compare 2 texts in char[]. Useful when need to compare sensitive data</summary>
        /// <param name="pText1">Text1 to compare</param>
        /// <param name="pText2">Text2 to compare</param>
        /// <returns>Returns 0 when both are nulls or equal, returns -1 when Text1 is less then Text2, returns 1 when Text1 is greather then Text2</returns>
        public static int Compare(char[] pText1, char[] pText2)
        {
            if (pText1 == null && pText2 == null) return 0;
            if (pText1 == null && pText2 != null) return -1;
            if (pText1 != null && pText2 == null) return 1;

            if (pText1.Length < pText2.Length) return -1;
            if (pText1.Length > pText2.Length) return 1;

            for (int iCh = 0; iCh < pText1.Length; iCh++)
            {
                if (pText1[iCh] < pText2[iCh]) return -1;
                else if (pText1[iCh] > pText2[iCh]) return 1;
            }
            return 0;
        }

        /// <summary>Encode normal string into SecureString object</summary>
        /// <param name="pText">normal string value to encode</param>
        /// <returns>SecureString object</returns>
        public static SecureString ToSecureString(String pText)
        {
            SecureString ss = new SecureString();
            for (int iCh = 0; iCh < pText.Length; iCh++)
            {
                ss.AppendChar(pText[iCh]);
            }
            //for (int iCh = 0; iCh < pText.Length; iCh++) { pText = pText. }
            //pText.Remove(0, pText.Length);
            pText = pText.Trim(pText.ToCharArray());
            return ss;
        }

        /// <summary>Encode normal string into SecureString object</summary>
        /// <param name="pText">normal string value to encode</param>
        /// <returns>SecureString object</returns>
        public static SecureString ToSecureString(StringBuilder pText)
        {
            SecureString ss = new SecureString();
            for (int iCh = 0; iCh < pText.Length; iCh++)
            {
                ss.AppendChar(pText[iCh]);
                pText[iCh] = '*';
            }
            pText.Remove(0, pText.Length);
            return ss;
        }

        /// <summary>Encode text into SecureString object</summary>
        /// <param name="pText">characters to encode</param>
        /// <returns>SecureString object</returns>
        public static SecureString ToSecureString(char[] pText)
        {
            SecureString ss = new SecureString();
            for (int iCh = 0; iCh < pText.Length; iCh++)
            {
                ss.AppendChar(pText[iCh]);
            }
            Cleanup(pText);
            return ss;
        }

        /// <summary>Decode SecureString object to normal string</summary>
        /// <param name="ss">SecureString object to decode</param>
        /// <returns>Normal string value</returns>
        public static StringBuilder FromSecureString(SecureString ss)
        {
            IntPtr unmngStr = IntPtr.Zero;
            try
            {
                unmngStr = Marshal.SecureStringToGlobalAllocUnicode(ss);
                StringBuilder sb = new StringBuilder(ss.Length + 4);
                sb.Append(Marshal.PtrToStringUni(unmngStr));
                return sb;
            }
            finally
            {
                // Note! Marshal.ZeroFreeGlobalAllocUnicode is not reliable! Sometimes it does not clear memory buffer! So, it is bettrer to clear it explicitly
                //for (int iCh = 0; iCh < ss.Length; iCh++) Marshal.WriteByte(unmngStr, (byte)RND_CHARS[iCh % RND_CHARS.Length]);
                CleanupUnmanaged(unmngStr, ss.Length);

                Marshal.ZeroFreeGlobalAllocUnicode(unmngStr);
            }
        }

        /// <summary>Decode SecureString object to array of chars</summary>
        /// <param name="ss">SecureString object to decode</param>
        /// <returns>Normal string value in a form of char[] array</returns>
        public static char[] FromSecureStringCA(SecureString ss)
        {
            IntPtr unmngStr = IntPtr.Zero;
            try
            {
                unmngStr = Marshal.SecureStringToGlobalAllocUnicode(ss);
                StringBuilder sb = new StringBuilder(ss.Length + 4);
                return Marshal.PtrToStringUni(unmngStr).ToCharArray();
            }
            finally
            {
                // Note! Marshal.ZeroFreeGlobalAllocUnicode is not reliable! Sometimes it does not clear memory buffer!
                CleanupUnmanaged(unmngStr, ss.Length);

                Marshal.ZeroFreeGlobalAllocUnicode(unmngStr);
            }
        }

        public static char[] RND_CHARS = "^#$%@X*TGH@Y&*^@&+(FUI*&76r8w9a3s42bkj0khcOhHVjJOlH22VKZLJ-=1(^&(*&1*2&G(*&JHBN3".ToCharArray();

        /// <summary>Replace content of specified char[] array with trash/random chars</summary>
        public static void Cleanup(char[] sb)
        {
            for (int iCh = 0; iCh < sb.Length; iCh++)
            {
                sb[iCh] = RND_CHARS[iCh % RND_CHARS.Length];
            }
        }

        /// <summary>Replace content of specified StringBuilder with trash/random chars, then clear it</summary>
        public static void Cleanup(StringBuilder sb)
        {
            for (int iCh = 0; iCh < sb.Length; iCh++)
            {
                sb[iCh] = RND_CHARS[iCh % RND_CHARS.Length];
            }
            if (sb.Length > 0)
                sb.Remove(0, sb.Length);
        }

        /// <summary>Cleanup content of unmanaged memory block, fill it with random data</summary>
        /// <param name="unmngData">Unmanaged memory block</param>
        /// <param name="pLength">Size in bytes of unmanaged memory block to cleanup</param>
        public static void CleanupUnmanaged(IntPtr pUnmngData, int pLength)
        {
            if (pUnmngData == IntPtr.Zero) return;
            for (int iCh = 0; iCh < pLength; iCh++)
                Marshal.WriteByte(pUnmngData, (byte)RND_CHARS[iCh % RND_CHARS.Length]);
        }

        /// <summary>If application is running in admin-mode</summary>
        public static bool IsAdminMode()
        {
            bool result = false;
            WindowsIdentity user = null;
            try
            {
                // get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                result = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            // catch (UnauthorizedAccessException ex) { result = false; }
            catch (Exception ex)
            {
                Trace.WriteLineIf(CommonUtils.TrcLvl.TraceError, CommonUtils.TrcLvl.TraceError ? string.Format(
                    "{0}\nat{1}", ErrorUtils.FormatErrorMsg(ex), ErrorUtils.FormatStackTrace(ex)) : "");
                result = false;
            }
            finally
            {
                if (user != null)
                    user.Dispose();
            }
            return result;
        }
    }


    /// <summary>
    /// XOR randomizer (XorShift128)
    /// </summary>
    public class XorRandomizer
    {
        public const uint INITIAL_X = 123456789;
        public const uint INITIAL_Y = 362436069;
        public const uint INITIAL_Z = 521288629;
        public const uint INITIAL_W = 88675123;

        private uint x = INITIAL_X, y = INITIAL_Y, z = INITIAL_Z, w = INITIAL_W;

        public void Reset()
        {
            x = INITIAL_X;
            y = INITIAL_Y;
            z = INITIAL_Z;
            w = INITIAL_W;
        }

        public int Next()
        { 
            uint t;
            t = (x ^ (x << 11)); x = y; y = z; z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            return (int)w;
        }
    }


    /// <summary>
    /// RC4 chipher.
    /// </summary>
    public class RC4Encryptor
    {
        public string Password = string.Empty;
        public byte x = 0, y = 0;
        public byte[] state = new byte[256];

        public void SetupKey(string pPassword)
        {
            if (string.IsNullOrEmpty(pPassword))
                pPassword = this.Password;
            if (string.IsNullOrEmpty(pPassword))
                throw new XServiceError("Password cannot be empty");

            this.Password = pPassword;
            int keyDataLen = this.Password.Length;
            this.initialMatrix();
            byte idx1 = 0, idx2 = 0;
            for (int i = 0; i <= 255; i++)
            {
                idx2 = (byte)((byte)this.Password[idx1] + this.state[i] + idx2);
                this.swap((byte)i, idx2);
                idx1 = (byte)((idx1 + 1) % keyDataLen);
            }
            this.x = 0;
            this.y = 0;
        }

        public void ResetKey()
        {
            SetupKey(null);
        }

        public override string ToString()
        {
            string info = string.Format("x:{0}; y:{1};", this.x, this.y);
            for (int i = 0; i <= 255; i++)
            {
                if ((i % 16) == 0)
                    info += Environment.NewLine;
                info += (this.state[i].ToString("X2") + ' ');
            }
            return info;
        }

        public byte[] Transform(byte[] pSourceData)
        {
            byte[] encryptedData = new byte[pSourceData.Length];
            byte lx = this.x;
            byte ly = this.y;
            byte b1, b2;

            for (int i = 0; i < pSourceData.Length; i++)
            {
                lx++;
                ly = (byte)(ly + this.state[lx]);
                this.swap(lx, ly);
                b1 = (byte)pSourceData[i];
                b2 = (byte)(b1 ^ this.state[(this.state[lx] + this.state[ly]) & 0xFF]);
                encryptedData[i] = b2;
            }

            this.x = lx;
            this.y = ly;
            return encryptedData;
        }

        protected void initialMatrix()
        {
            for (int i = 0; i <= 255; i++)
            {
                this.state[i] = (byte)i;
            }
        }

        protected void swap(byte i1, byte i2)
        {
            byte b = this.state[i1];
            this.state[i1] = this.state[i2];
            this.state[i2] = b;
        }

    }

}
