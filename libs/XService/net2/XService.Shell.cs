/*
 * Simple utilities to start shell commands and applications.
 * Written by Dmitry Bond. at Apr 21, 2007
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using XService.Utils;

namespace XService
{
    public class Shell
    {
		public static int SOURCE_Encoding = 1251;
		public static int TARGET_Encoding = 866;
		public static string STDERR_Delimiter = "\x01\x02\x03";

		public static string RunCmd(string pCmd, string pArgs)
		{
			return RunCmd(pCmd, pArgs, true);
		}

        public static string RunCmd(string pCmd, string pArgs, bool SendShellExit)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = pCmd;
            proc.StartInfo.Arguments = pArgs;
            proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.StartInfo.RedirectStandardInput = true;
			proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit(1000);
			if (SendShellExit)
			{
				proc.StandardInput.WriteLine("");
				proc.StandardInput.WriteLine("exit");
				proc.StandardInput.WriteLine("");
			}
            Thread.Sleep(1000);
			string text = FixCmdOutput(proc.StandardOutput.ReadToEnd(), SOURCE_Encoding, TARGET_Encoding);
			string errText = FixCmdOutput(proc.StandardError.ReadToEnd(), SOURCE_Encoding, TARGET_Encoding);
			if (!string.IsNullOrEmpty(errText.Trim(StrUtils.CH_SPACES)))
				text += STDERR_Delimiter + errText;

            return text;
        }

        private static char[] _ARG_DELIM_CHARS = new char[] { ' ' };

        public static string QuoteCmdPrm(string pValue)
        {
            if (pValue.IndexOfAny(_ARG_DELIM_CHARS) >= 0) pValue = "\"" + pValue + "\"";
            return pValue;
        }

        public static string FixCmdOutput(string pText, int pSrcCodePage, int pDestCodePage)
        {
            Encoding enc_src = Encoding.GetEncoding(pSrcCodePage);

            Encoding enc_trg = Encoding.GetEncoding(pDestCodePage);

            pText = TextUtils.ChangeEncoding(pText, enc_src, enc_trg);

            return pText;
        }

		/// <summary>
		/// Split specified command line to program name and program command line arguments
		/// </summary>
		/// <param name="pCmd">command line to split</param>
		/// <param name="pProgram">out: extracted program name</param>
		/// <param name="pArguments">out: program command line arguments</param>
		public static void SplitCommandToProgramAndArgs(string pCmd, out string pProgram, out string pArguments)
		{
			pProgram = null;
			pArguments = null;
			char chQuote = '\0';
			string collector = "";
			for (int i = 0; i < pCmd.Length; i++)
			{
				char ch = pCmd[i];
				collector += ch;
				if (ch == '\"')
				{
					if (chQuote == '\0') // opening quote
					{
						chQuote = ch;
					}
					else // closing quote
					{
						chQuote = '\0';
						if (pProgram == null)
						{
							pProgram = collector;
						}
						else
						{
							if (pArguments == null)
								pArguments = collector;
							else
								pArguments += collector;
						}
						collector = "";
					}
				}
				if (chQuote == '\0' && (ch == ' ' || ch == '\t'))
				{
					if (pProgram == null)
					{
						pProgram = collector;
					}
					else
					{
						if (pArguments == null)
							pArguments = collector;
						else
							pArguments += collector;
					}
					collector = "";
				}
			}
		}

	}
}

namespace XService.Utils
{
	public class Shell : XService.Shell
	{ 
	}
}