using System;
using System.IO;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileRename : BindableBase
    {
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
            if (string.Compare(src, dst, true) != 0 && FileSystem.Exists(dst))
            {
                newName = FileSystem.CreateUniqueFileName(folder, newName);
                dst = Path.Combine(folder, newName);
            }

            // 名前変更実行
            try
            {
                FileSystem.Rename(src, dst);
                Renamed?.Invoke(this, new RenamedEventArgs(WatcherChangeTypes.Renamed, folder, newName, oldName));
            }
            catch (Exception ex)
            {
                Error = $"名前の変更に失敗しました。\n\n{ex.Message}";
                return null;
            }

            return dst;
        }
    }

}
