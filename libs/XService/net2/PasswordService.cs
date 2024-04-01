/*
 * Passwords Encoder.
 * The utility allows to save passwords in open config files (in encoded form).
 * 
 * Copyright (c) Dmitry Bondarenko
 * Version 1.00
 * Date June 13, 2007
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using XService.Utils;

namespace XService.Security
{
    public class PasswordService
    {
        /// <summary>Default password encryption algorithm</summary>
        public static string DEFAULT_PasswordEncryptionProviderId = "TDES";

        /// <summary>Number of normal password chars between random padding chars</summary>
        public static byte RANDOM_STUFF_SEED = 5;

        /// <summary>Compatibility with very old version</summary>
        public static bool COMPATIBILITY_MODE = false;

        /// <summary>Pad password with extra chars</summary>
        public static bool PADDED_PASSWORD = true;

        /// <summary>Derives a key from a password using an extension of the PBKDF1 algorithm</summary>
        public static bool DERIVE_PASSWORD = true;
        /// <summary>What algorithm use when derives a key from a password</summary>
        public static string DERIVE_HASHCODE = "SHA1";
        /// <summary>How many iterations use when derives a key from a password</summary>
        public static int DERIVE_ITERATIONS = 2;

        /// <summary>List of supported encryption algorithms (with descriptions)</summary>
        public static string[] SUPPORTED_ALGORITHMS = new string[] { "TD: Tripple DES", "RC: RC2", "RJ: Rijndael", "RCE: RC4-PwdEncryptor" };

        /// <summary>List of supported encryption algorithms names (incliding aliases)</summary>
        public static string[] SUPPORTED_ALGORITHM_NAMES = new string[]
            {
                "TRIPLEDES", "3DES", "TDES", "TD",
                "RC2", "RC",
                "RIJNDAEL", "RJ",
                "RCE"
            };

        /// <summary>Password for passwords encoding</summary>
        private static string Pass4Passws
        {
            get
            {
                if (p4p == string.Empty)
                {
                    byte[] data = new byte[1];
                    foreach (double n in password4passwords)
                    {
                        data[0] = (byte)n;
                        p4p += Encoding.ASCII.GetString(data);
                    }
                    if (PADDED_PASSWORD)
                    {
                        int i = 0;
                        string original = p4p;
                        while (p4p.Length < 16)
                            p4p += original[original.Length - ((i++) % original.Length) - 1];
                        if (p4p.Length > 16)
                            p4p = p4p.Substring(0, 16);
                    }
                }
                return p4p;
            }
        }

        /// <summary>Returns a string with diagnostic info about password encoding params</summary>
        public static string SerializedOptions
        {
            get
            {
                return string.Format(
                    "Deprecated:{1}{0}RndSeed:{2}{0}DefAlg:{3}{0}Padded:{4}{0}DerivePassw:{5}[{6}/{7}]{0}",
                    "\n",
                    COMPATIBILITY_MODE,
                    RANDOM_STUFF_SEED,
                    DEFAULT_PasswordEncryptionProviderId,
                    PADDED_PASSWORD,
                    DERIVE_PASSWORD,
                    DERIVE_HASHCODE,
                    DERIVE_ITERATIONS,
                    0
                    );
            }
        }

        /// <summary>Holder of password encoding params</summary>
        public class PasswordEncodingParams : IComparable
        {
            // Fields
            public bool CompatibilityMode;
            public bool DerivePassword;
            public string DeriveAlg;
            public int DeriveIterations;
            public byte[] IV128;
            public byte[] IV192;
            public byte[] IV256;
            public byte[] IV64;
            public string Key;
            public bool KeyPadding;
            public string StuffData;
            public string StuffData64;
            public byte StuffSeed;
            public string DefaultAlg;

            // Methods
            public PasswordEncodingParams()
            {
            }

            public PasswordEncodingParams(PasswordService.PasswordEncodingParams pSrc)
            {
                this.Key = pSrc.Key;
                this.StuffSeed = pSrc.StuffSeed;
                this.KeyPadding = pSrc.KeyPadding;
                this.CompatibilityMode = pSrc.CompatibilityMode;
                this.DerivePassword = pSrc.DerivePassword;
                this.DeriveAlg = pSrc.DeriveAlg;
                this.DeriveIterations = pSrc.DeriveIterations;
                this.IV64 = pSrc.IV64;
                this.IV128 = pSrc.IV128;
                this.IV192 = pSrc.IV192;
                this.IV256 = pSrc.IV256;
                this.StuffData = pSrc.StuffData;
                this.StuffData64 = pSrc.StuffData64;
                this.DefaultAlg = pSrc.DefaultAlg;
            }

            public override string ToString()
            {
                return string.Format(
                    "Deprecated:{1}{0}RndSeed:{2}{0}DefAlg:{3}{0}Pad:{4}{0}DerivePsw:{5}[{6}/{7}]{0}Key:{8}",
                    "\n",
                    CompatibilityMode,
                    StuffSeed,
                    StrUtils.FixNull(DefaultAlg),
                    KeyPadding,
                    DerivePassword, DeriveAlg, DeriveIterations,
                    Key
                    );
            }

            public int CompareTo(object obj)
            {
                PasswordEncodingParams pSrc = (PasswordEncodingParams)obj;

                int result = this.Key.CompareTo(pSrc.Key);
                if (result != 0) return 0;

                result = this.StuffSeed.CompareTo(pSrc.StuffSeed);
                if (result != 0) return 0;

                result = this.KeyPadding.CompareTo(pSrc.KeyPadding);
                if (result != 0) return 0;

                result = this.CompatibilityMode.CompareTo(pSrc.CompatibilityMode);
                if (result != 0) return 0;

                result = this.DerivePassword.CompareTo(pSrc.DerivePassword);
                if (result != 0) return 0;
                result = this.DeriveAlg.CompareTo(pSrc.DeriveAlg);
                if (result != 0) return 0;
                result = this.DeriveIterations.CompareTo(pSrc.DeriveIterations);
                if (result != 0) return 0;

                result = CommonUtils.CompareArrays(this.IV64, pSrc.IV64);
                if (result != 0) return 0;
                result = CommonUtils.CompareArrays(this.IV128, pSrc.IV128);
                if (result != 0) return 0;
                result = CommonUtils.CompareArrays(this.IV192, pSrc.IV192);
                if (result != 0) return 0;
                result = CommonUtils.CompareArrays(this.IV256, pSrc.IV256);
                if (result != 0) return 0;

                result = this.StuffData.CompareTo(pSrc.StuffData);
                if (result != 0) return 0;
                result = this.StuffData64.CompareTo(pSrc.StuffData64);
                if (result != 0) return 0;

                return 0;
            }
        }

        /// <summary>Get all password encoding params</summary>
        public static void GetPasswordEncodingParams(out PasswordEncodingParams pObj)
        {
            pObj = new PasswordEncodingParams();
            pObj.Key = Pass4Passws.Substring(0, password4passwords.Length);
            pObj.StuffSeed = RANDOM_STUFF_SEED;
            pObj.KeyPadding = PADDED_PASSWORD;
            pObj.CompatibilityMode = COMPATIBILITY_MODE;
            pObj.DerivePassword = DERIVE_PASSWORD;
            pObj.DeriveAlg = DERIVE_HASHCODE;
            pObj.DeriveIterations = DERIVE_ITERATIONS;
            pObj.IV64 = my_iv_64;
            pObj.IV128 = my_iv_128;
            pObj.IV192 = my_iv_192;
            pObj.IV256 = my_iv_256;
            pObj.StuffData = StuffData;
            pObj.StuffData64 = StuffData64;
            pObj.DefaultAlg = DEFAULT_PasswordEncryptionProviderId;
        }

        /// <summary>Set all password encoding params</summary>
        public static void SetPasswordEncodingParams(PasswordEncodingParams pObj)
        {
            p4p = string.Empty;

            if (pObj.Key != null)
            {
                SetPasswordEncodingParams(pObj.Key, pObj.StuffSeed);
            }

            PADDED_PASSWORD = pObj.KeyPadding;
            COMPATIBILITY_MODE = pObj.CompatibilityMode;
            DERIVE_PASSWORD = pObj.DerivePassword;
            DERIVE_HASHCODE = pObj.DeriveAlg;
            DERIVE_ITERATIONS = pObj.DeriveIterations;

            if (pObj.IV64 != null) my_iv_64 = pObj.IV64;
            if (pObj.IV128 != null) my_iv_128 = pObj.IV128;
            if (pObj.IV192 != null) my_iv_192 = pObj.IV192;
            if (pObj.IV256 != null) my_iv_256 = pObj.IV256;
            if (pObj.StuffData != null) StuffData = pObj.StuffData;
            if (pObj.StuffData64 != null) StuffData64 = pObj.StuffData64;
            if (pObj.DefaultAlg != null) DEFAULT_PasswordEncryptionProviderId = pObj.DefaultAlg;
        }

        /// <summary>Get core password encoding params</summary>
        public static void GetPasswordEncodingParams(out string pPassw4Passw, out byte pStuffSeed)
        {
            pPassw4Passw = Pass4Passws;
            pStuffSeed = RANDOM_STUFF_SEED;
        }

        /// <summary>Set core password encoding params</summary>
		public static void SetPasswordEncodingParams(string pPassw4Passw, byte pStuffSeed)
        {
            p4p = string.Empty;
            password4passwords = new double[pPassw4Passw.Length];
            for (int i = 0; i < pPassw4Passw.Length; i++)
                password4passwords[i] = Convert.ToDouble(Convert.ToByte(pPassw4Passw[i]));

            RANDOM_STUFF_SEED = pStuffSeed;
        }

        /// <summary>Encrypt specified text using specified providerId (if providerId is null then use default)</summary>
        public static string EncryptPassword(string pPassword, string pProviderId)
        {
            if (string.IsNullOrEmpty(pProviderId))
                pProviderId = DEFAULT_PasswordEncryptionProviderId;

            if (StrUtils.IsSameText(pProviderId, "rce"))
            {
                PwdEncryptor.SecureKey = Pass4Passws;
                string result = PwdEncryptor.EncryptPassword(pPassword);
                return "{" + result + "}";
            }

            SymmetricAlgorithm encoder = GetCryptoProvider(pProviderId);

            byte[] my_iv_ref = getIvRef(encoder);

            string pPassw = Pass4Passws;
            if (PADDED_PASSWORD)
            {
                pPassw = PadPassword(pPassw, encoder.KeySize / 8);
            }

            byte[] bytes = Encoding.ASCII.GetBytes(pPassw);
            if (DERIVE_PASSWORD)
            {
                bytes = new PasswordDeriveBytes(Pass4Passws, my_iv_256, DERIVE_HASHCODE, DERIVE_ITERATIONS)
                    .GetBytes(encoder.KeySize / 8);
            }

            MemoryStream out_strm = new MemoryStream();
            byte[] my_key = Encoding.ASCII.GetBytes(Pass4Passws);

            ICryptoTransform trx = encoder.CreateEncryptor(my_key, my_iv_ref);
            CryptoStream encStream = new CryptoStream(out_strm, trx, CryptoStreamMode.Write);
            StreamWriter sw = new StreamWriter(encStream);
            sw.Write(pPassword);
            sw.Close();
            encStream.Close();
            byte[] encoded = out_strm.ToArray();
            string x = Convert.ToBase64String(encoded);

            if (COMPATIBILITY_MODE)
                x = x.Remove(x.Length - 1, 1);

            return "{" + StuffWithRandom(x, RANDOM_STUFF_SEED) + "}";
        }

        /// <summary>Try to extract providerId from encryptedt text</summary>
        public static bool ExtractProviverId(string pEncryptedPassword, out string pProvId, out string pEncPassw)
        {
            Regex re_pwd = new Regex(@"\{(\w+\:)+([A-Za-z0-9=+\/]+)\}", RegexOptions.IgnoreCase);
            MatchCollection mc = re_pwd.Matches(pEncryptedPassword);
            bool result = (mc.Count > 0);
            if (result)
            {
                Match m = mc[0];
                string prov_id = m.Groups[1].Captures[0].Value;
                if (prov_id.EndsWith(":")) prov_id = prov_id.Remove(prov_id.Length - 1, 1);
                pProvId = prov_id;
                pEncPassw = "{" + m.Groups[2].Captures[0].Value + "}";
            }
            else
            {
                pProvId = DEFAULT_PasswordEncryptionProviderId;
                pEncPassw = pEncryptedPassword;
            }
            return result;
        }

        /// <summary>Decrypt encrypted-text (it will try to extract providerId from it)</summary>
        public static string DecryptPassword(string pEncryptedPassword)
        {
            string passw = "";
            string prov_id = "";
            if (!ExtractProviverId(pEncryptedPassword, out prov_id, out passw))
                prov_id = DEFAULT_PasswordEncryptionProviderId;

            return DecryptPassword(passw, prov_id);
        }

        // decrypt password which was encrypted multiple times
        public static string RoundedDecryptPassword(string pEncryptedPassword, string pRoundsPic)
        {
            string[] roundsPic = null;
            if (!string.IsNullOrEmpty(pRoundsPic))
            {
                string s = pRoundsPic.Trim(" \t\r\n".ToCharArray());
                if (s.IndexOf(CultureInfo.CurrentCulture.TextInfo.ListSeparator) >= 0)
                    roundsPic = s.Split(new string[] { CultureInfo.CurrentCulture.TextInfo.ListSeparator }, StringSplitOptions.RemoveEmptyEntries);
                else if (s.IndexOf(";") >= 0) roundsPic = s.Split(';');
                else if (s.IndexOf(",") >= 0) roundsPic = s.Split(',');
                else if (s.IndexOf("-") >= 0) roundsPic = s.Split('-');
            }

            int iRound = 0;
            string text = "";
            do
            {
                string algId = null;
                if (roundsPic != null)
                    algId = roundsPic[iRound % roundsPic.Length];

                if (iRound == 0)
                    text = pEncryptedPassword;

                // algorithmID in string seems have higher priority...
                string id;
                if (ExtractProviverId(text, out id, out text))
                    algId = id;

                if (algId != null)
                    text = PasswordService.DecryptPassword(text, algId);
                else
                    text = PasswordService.DecryptPassword(text);

                iRound++;
            }
            while (text.StartsWith("{") && text.EndsWith("}"));

            return text;
        }

        /// <summary>Decrypt encrypted-text using specified algorithm (when pProviderId is null then use default)</summary>
        public static string DecryptPassword(string pEncryptedPassword, string pProviderId)
        {
            if (string.IsNullOrEmpty(pProviderId))
                pProviderId = DEFAULT_PasswordEncryptionProviderId;

            bool isRce = StrUtils.IsSameText(pProviderId, "rce");
            if (pEncryptedPassword.StartsWith("{") && pEncryptedPassword.EndsWith("}"))
            {
                pEncryptedPassword = pEncryptedPassword.Remove(pEncryptedPassword.Length - 1, 1).Remove(0, 1);
                if (!isRce)
                    pEncryptedPassword = RemoveRandomStuff(pEncryptedPassword, RANDOM_STUFF_SEED);
            }
            else
                throw new Exception(String.Format("Invalid secure password format ({0})!", pEncryptedPassword));

            if (StrUtils.IsSameText(pProviderId, "rce"))
            {
                PwdEncryptor.SecureKey = Pass4Passws;
                return PwdEncryptor.DecryptPassword(pEncryptedPassword);
            }

            SymmetricAlgorithm decoder = GetCryptoProvider(pProviderId);

            byte[] my_iv_ref = getIvRef(decoder);

            string pPassw = Pass4Passws;
            if (PADDED_PASSWORD)
            {
                pPassw = PadPassword(pPassw, decoder.KeySize / 8);
            }

            byte[] bytes = Encoding.ASCII.GetBytes(pPassw);
            if (DERIVE_PASSWORD)
            {
                bytes = new PasswordDeriveBytes(Pass4Passws, my_iv_256, DERIVE_HASHCODE, DERIVE_ITERATIONS)
                    .GetBytes(decoder.KeySize / 8);
            }

            string x = pEncryptedPassword;
            if (COMPATIBILITY_MODE)
                x += "=";

            byte[] data;
            try { data = Convert.FromBase64String(x); }
            catch
            {
                // have to do this to fix possible wrong coding
                if (x.EndsWith("=")) x = x.Remove(x.Length - 1, 1);
                data = Convert.FromBase64String(x);
            }
            MemoryStream in_strm = new MemoryStream();
            in_strm.Write(data, 0, data.Length);
            in_strm.Seek(0, SeekOrigin.Begin);
            byte[] my_key = Encoding.ASCII.GetBytes(Pass4Passws);

            ICryptoTransform trx = decoder.CreateDecryptor(my_key, my_iv_ref);
            CryptoStream decStream = new CryptoStream(in_strm, trx, CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(decStream);
            string result = sr.ReadToEnd();
            sr.Close();
            decStream.Close();

            return result;
        }

        public static Random Rnd
        {
            get
            {
                lock (typeof(Random))
                {
                    if (rnd == null)
                        rnd = new Random();
                    return rnd;
                }
            }
        }

        public static RNGCryptoServiceProvider CryptoRnd
        {
            get
            {
                lock (typeof(RNGCryptoServiceProvider))
                {
                    if (cryptoRnd == null)
                        cryptoRnd = new RNGCryptoServiceProvider();
                    return cryptoRnd;
                }
            }
        }

        public static XorRandomizer XorRnd
        {
            get
            {
                lock (typeof(XorRandomizer))
                {
                    if (xorRnd == null)
                        xorRnd = new XorRandomizer();
                    return xorRnd;
                }
            }
        }

        /// <summary>Simulate a roll-dice with specified number of sides</summary>
        public static byte RollDice(byte NumSides)
        {
            byte[] randomNumber = new byte[7];
            //RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();
            CryptoRnd.GetBytes(randomNumber);
            byte rand = randomNumber[0];
            if (rand >= NumSides)
                rand = (byte)(rand % NumSides);
            return (byte)(rand + 1);
        }

        /// <summary>Chars to be used as padding-chars for passwords (62 items)</summary>
        public static string StuffData = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // 62 chars

        /// <summary>Chars to be used as padding-chars for passwords (64 items)</summary>
        public static string StuffData64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_~"; // 64 chars

        /// <summary>Insert random padding characters inside a text</summary>
        public static string StuffWithRandom(string pText, byte pSeed)
        {
            int i;
            string stuff = "";
            for (i = 0; i < pText.Length; i++)
            {
                byte n = RollDice((byte)StuffData.Length); // 1..62
                stuff += StuffData[n - 1];
            }
            //DEBUG: stuff = "-----------------------------";
            i = ((pSeed & (byte)1) == 0 ? 0 : 1);
            int j = 0;
            while (i < pText.Length)
            {
                pText = pText.Insert(i, "" + stuff[j++]);
                i += pSeed;
            }
            return pText;
        }

        /// <summary>Remove padding characters from text</summary>
        public static string RemoveRandomStuff(string pText, byte pSeed)
        {
            int i = ((pSeed & (byte)1) == 0 ? 0 : 1);
            //int j = 0;
            while (i < pText.Length)
            {
                pText = pText.Remove(i, 1);
                i += pSeed;
                i--;
            }
            return pText;
        }

        /// <summary>Pad password to specified length with random text chars</summary>
        public static string RandomPaddingText(int pLength)
        {
            string result = "";
            for (int i = 0; i < pLength; i++)
                result = result + (char)(33 + PasswordService.Rnd.Next(94));
            return result;
        }

        /// <summary>Create chipher object of specified algorithm</summary>
        public static SymmetricAlgorithm GetCryptoProvider(string pProviderId)
        {
            pProviderId = pProviderId.ToUpper();
            switch (pProviderId)
            {
                case "TRIPLEDES":
                case "3DES":
                case "TDES":
                case "TD":
                    return new TripleDESCryptoServiceProvider();
                case "RC2":
                case "RC":
                    return new RC2CryptoServiceProvider();
                case "RIJNDAEL":
                case "RJ":
                    return new RijndaelManaged();
            }
            throw new Exception(String.Format("Invalid crypto provider ID ({0})!", pProviderId));
        }

        /// <summary>Converts array of float into program code text (to include it later into compiled application as constant)</summary>
        public static string FloatsToCode(float[] pArray)
        {
            string str = "{ ";
            int num = 0;
            foreach (float num2 in pArray)
            {
                string str2 = num2.ToString().Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".");
                str = str + string.Format("{0}{1}f", (num > 0) ? ", " : "", str2);
                num++;
            }
            return (str + " }");
        }

        /// <summary>Converts array of float numbers into a string (using integer part of each float as ASCII code of char)</summary>
        public static string FloatsToText(float[] pArray)
        {
            string str = "";
            foreach (float num in pArray)
            {
                int num2 = (int)Math.Truncate((double)num);
                char ch = (char)num2;
                str = str + ch;
            }
            return str;
        }

        /// <summary>Load PasswordService settings from app.config</summary>
        public static void InitializePswService()
        {
            if (!_initialized)
            {
                int num;
                string str = "XService.Security.PasswordService.";
                string str2 = ConfigurationManager.AppSettings[str + "COMPATIBILITY_MODE"];
                if (!string.IsNullOrEmpty(str2))
                {
                    COMPATIBILITY_MODE = StrUtils.GetAsBool(str2);
                }
                str2 = ConfigurationManager.AppSettings[str + "PADDED_PASSWORD"];
                if (!string.IsNullOrEmpty(str2))
                {
                    PADDED_PASSWORD = StrUtils.GetAsBool(str2);
                }
                str2 = ConfigurationManager.AppSettings[str + "DERIVE_PASSWORD"];
                if (!string.IsNullOrEmpty(str2))
                {
                    DERIVE_PASSWORD = StrUtils.GetAsBool(str2);
                }
                str2 = ConfigurationManager.AppSettings[str + "DEFAULT_PasswordEncryptionProviderId"];
                if (!string.IsNullOrEmpty(str2))
                {
                    DEFAULT_PasswordEncryptionProviderId = str2;
                }
                str2 = ConfigurationManager.AppSettings[str + "StuffData"];
                if (!string.IsNullOrEmpty(str2))
                {
                    StuffData = str2;
                }
                str2 = ConfigurationManager.AppSettings[str + "StuffData64"];
                if (!string.IsNullOrEmpty(str2))
                {
                    StuffData64 = str2;
                }
                str2 = ConfigurationManager.AppSettings[str + "RANDOM_STUFF_SEED"];
                if (!string.IsNullOrEmpty(str2) && StrUtils.GetAsInt(str2, out num))
                {
                    RANDOM_STUFF_SEED = (byte)num;
                }
                str2 = ConfigurationManager.AppSettings[str + "DERIVE_HASHCODE"];
                if (!string.IsNullOrEmpty(str2))
                {
                    DERIVE_HASHCODE = str2;
                }
                str2 = ConfigurationManager.AppSettings[str + "DERIVE_ITERATIONS"];
                if (!string.IsNullOrEmpty(str2) && StrUtils.GetAsInt(str2, out num))
                {
                    DERIVE_ITERATIONS = num;
                }
                _initialized = true;
            }
        }

        /// <summary>Pad password to specified length; in current version - it pads with chars from password itself</summary>
        public static string PadPassword(string pPassw, int pLength)
        {
            int num = 0;
            string str = pPassw;
            string str2 = pPassw;
            while (str2.Length < pLength)
            {
                str2 = str2 + str[(str.Length - (num++ % str.Length)) - 1];
            }
            if (str2.Length > 0x10)
            {
                str2 = str2.Substring(0, 0x10);
            }
            return str2;
        }

        /// <summary>Trancate array to specified length (it returns a truncated copy of source array)</summary>
        public static byte[] TruncateArray(byte[] pSrcArr, int pSize)
        {
            byte[] buffer = new byte[pSize];
            for (int i = 0; i < pSize; i++)
            {
                buffer[i] = pSrcArr[i];
            }
            return buffer;
        }

        #region Implementation details

        protected static byte[] getIvRef(SymmetricAlgorithm encoder)
        {
            byte[] buffer = my_iv_256;
            if (encoder.BlockSize == 64)
                return my_iv_64;
            if (encoder.BlockSize == 128)
                return my_iv_128;
            if (encoder.BlockSize == 192)
                return my_iv_192;
            if (encoder.BlockSize == 256)
                buffer = my_iv_256;
            return buffer;
        }


        protected static byte[] my_iv_64
        {
            get
            {
                if (_my_iv_64 == null)
                    _my_iv_64 = _actual_my_iv_64;
                return _my_iv_64;
            }
            set { _my_iv_64 = value; }
        }

        protected static byte[] my_iv_128
        {
            get
            {
                if (_my_iv_128 == null)
                    _my_iv_128 = TruncateArray(my_iv_256, 16);
                return _my_iv_128;
            }
            set { _my_iv_128 = value; }
        }

        protected static byte[] my_iv_192
        {
            get
            {
                if (_my_iv_192 == null)
                    _my_iv_192 = TruncateArray(my_iv_256, 24);
                return _my_iv_192;
            }
            set { _my_iv_192 = value; }
        }

        private static byte[] _actual_my_iv_64 = new byte[] {
            89, 76, 31,  3,  5, 17, 99, 13
        };

        protected static byte[] my_iv_256 = new byte[] {
            31, 89, 76,  5, 17, 99,  3, 13,
            88, 11, 44, 99, 97, 13, 22, 38,
            91, 12, 62, 55,  9,  1, 31, 22,
            88,  2,  3, 49, 13,  5, 19, 71
        };

        private static byte[] _my_iv_64 = null;
        private static byte[] _my_iv_128 = null;
        protected static byte[] _my_iv_192 = null;

        // password for encrypting/decrypting passwords (in ascii codes but using float numbers to avoid plain text in binary)
        private static double[] password4passwords = new double[] {
            70.1443, 105.2234, 97.39786, 116.4112, 76.5345, 117.6876, 120.70011 };

        private static string p4p = "";

        private static Random rnd = null;
        private static RNGCryptoServiceProvider cryptoRnd = null;
        public static XorRandomizer xorRnd = null;

        private static bool _initialized = false;

        #endregion // Implementation details
    }
}
