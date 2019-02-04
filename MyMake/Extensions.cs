using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MyMake
{
    static class Extensions
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr PathCombine([Out] StringBuilder lpszDest, string lpszDir, string lpszFile);

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathRelativePathTo([Out] StringBuilder pszPath, [In] string pszFrom, [In] System.IO.FileAttributes dwAttrFrom, [In] string pszTo, [In] System.IO.FileAttributes dwAttrTo);

        private static string GetAbsolutePath(DirectoryInfo basePath, string relativePath)
        {
            StringBuilder sb = new StringBuilder(2048);
            IntPtr res = PathCombine(sb, basePath.FullName, relativePath);
            if (res == IntPtr.Zero)
                throw new Exception("絶対パスの取得に失敗しました。");
            return sb.ToString();
        }

        private static string GetRelativePath(DirectoryInfo basePath, string absolutePath)
        {
            StringBuilder sb = new StringBuilder(260);
            bool res = PathRelativePathTo(sb, basePath.FullName, FileAttributes.Directory, absolutePath, FileAttributes.Normal);
            if (!res)
                throw new Exception("相対パスの取得に失敗しました。");
            return sb.ToString();
        }

        public static FileInfo GetFile(this DirectoryInfo base_dir, string file_name)
        {
            return (new FileInfo(GetAbsolutePath(base_dir, file_name)));
        }

        public static DirectoryInfo GetDirectory(this DirectoryInfo base_dir, string dir_name)
        {
            return (new DirectoryInfo(GetAbsolutePath(base_dir, dir_name)));
        }

        public static string GetRelativePath(this DirectoryInfo base_dir, DirectoryInfo dir_path)
        {
            return (GetRelativePath(base_dir, dir_path.FullName));
        }

        public static string GetRelativePath(this DirectoryInfo base_dir, FileInfo file_path)
        {
            return (GetRelativePath(base_dir, file_path.FullName));
        }
    }

}
