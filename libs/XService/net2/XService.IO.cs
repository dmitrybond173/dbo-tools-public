/*
 * Simple utilities to work with IO objects in .NET.
 * Written by Dmitry Bond. at June 14, 2006
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using XService.Utils; 

namespace XService.Utils.IO
{
    /// <summary>
    /// PathUtils - utilities to manipulate file pathes and file names
    /// </summary>
    public class PathUtils
    {
        /// <summary>Include trailing slash from filepath (both slashes are supported - DirectorySeparatorChar and AltDirectorySeparatorChar)</summary>
        /// <param name="str">Filepath to include trailing slash from</param>
        /// <returns>Filepath with included trailing slash</returns>
        public static string IncludeTrailingSlash(string str)
        {
            int n1 = str.IndexOf(Path.DirectorySeparatorChar);
            int n2 = str.IndexOf(Path.AltDirectorySeparatorChar);
            if (n1 >= 0 || (n1 < 0 && n2 < 0))
                return StrUtils.IncludeTrailing(str, Path.DirectorySeparatorChar);
            else
                return StrUtils.IncludeTrailing(str, Path.AltDirectorySeparatorChar);
        }

        private static string s_invalidFnChars = null;

        /// <summary>Returns string of chars which are invalid for filename</summary>
        public static string InvalidFilenameChars
        {
            get
            {
                if (s_invalidFnChars == null)
                {
                    char[] chArr = Path.GetInvalidFileNameChars();
                    foreach (char ch in chArr)
                    {
                        if (s_invalidFnChars == null) s_invalidFnChars = "" + ch;
                        else s_invalidFnChars += ch;
                    }
                }
                return s_invalidFnChars;
            }
        }

        private static string s_invalidPathChars = null;

        /// <summary>Returns string of chars which are invalid for file path</summary>
        public static string InvalidPathChars
        {
            get
            {
                if (s_invalidPathChars == null)
                {
                    char[] chArr = Path.GetInvalidPathChars();
                    foreach (char ch in chArr)
                    {
                        if (s_invalidPathChars == null) s_invalidPathChars = "" + ch;
                        else s_invalidPathChars += ch;
                    }
                }
                return s_invalidPathChars;
            }
        }

        /// <summary>Exclude trailing slash from filepath (both slashes are supported - DirectorySeparatorChar and AltDirectorySeparatorChar)</summary>
        /// <param name="str">Filepath to exclude trailing slash from</param>
        /// <returns>Filepath with excluded trailing slash</returns>
        public static string ExcludeTrailingSlash(string str)
        {
            int n1 = str.IndexOf(Path.DirectorySeparatorChar);
            int n2 = str.IndexOf(Path.AltDirectorySeparatorChar);
            if (n1 >= 0 || (n1 < 0 && n2 < 0))
                return StrUtils.ExcludeTrailing(str, Path.DirectorySeparatorChar);
            else
                return StrUtils.ExcludeTrailing(str, Path.AltDirectorySeparatorChar);
        }

        /// <summary>Expand environment variables and replace ref to home-directory ("~/" or "~\") with home directory path</summary>
        /// <param name="path">Filepath to replace </param>
        /// <returns>Filepath with expanced environment variables and replaced ref to home-directory</returns>
        public static string FixPath(string path)
        {
            if (path.IndexOf('%') >= 0)
                path = Environment.ExpandEnvironmentVariables(path);
            if (path.StartsWith("~/"))
                path = path.Replace("~/", IncludeTrailingSlash(AppDomain.CurrentDomain.BaseDirectory));
            else
                if (path.StartsWith(@"~\"))
                    path = path.Replace(@"~\", IncludeTrailingSlash(AppDomain.CurrentDomain.BaseDirectory));
            return FixDirSeparators(path);
        }

        /// <summary>Replace wrong chars in filename</summary>
        /// <param name="path">Filename to fix</param>
        /// <returns>Fixed filename</returns>
        public static string FixFilename(string pFilename)
        {
            int fixCount = 0;
            char[] chArr = pFilename.ToCharArray();
            for (int i = 0; i < chArr.Length; i++)
            {
                if (InvalidFilenameChars.IndexOf(chArr[i]) >= 0)
                {
                    fixCount++;
                    chArr[i] = '_';
                }
            }
            if (fixCount > 0)
            {
                pFilename = "";
                foreach (char ch in chArr)
                {
                    pFilename += ch;
                }
            }
            return pFilename;
        }

        /// <summary>Adjust file name/path to use primary directory separator</summary>
        /// <param name="name">Filename(path)</param>
        /// <returns>Adjusted filename</returns>
        public static string FixDirSeparators(string name)
        {
            // if both types of directory separators found ...
            if (name.IndexOf(Path.DirectorySeparatorChar) >= 0 && name.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
                name = name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return name;
        }

        /// <summary>Add specified suffix to filename (insert in front of file extension)</summary>
        /// <param name="pFilename">Filename to insert suffix into</param>
        /// <param name="pSuffix">Suffix to insert</param>
        /// <returns>Filename with inserted suffix</returns>
        public static string GetSuffixedFilename(string pFilename, string pSuffix)
        {
            if (string.IsNullOrEmpty(pSuffix)) return pFilename;

            string dir = Path.GetDirectoryName(pFilename);
            string clearFn = Path.GetFileNameWithoutExtension(pFilename);
            string ext = Path.GetExtension(pFilename);
            if (!string.IsNullOrEmpty(dir))
                dir += Path.DirectorySeparatorChar;
            pFilename = dir + clearFn + pSuffix + ext;
            return pFilename;
        }

        /// <summary>Load list of files matching specified filemask from specified path.</summary>
        /// <param name="pPath">path to search files in</param>
        /// <param name="pFilespec">filespec (files mask), could be multiple delimited by "|" (pipe char)</param>
        /// <returns>List of files matching specified filemask</returns>
        public static List<FileInfo> LoadFilesList(string pPath, string pFilespec)
        {
            List<FileInfo> filesList = new List<FileInfo>();
            LoadFilesList(filesList, pPath, pFilespec);
            return filesList;
        }

        /// <summary>Load list of files matching specified filemask from specified path.</summary>
        /// <param name="pTargetList">List to add files into</param>
        /// <param name="pPath">path to search files in</param>
        /// <param name="pFilespec">filespec (files mask), could be multiple delimited by "|" (pipe char)</param>
        /// <returns>Number of files added to list</returns>
        public static int LoadFilesList(List<FileInfo> pTargetList, string pPath, string pFilespec)
        {
            int savedCount = pTargetList.Count;
            DirectoryInfo dir = (pPath != null ? new DirectoryInfo(pPath) : new DirectoryInfo(Directory.GetCurrentDirectory()));
            string[] filespecs = pFilespec.Split('|');
            foreach (string fs in filespecs)
            {
                FileInfo[] files = dir.GetFiles(fs);
                if (files.Length > 0)
                    pTargetList.Capacity += files.Length;
                foreach (FileInfo item in files)
                {
                    pTargetList.Add(item);
                }
            }
            return pTargetList.Count - savedCount;
        }

        /// <summary>Load list of files matching specified filemask from specified path.</summary>
        /// <param name="pTargetList">List to add files into</param>
        /// <param name="pPath">path to search files in</param>
        /// <param name="pFilespec">filespec (files mask), could be multiple delimited by "|" (pipe char)</param>
        /// <param name="pRecursive">Scan recursively subdirectories</param>
        /// <returns>Number of files added to list</returns>
        public static int LoadFilesList(List<FileInfo> pTargetList, string pPath, string pFilespec, bool pRecursive)
        {
            int cnt = LoadFilesList(pTargetList, pPath, pFilespec);
            if (!pRecursive) return cnt;

            DirectoryInfo dir = new DirectoryInfo(pPath);
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo di in dirs)
            {
                cnt += LoadFilesList(pTargetList, di.FullName, pFilespec, pRecursive);                
            }
            return cnt;
        }

    } /* PathUtils */


    /// <summary>
    /// StreamUtils - set of service routines providing extra possibilities of Stream handling
    /// </summary>
    public class StreamUtils
    {
        /// <summary>Load bytes from stream into byte[] array</summary>
        /// <param name="pStrm">Source stream</param>
        /// <param name="pFromPos">From position in stream; when less than 0 then it will load from be from current position</param>
        /// <param name="pCount">Number of bytes to load; when less than 0 then it will load rest of bytes from current position</param>
        /// <returns></returns>
        public static byte[] StreamToBytes(Stream pStrm, int pFromPos, int pCount)
        {
            long saved_pos = pStrm.Position;
            if (pFromPos >= 0)
                pStrm.Seek(pFromPos, SeekOrigin.Begin);
            if (pCount < 0)
                pCount = (int)(pStrm.Length - pStrm.Position);
            byte[] buffer = new byte[pCount];
            pStrm.Read(buffer, 0, pCount);
            return buffer;
        }

        public static string StreamToString(Stream pStrm, int pFromPos, int pCount, Encoding pEnc)
        {
            byte[] buffer = StreamToBytes(pStrm, pFromPos, pCount);
            if (pEnc == null)
                pEnc = Encoding.Default;
            string data = pEnc.GetString(buffer);
            return data;
        }

        public static void StringToStream(string pText, Stream pStream, Encoding pEnc)
        {
            using (StreamWriter sw = (pEnc != null ? new StreamWriter(pStream, pEnc) : new StreamWriter(pStream)))
            {
                sw.Write(pText);
            }
        }

        /// <summary>Calculate hash code of default type for specified stream</summary>
        /// <param name="pStrm">Source stream to calculate hash code for data in it</param>
        /// <returns>String representation of hashcode</returns>
        public static string CalculateHash(Stream pStrm)
        {
            return CalculateHashEx(pStrm, "MD5");
        }

        /// <summary>Calculate hash code of specified type for specified stream</summary>
        /// <param name="pStrm">Source stream to calculate hash code for data in it</param>
        /// <param name="pHashName">Type of hashcode to use: MD5, SHA1, SHA256, SHA384, SHA512</param>
        /// <returns>String representation of hashcode</returns>
        public static string CalculateHashEx(Stream pStrm, string pHashName)
        {
            using (HashAlgorithm engine = HashAlgorithm.Create(pHashName))
            {
                byte[] hash = CalculateHashBytesEx(pStrm, pHashName);
                string txt = "";
                for (int i = 0; i < hash.Length; i++)
                {
                    txt += hash[i].ToString("X2");
                }
                return txt;
            }
        }

        /// <summary>Calculate hash code of specified type for specified stream</summary>
        /// <param name="pStrm">Source stream to calculate hash code for data in it</param>
        /// <param name="pHashName">Type of hashcode to use: MD5, SHA1, SHA256, SHA384, SHA512</param>
        /// <returns>Original bytes of calculated hashcode</returns>
        public static byte[] CalculateHashBytesEx(Stream pStrm, string pHashName)
        {
            using (HashAlgorithm engine = HashAlgorithm.Create(pHashName))
            {
                engine.Initialize();
                byte[] hash = engine.ComputeHash(pStrm);
                return hash;
            }
        }
    }


    /// <summary>
    /// FileUtils - set of service routines providing extra possibilities of File handling
    /// </summary>
    public class FileUtils
    {
        public static string CalculateHash(string pFilename)
        {
            return CalculateHashEx(pFilename, "MD5");
        }

        public static string CalculateHashEx(string pFilename, string pHashName)
        {
            using (HashAlgorithm engine = HashAlgorithm.Create(pHashName))
            {
                engine.Initialize();
                using (Stream strm = File.OpenRead(pFilename))
                {
                    string txt = "";
                    byte[] hash = engine.ComputeHash(strm);
                    for (int i = 0; i < hash.Length; i++)
                    {
                        txt += hash[i].ToString("X2");
                    }
                    return txt;
                }
            }
        }

		public static byte[] LoadFile(string pFilename)
		{
			using (Stream strm = File.OpenRead(pFilename))
			{
				byte[] data = new byte[(int)strm.Length];
				strm.Read(data, 0, (int)strm.Length);
				return data;
			}
		}

        public static void WriteToFile(string pFilename, string pText)
        {
            using (StreamWriter sw = File.CreateText(pFilename))
            {
                sw.Write(pText);
            }
        }

        public static FileAttributes UndeletableFileAttributes = FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System;

        public static bool ForceDelete(FileInfo pFile)
        {
            if ((pFile.Attributes & UndeletableFileAttributes) != 0)
            {
                pFile.Attributes &= ~UndeletableFileAttributes;
            }
            pFile.Delete();
            return pFile.Exists;
        }

        public static bool ForceDelete(string pFilename)
        {
            return ForceDelete(new FileInfo(pFilename));
        }

        public static void FixFileAttributes(FileInfo pFi, FileAttributes pAttrsToRemove)
        {
            if ((pFi.Attributes & pAttrsToRemove) == pAttrsToRemove)
            {
                pFi.Attributes &= ~pAttrsToRemove;
            }
        }

		public static string STR_INVALID_FILENAME_CHARS = 
			"\\/?*:<>|\"^";

		public static string FixFilename(string pFilename)
		{
			string fn = "";
			for (int i = 0; i < pFilename.Length; i++)
			{
				char ch = pFilename[i];
				if (STR_INVALID_FILENAME_CHARS.IndexOf(ch) >= 0)
					ch = '_';
				fn += ch;
			}
			return fn;
		}
	}
}

namespace XService.Utils
{
	public class PathUtils : XService.Utils.IO.PathUtils
	{
	}

	public class StreamUtils : XService.Utils.IO.StreamUtils
	{ 
	}

	public class FileUtils : XService.Utils.IO.FileUtils
	{ 
	}

}
