using System;
using System.Collections.Generic;
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
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool MoveFile(string src, string dst);
        }

        #endregion Native methods

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
            ShellFileOperation.Delete(App.Current.MainWindow, files, true);
        }


        public static void Rename(string src, string dst)
        {
            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst)) throw new NotSupportedException();

            if (src == dst) return;

            if (Directory.Exists(src))
            {
                RenameDirectory(src, dst);
            }
            else
            {
                File.Move(src, dst);
            }
        }

        private static void RenameDirectory(string src, string dst)
        {
            if (!Directory.Exists(src)) throw new DirectoryNotFoundException();

            if (src == dst) return;

            if (string.Compare(src, dst, true) != 0)
            {
                Directory.Move(src, dst);
            }
            else
            {
                var isSuccess = NativeMethods.MoveFile(src, dst);
                if (!isSuccess)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new IOException($"Cannot rename directory ({error})");
                }
            }
        }
    }

}
