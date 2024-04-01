/*
 * Simple demo application to show how to decrypt passwords with RC4.
 * Written by Dmitry Bond. (dima_ben@ukr.net)
 *
 * Note: C# code for RC4 was taken from 
 *   https://www.codeproject.com/Questions/1105019/RC-file-encryption-and-decryption-in-Csharp
 * and then adjusted to be more understandable.
*/

using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
//using System.Linq;
using System.Text;

namespace XService.Security
{
    /// <summary>
    /// Password encryption service routinues
    /// </summary>
    public class PwdEncryptor
    {
        /// <summary>Password key to encrypt/decrypt passwords</summary>
        public static string SecureKey = "p@zzW0rD";

        public static string SaltChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>Default text encoding</summary>
        public static Encoding DefaultEncoding = Encoding.ASCII;

        /// <summary>Main API: password decryptor method</summary>
        /// <param name="pPasswordCodes">Encrypted password to decrypt</param>
        /// <returns>Decrypted password</returns>
        public static string EncryptPassword(string pPassword)
        {
            string salt1 = generateSalt(9);
            string salt2 = generateSalt(7);
            string source = salt1 + pPassword + salt2;
            byte[] sourceData = DefaultEncoding.GetBytes(source);

            // get bytes of password
            byte[] key = PwdEncryptor.DefaultEncoding.GetBytes(SecureKey);

            byte[] resultData = EncryptBytes(key, sourceData);

            string result = PwdEncryptor.Hex(resultData);
            return result;
        }

        protected static Random rndSalt = new Random(173);

        protected static string generateSalt(int pLen)
        { 
            string result = "";
            for (int i = 0; i < pLen; i++)
            {
                result += SaltChars[rndSalt.Next(SaltChars.Length)];
            }
            return result;
        }

        /// <summary>Main API: password decryptor method</summary>
        /// <param name="pPasswordCodes">Encrypted password to decrypt</param>
        /// <returns>Decrypted password</returns>
        public static SecureString DecryptPasswordSS(string pPasswordCodes)
        {
            byte[] data = PwdEncryptor.Unhex(pPasswordCodes);

            // get bytes of password
            byte[] key = PwdEncryptor.DefaultEncoding.GetBytes(SecureKey);

            // decrypt password data
            byte[] sourceData = PwdEncryptor.DecryptBytes(key, data);

            SecureString result = new SecureString();
            char[] arr = DefaultEncoding.GetChars(sourceData, 9, sourceData.Length - (9 + 7));
            foreach (char ch in arr)
            {
                result.AppendChar(ch);
            }
            return result;

            // remove salt fractions and construct original password
            //return PwdEncryptor.DefaultEncoding.GetString(sourceData, 9, sourceData.Length - (9 + 7));
        }

        /// <summary>Main API: password decryptor method</summary>
        /// <param name="pPasswordCodes">Encrypted password to decrypt</param>
        /// <returns>Decrypted password</returns>
        public static string DecryptPassword(string pPasswordCodes)
        {
            byte[] data = PwdEncryptor.Unhex(pPasswordCodes);

            // get bytes of password
            byte[] key = PwdEncryptor.DefaultEncoding.GetBytes(SecureKey);

            // decrypt password data
            byte[] sourceData = PwdEncryptor.DecryptBytes(key, data);

            // remove salt fractions and construct original password
            return PwdEncryptor.DefaultEncoding.GetString(sourceData, 9, sourceData.Length - (9 + 7));
        }

        public static string Hex(byte[] pData)
        {
            string result = "";
            foreach (byte b in pData)
            {
                result += b.ToString("X2");
            }
            return result;
        }

        public static byte[] Unhex(string pHexCodesStr)
        {
            if ((pHexCodesStr.Length % 2) != 0)
                throw new Exception(string.Format("Invalid hex codes str: {0}", pHexCodesStr));

            List<byte> result = new List<byte>();
            string bs = "";
            int i = 0;
            while (i < pHexCodesStr.Length || bs.Length > 0)
            {
                char ch = (i < pHexCodesStr.Length ? pHexCodesStr[i] : '\0');

                if (bs.Length >= 2)
                {
                    result.Add((byte)Convert.ToInt32(bs, 16));
                    bs = "";
                }
                if (ch != '\0')
                    bs += ch;

                i++;
            }
            return result.ToArray();
        }

        public static string Encrypt(string key, string data)
        {
            return Convert.ToBase64String(EncryptBytes(DefaultEncoding.GetBytes(key), DefaultEncoding.GetBytes(data)));
        }

        public static string Decrypt(string key, string data)
        {
            return DefaultEncoding.GetString(EncryptBytes(DefaultEncoding.GetBytes(key), DefaultEncoding.GetBytes(data))); // Convert.FromBase64String(
        }

        public static byte[] EncryptBytes(byte[] key, byte[] data)
        {
            return EncryptOutput(key, data);
        }

        public static byte[] DecryptBytes(byte[] key, byte[] data)
        {
            return EncryptOutput(key, data);
        }

        public static byte[] SetupKey(byte[] key)
        {
            byte[] s = new byte[256];
            int i;
            for (i = 0; i < 256; i++)
                s[i] = (byte)i;

            int j;
            for (i = 0, j = 0; i < 256; i++)
            {
                j = (j + key[i % key.Length] + s[i]) & 255;
                Swap(s, i, j);
            }

            return s;
        }

        private static byte[] EncryptOutput(byte[] pPasswordBytes, byte[] pData)
        {
            byte[] key = SetupKey(pPasswordBytes);

            int i = 0;
            int j = 0;

            byte[] output = new byte[pData.Length];
            for (int iByte = 0; iByte < pData.Length; iByte++)
            {
                byte b = pData[iByte];

                i = (i + 1) & 255;
                j = (j + key[i]) & 255;

                Swap(key, i, j);

                output[iByte] = (byte)(b ^ key[(key[i] + key[j]) & 255]);
            }
            return output;
        }

        private static void Swap(byte[] data, int i, int j)
        {
            byte c = data[i];

            data[i] = data[j];
            data[j] = c;
        }
    }

}