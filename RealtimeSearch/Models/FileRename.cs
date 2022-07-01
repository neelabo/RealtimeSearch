using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileRename : BindableBase
    {
        #region Native methods

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool MoveFile(string src, string dst);
        }

        #endregion Native methods


        private string _error = "";


        public FileRename()
        {
        }


        public event EventHandler<RenamedEventArgs>? Renamed;


        public string Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }


        /// <summary>
        /// エラー情報をクリア
        /// </summary>
        public void ClearError()
        {
            _error = "";
        }

        /// <summary>
        /// 無効なファイル文字が含まれていないか調べる
        /// </summary>
        /// <param name="filename">調べる文字列</param>
        /// <returns>含まれていた無効な文字</returns>
        public char CheckInvalidFileNameChars(string filename)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            int invalidCharsIndex = filename.IndexOfAny(invalidChars);
            return invalidCharsIndex >= 0 ? filename[invalidCharsIndex] : '\0';
        }

        /// <summary>
        /// ファイルかディレクトリの存在をチェック
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Exists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        /// <summary>
        /// 名前変更
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newNameSource"></param>
        /// <returns>成功した場合は新しいフルパス。失敗した場合はnull</returns>
        public string? Rename(string folder, string oldName, string newNameSource)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newNameSource)) return null;
            if (oldName == newNameSource) return null;

            var newName = newNameSource;

            string src = Path.Combine(folder, oldName);
            string dst = Path.Combine(folder, newName);

            // 重複ファイル名回避
            //if (string.Compare(src, dst, true) != 0 && Exists(dst))
            if (string.Compare(src, dst, true) != 0 && Exists(dst))
            {
                newName = CreateUniqueFileName(folder, newName);
                dst = Path.Combine(folder, newName);
            }

            // 名前変更実行
            try
            {
                if (Directory.Exists(src))
                {
                    RenameDirectory(src, dst);
                }
                else
                {
                    File.Move(src, dst);
                }

                Renamed?.Invoke(this, new RenamedEventArgs(WatcherChangeTypes.Renamed, folder, newName, oldName));
            }
            catch (Exception ex)
            {
                Error = $"名前の変更に失敗しました。\n\n{ex.Message}";
                return null;
            }

            return dst;
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
                    throw new IOException($"cannot rename directory ({error})");
                }
            }
        }

        public string CreateUniqueFileName(string directory, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("filename must not be empty.");

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
    }


}
