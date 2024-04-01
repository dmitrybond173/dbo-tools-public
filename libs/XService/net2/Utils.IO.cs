using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XService.Utils; 

namespace XService.Utils.IO
{
    /// <summary>
    /// PathUtils - utilities to manipulate file pathes and file names.
    /// </summary>
    public sealed class PathUtils
    {
        public static string IncludeTrailingSlash(string str)
        {
            int n1 = str.IndexOf(Path.DirectorySeparatorChar);
            int n2 = str.IndexOf(Path.AltDirectorySeparatorChar);
            if (n1 >= 0 || (n1 < 0 && n2 < 0))
                return StrUtils.IncludeTrailing(str, Path.DirectorySeparatorChar);
            else
                return StrUtils.IncludeTrailing(str, Path.AltDirectorySeparatorChar);
        }

        public static string ExcludeTrailingSlash(string str)
        {
            int n1 = str.IndexOf(Path.DirectorySeparatorChar);
            int n2 = str.IndexOf(Path.AltDirectorySeparatorChar);
            if (n1 >= 0 || (n1 < 0 && n2 < 0))
                return StrUtils.ExcludeTrailing(str, Path.DirectorySeparatorChar);
            else
                return StrUtils.ExcludeTrailing(str, Path.AltDirectorySeparatorChar);
        }

        public static string FixPath(string path)
        {
            if (path.StartsWith("~/"))
                path = path.Replace("~/", IncludeTrailingSlash(AppDomain.CurrentDomain.BaseDirectory));
            else
                if (path.StartsWith(@"~\"))
                    path = path.Replace(@"~\", IncludeTrailingSlash(AppDomain.CurrentDomain.BaseDirectory));
            return FixDirSeparators(path);
        }

        public static string FixDirSeparators(string name)
        {
            // if both types of directory separators found ...
            if (name.IndexOf(Path.DirectorySeparatorChar) >= 0 && name.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
                name = name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return name;
        }

        /// <summary>
        /// Load list of files rom specified path of specified filespec.
        /// </summary>
        /// <param name="path">path to search files in</param>
        /// <param name="mask">filespec (files mask)</param>
        /// <returns></returns>
        public static List<FileInfo> LoadFilesList(string path, string filespec)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles(filespec);
            List<FileInfo> list = new List<FileInfo>(files.Length);
            foreach (FileInfo item in files)
            {
                list.Add(item);
            }
            return list;
        }

    } /* PathUtils */

}
