using System;
using System.Collections.Generic;
using System.Text;
using XService.Utils;

namespace XService.Security
{
    /// <summary>
    /// SecureText is a class to encode/decode sensitive data such as password and so on.
    /// </summary>
    public sealed class SecureText
    {
        private static SecureText _instance = null;

        /// <summary>Return instance of SecureText object to work with</summary>
        public static SecureText Instance
        {
            get
            {
                lock (typeof(SecureText))
                {
                    if (_instance == null)
                    {
                        _instance = new SecureText();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Set encoding method. Return previously used method.
        /// Where pMethodId could have one of values: 
        /// -1 - return current MethodId, do not change anything;
        /// 0 - reset to default (recommened) MethodId;
        /// 1..N - set MethodId.
        /// </summary>
        public int SetSecurMethod(int pMethodId)
        {
            if (pMethodId < 0)
                return methodId;

            if (pMethodId == 0)
            {
                methodId = DEFAULT_MethodID;
                return methodId;
            }

            methodId = pMethodId;
            return methodId;
        }

        /// <summary>Return current password used to encode/decode data</summary>
        public string GetSecurKey()
        {
            return this.securKey;
        }

        /// <summary>Set password to encode/decode data</summary>
        public void SetSecurKey(string pNewKey) 
        {
            if (string.IsNullOrEmpty(pNewKey))
                pNewKey = DEFAULT_SecurKey;
            this.securKey = pNewKey;
        }

        /// <summary>Encode specified text</summary>
        /// <param name="pText">Text to encode</param>
        /// <param name="pEncodedText">Encoded text</param>
        public void EncodeSecuredText(string pText, out string pEncodedText)
        {
            int binSize;
            byte[] buffer = null;
            switch (this.methodId)
            {
                case 1: binSize = encode1(pText, out buffer); break;
                case 2: binSize = encode2(pText, out buffer); break;
                default: binSize = encode1(pText, out buffer); break;
            }

            uint chk = 0;
            BitUtils.current_encoding_table = BitUtils.base64_encoding_table;
            string txt = BitUtils.x_encode(BitUtils.EEncodeBase.x6bit, buffer, out chk);
            pEncodedText = chk.ToString("X8") + "~" + txt;
        }

        /// <summary>Decode previously encoded text</summary>
        /// <param name="pText">String with previously encoded text</param>
        /// <param name="pDecodedText">Decoded text</param>
        /// <returns>Returns true in case when text was successfully decoded, otherwise returns false and pDecodedText set to null</returns>
        public bool DecodeSecuredText(string pText, out string pDecodedText)
        {
            pDecodedText = null;
            if (string.IsNullOrEmpty(pText))
                return false;

            int mi = methodId;
            if (pText.Length >= 10 && pText[1] == '!')
            {
                char ch = pText[0];
                if (isCharInRange(ch, '0', '9'))
                    mi = ch - (int)'0';
                else if (isCharInRange(ch, 'A', 'Z'))
                    mi = 10 + (ch - (int)'A');
                else if (isCharInRange(ch, 'a', 'z'))
                    mi = 10 + (ch - (int)'a');
                else
                    return false;
                pText = pText.Remove(0, 2); // remove {MethodId} + '!' from beginning of string
            }

            return doDecodeSecuredText(mi, pText, out pDecodedText);
        }

        /// <summary>Ensure to include methodId into encoded text</summary>
        /// <param name="pText">String with encoded text</param>
        /// <param name="pMethodId">MethodID to include into encoded text</param>
        public void IncludeMethodId(ref string pText, int pMethodId)
        {
            if (pMethodId <= 0) pMethodId = methodId;
            pText = pMethodId.ToString() + "!" + pText;
        }

        /// <summary>
        /// Validate if specified text match pattern of encoded text.
        /// Assume it should with MethodId and checksum - full form of encrypted text
        /// </summary>
        /// <param name="pText">Text to validate</param>
        /// <returns>Returns true if specified text match pattern of encoded text</returns>
        public bool IsEcryptedPattern(string pText)
        {
            //         _123456789_12345
            // Format: m!xxxxxxxx~[...]
            // Assume it cannot be shorter than 16 chars length: methodId(2) + checksum(9) + MinPasswordLength(5)
            if (string.IsNullOrEmpty(pText) || pText.Length < 16) return false;

            // expect valid checksum
            bool isValidHex = true;
            for (int i = 2; i < 10; i++)
            {
                char ch = pText[i];
                isValidHex = isValidHex && isCharInRange(ch, '0', '9') || isCharInRange(ch, 'A', 'F') || isCharInRange(ch, 'a', 'f');
                if (!isValidHex) break;
            }

            return isCharInRange(pText[0], '0', '9')
              && (pText[1] == '!')
              && (pText[10] == '~')
              && isValidHex;
        }

        #region Implementation details

        private int encode1(string pText, out byte[] pBuffer)
        {
            pBuffer = Encoding.ASCII.GetBytes(pText);
            
            for (int i=0; i<pBuffer.Length; i++)
            {
                int x = pBuffer[i];
                x = (int)BitUtils.I32_bitSwap1((uint)x);
                x = (int)BitUtils.I32_bitSwap2((uint)x);
                x = (int)BitUtils.I32_bitSwap3((uint)x);
                pBuffer[i] = (byte)x;
            }

            return pBuffer.Length;
        }

        private int encode2(string pText, out byte[] pBuffer)
        {
            // evaluate target buffer size
            int dst_len = ((pText.Length + sizeof(uint) + 63) / 64) * 64;

            pBuffer = new byte[dst_len];
            Array.Clear(pBuffer, 0, pBuffer.Length);

            // put some random data together with text to encode
            int x = xRnd.Next();
            byte[] valueArr = BitConverter.GetBytes(x);
            Array.Copy(valueArr, 0, pBuffer, 0, sizeof(int));
            byte[] txtArr = Encoding.ASCII.GetBytes(pText);
            Array.Copy(txtArr, 0, pBuffer, sizeof(int), txtArr.Length);
            // put End-Of-Password marker 
            int i = sizeof(int) + pText.Length;            
            pBuffer[i] = (byte)'\xFF';

            // fill up extra part of buffer with random staff
            i++;
            while (i < dst_len)
            {
                x = xRnd.Next();
                int sz = sizeof(int);
                if ((i + sz) >= dst_len) sz = dst_len - i;
                valueArr = BitConverter.GetBytes(x);
                Array.Copy(valueArr, 0, pBuffer, i, sz);
                i += sz;
            }

            // prepare keyBuffer
            byte[] key = Encoding.ASCII.GetBytes(this.securKey);
            byte[] keyBuffer = new byte[this.securKey.Length + sizeof(int)];
            Array.Clear(keyBuffer, 0, key.Length);
            Array.Copy(key, 0, keyBuffer, 0, key.Length);

            // encode buffer  
            int iKey;
            uint xv, keyX ;
            for (i = 0, iKey = 0; i < dst_len; i += sizeof(int))
            {
                xv = BitConverter.ToUInt32(pBuffer, i);
                keyX = BitConverter.ToUInt32(keyBuffer, iKey);
                xv = xv ^ keyX;
                xv = BitUtils.I32_bitRotate(xv);
                xv = BitUtils.I32_bitSwap3(xv);
                xv = BitUtils.I32_bitSwap1(xv);
                xv = BitUtils.I32_bitSwap2(xv);
                valueArr = BitConverter.GetBytes(xv);
                Array.Copy(valueArr, 0, pBuffer, i, sizeof(uint));

                iKey++;
                if (iKey >= this.securKey.Length) iKey = 0;
            }

            // additional encoding - run bits mixer over the buffer
            for (i = 1; i < (dst_len - sizeof(int)); i++)
            {
                xv = BitConverter.ToUInt32(pBuffer, i);
                xv = BitUtils.I32_bitSwap16(xv);
                xv = BitUtils.I32_bitSwap3(xv);
                xv = BitUtils.I32_bitSwap8(xv);
                xv = BitUtils.I32_bitSwap4(xv);
                valueArr = BitConverter.GetBytes(xv);
                Array.Copy(valueArr, 0, pBuffer, i, sizeof(uint));
            }

            return pBuffer.Length;
        }

        private bool doDecodeSecuredText(int pMethodId, string pText, out string pDecodedText)
        {
            pDecodedText = null;
            int p = pText.IndexOf('~');
            if (p < 0) return false;

            // _123456789_
            // AABBCCDD~vbh

            string chkSumStr = pText.Substring(0, p);
            pText = pText.Remove(0, p + 1);
            uint chkSum = Convert.ToUInt32(chkSumStr, 16);

            int bytesCount = 0;
            uint dataChkSum;
            byte[] decodedData;
            BitUtils.current_encoding_table = BitUtils.base64_encoding_table;
            if (!BitUtils.x_decode(BitUtils.EEncodeBase.x6bit, pText, out decodedData, ref bytesCount, out dataChkSum))
                return false;

            if (dataChkSum != chkSum)
                return false;

            switch (pMethodId)
            {
                case 1: decode1(bytesCount, decodedData, out pDecodedText); break;
                case 2: decode2(bytesCount, decodedData, out pDecodedText); break;
                default: decode1(bytesCount, decodedData, out pDecodedText); break;
            }

            return true;
        }

        private bool decode1(int pDataSize, byte[] pBuffer, out string pText)
        {
            for (int i = 0; i < pDataSize; i++)
            {
                int x = pBuffer[i];
                x = (int)BitUtils.I32_bitSwap3((uint)x);
                x = (int)BitUtils.I32_bitSwap2((uint)x);
                x = (int)BitUtils.I32_bitSwap1((uint)x);
                pBuffer[i] = (byte)x;
            }
            pText = Encoding.ASCII.GetString(pBuffer, 0, pDataSize);
            return true;
        }

        private bool decode2(int pDataSize, byte[] pBuffer, out string pText)
        {
            pText = null;
            if ( (pDataSize % 64) != 0 ) return false;

            int i;
            uint xv;
            byte[] valueArr;

            // additional decoding - run bits unmixer over the buffer
            for (i=(pDataSize - sizeof(int))-1; i > 0; i--)
            {
                xv = BitConverter.ToUInt32(pBuffer, i);
                xv = BitUtils.I32_bitSwap4(xv);
                xv = BitUtils.I32_bitSwap8(xv);
                xv = BitUtils.I32_bitSwap3(xv);
                xv = BitUtils.I32_bitSwap16(xv);
                valueArr = BitConverter.GetBytes(xv);
                Array.Copy(valueArr, 0, pBuffer, i, sizeof(uint));
            }

            // prepare keyBuffer
            byte[] key = Encoding.ASCII.GetBytes(this.securKey);
            byte[] keyBuffer = new byte[this.securKey.Length + sizeof(int)];
            Array.Clear(keyBuffer, 0, key.Length);
            Array.Copy(key, 0, keyBuffer, 0, key.Length);

            // decode buffer  
            int iKey;
            uint keyX;
            for (i = 0, iKey = 0; i < pDataSize; i += sizeof(int))
            {
                xv = BitConverter.ToUInt32(pBuffer, i);
                xv = BitUtils.I32_bitSwap2(xv);
                xv = BitUtils.I32_bitSwap1(xv);
                xv = BitUtils.I32_bitSwap3(xv);
                xv = BitUtils.I32_bitRotate(xv);
                keyX = BitConverter.ToUInt32(keyBuffer, iKey);
                xv = xv ^ keyX;
                valueArr = BitConverter.GetBytes(xv);
                Array.Copy(valueArr, 0, pBuffer, i, sizeof(uint));

                iKey++;
                if (iKey >= this.securKey.Length) iKey = 0;
            }

            i = sizeof(int);
            while (i < pDataSize && pBuffer[i] != '\xFF') i++;
            if (pBuffer[i] != '\xFF') return false;

            int src_len = i - sizeof(int);
            pText = Encoding.ASCII.GetString(pBuffer, sizeof(int), src_len);

            return true;
        }

        private static void initBytesArray(byte[] pArr, byte pValue, int pIndex, int pCount)
        {
            for (int i = pIndex; i < pCount; i++)
            {
                if ((pIndex + i) < pArr.Length)
                    pArr[pIndex + i] = pValue;
                else
                    break;
            }
        }

        private static bool isCharInRange(char ch, char pFrom, char pTo)
        {
            return (pFrom <= ch && ch <= pTo);
        }
        
        private int methodId;
        private string securKey = DEFAULT_SecurKey;
        private XorRandomizer xRnd = new XorRandomizer();

        protected const string DEFAULT_SecurKey = "Rp-BtO-P@zZw0rD";
        protected const int DEFAULT_MethodID = 1;

        #endregion // Implementation details
    }
}
