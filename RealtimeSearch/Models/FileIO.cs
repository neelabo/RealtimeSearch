using System;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileIO : BindableBase
    {
        private string _error = "";

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
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
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
            return System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
        }

        /// <summary>
        /// 名前変更
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newName"></param>
        /// <returns>成功した場合は新しいフルパス。失敗した場合はnull</returns>
        public string? Rename(string oldName, string newName)
        {
            string src = oldName;
            string dst = newName;
            if (src == dst) return null;

            // 重複ファイル名回避
            if (string.Compare(src, dst, true) != 0 && Exists(dst))
            {
                string dstBase = dst;
                string dir = System.IO.Path.GetDirectoryName(dst) ?? "";
                string name = System.IO.Path.GetFileNameWithoutExtension(dst);
                string ext = System.IO.Path.GetExtension(dst);
                int count = 1;

                do
                {
                    dst = $"{dir}\\{name} ({++count}){ext}";
                }
                while (Exists(dst));
            }

            // 名前変更実行
            try
            {
                if (System.IO.Directory.Exists(src))
                {
                    System.IO.Directory.Move(src, dst);
                }
                else
                {
                    System.IO.File.Move(src, dst);
                }
            }
            catch (Exception ex)
            {
                Error =$"名前の変更に失敗しました。\n\n{ex.Message}";
                return null;
            }

            return dst;
        }
    }


}
