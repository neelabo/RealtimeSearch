using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NeeLaboratory.RealtimeSearch
{
    public static class FileSystem
    {
        #region Native methods

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool MoveFile(string src, string dst);
        }

        #endregion Native methods

        private static IntPtr _hWnd;

        /// <summary>
        /// set owner window handle
        /// </summary>
        /// <param name="hWnd"></param>
        public static void SetOwnerWindowHandle(IntPtr hWnd)
        {
            _hWnd = hWnd;
        }

        /// <summary>
        /// ファイルかディレクトリの存在をチェック
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool Exists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        /// <summary>
        /// FileSystemInfo 作成
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static FileSystemInfo CreateFileSystemInfo(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists) return directoryInfo;
            return new FileInfo(path);
        }

        /// <summary>
        /// 無効なファイル文字が含まれていないか調べる
        /// </summary>
        /// <param name="filename">調べる文字列</param>
        /// <returns>含まれていた無効な文字</returns>
        public static char FindInvalidFileNameChar(string filename)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            int invalidCharsIndex = filename.IndexOfAny(invalidChars);
            return invalidCharsIndex >= 0 ? filename[invalidCharsIndex] : '\0';
        }


        public static string CreateUniqueFileName(string directory, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("Filename must not be empty.");

            var newFileName = filename;
            var name = Path.GetFileNameWithoutExtension(filename);
            var ext = Path.GetExtension(filename);
            int count = 1;

            var regex = new Regex(@"^(.+) \((\d+)\)$");
            var match = regex.Match(name);
            if (match.Success)
            {
                name = match.Groups[1].Value.Trim();
                count = int.Parse(match.Groups[2].Value);
            }

            while (Exists(Path.Combine(directory, newFileName)))
            {
                count++;
                newFileName = $"{name} ({count}){ext}";
            }

            return newFileName;
        }


        public static void SendToRecycleBin(IList<string> files)
        {
            SendToRecycleBin(_hWnd, files);
        }

        public static void SendToRecycleBin(IntPtr hWnd, IList<string> files)
        {
            ShellFileOperation.Delete(hWnd, files, true);
        }

        public static void Rename(string src, string dst)
        {
            Rename(_hWnd, src, dst);
        }

        public static void Rename(IntPtr hWnd, string src, string dst)
        {
            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst)) throw new ArgumentNullException();
            if (Path.GetDirectoryName(src) != Path.GetDirectoryName(dst)) throw new ArgumentException("Different directories");

            if (src == dst) return;

            var srcInfo = CreateFileSystemInfo(src);
            if (!srcInfo.Exists)
            {
                throw new FileNotFoundException(src);
            }

            ShellFileOperation.Rename(hWnd, src, dst, ShellFileOperation.OperationFlags.Default | ShellFileOperation.OperationFlags.RenameOnCollision);
        }


        public static void MergeDirectory(string src, string dst)
        {
            // TODO: 親ディレクトリをその子ディレクトリにマージしようとしたときに例外を発生させる

            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst)) throw new ArgumentNullException();

            Debug.WriteLine($"MergeDirectory: {src} -> {dst}");
            var srcDirectory = new DirectoryInfo(src);
            var dstDirectory = new DirectoryInfo(dst);

            if (!srcDirectory.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found: {srcDirectory.FullName}");
            }

            if (!dstDirectory.Exists)
            {
                Debug.WriteLine($"Move.directory: {src} -> {dst}");
                srcDirectory.MoveTo(dst);
                return;
            }

            foreach (var file in srcDirectory.GetFiles())
            {
                var dstFullPath = Path.Combine(dst, FileSystem.CreateUniqueFileName(dst, file.Name));
                Debug.WriteLine($"Move.file: {file.FullName} -> {dstFullPath}");
                file.MoveTo(dstFullPath);
            }


            foreach (var directory in srcDirectory.GetDirectories())
            {
                var dstFullPath = Path.Combine(dst, directory.Name);
                MergeDirectory(directory.FullName, dstFullPath);
            }

            if (srcDirectory.Exists)
            {
                if (srcDirectory.GetFileSystemInfos().Length == 0)
                {
                    srcDirectory.Delete();
                }
            }
        }
    }

}
