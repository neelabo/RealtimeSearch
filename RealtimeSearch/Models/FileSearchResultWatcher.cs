//#define LOCAL_DEBUG
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileSearchResultWatcher : IDisposable, ISearchResult<FileItem>
    {
        /// <summary>
        /// 所属する検索エンジン
        /// </summary>
        private readonly FileSearchEngine _engine;

        /// <summary>
        /// 監視する検索結果
        /// </summary>
        private readonly SearchResult<FileItem> _result;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="result"></param>
        public FileSearchResultWatcher(FileSearchEngine engine, SearchResult<FileItem> result)
        {
            _engine = engine;
            _result = result;

            _engine.Tree.AddContentChanged += Tree_AddContentChanged;
            _engine.Tree.RemoveContentChanged += Tree_RemoveContentChanged;
        }


        /// <summary>
        /// 検索結果変更
        /// </summary>
        public event EventHandler<CollectionChangedEventArgs<FileItem>>? CollectionChanged;


        // TODO: 投げっぱなし非同期なので例外処理をここで行う
        private async void Tree_AddContentChanged(object? sender, FileItemTree.FileTreeContentChangedEventArgs e)
        {
            if (_disposedValue) return;

            var entries = new List<FileItem>() { e.FileItem };
            var items = await _engine.SearchAsync(_result.Keyword, entries, CancellationToken.None);

            foreach (var item in items)
            {
                Trace($"Add: {item.Path}");
                _result.Items.Add(item);
                CollectionChanged?.Invoke(this, new CollectionChangedEventArgs<FileItem>(CollectionChangedAction.Add, item));
            }
        }


        private void Tree_RemoveContentChanged(object? sender, FileItemTree.FileTreeContentChangedEventArgs e)
        {
            if (_disposedValue) return;

            Trace($"Remove: {e.FileItem.Path}");
            _result.Items.Remove(e.FileItem);
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs<FileItem>(CollectionChangedAction.Remove, e.FileItem));
        }


        #region ISearchResult Support

        /// <summary>
        /// 検索結果項目
        /// </summary>
        public ObservableCollection<FileItem> Items => _result.Items;

        /// <summary>
        /// 検索キーワード
        /// </summary>
        public string Keyword => _result.Keyword;

        /// <summary>
        /// 検索失敗時の例外
        /// </summary>
        public Exception? Exception => _result.Exception;

        #endregion

        #region IDisposable Support
        private bool _disposedValue = false;

        protected void ThrowIfDisposed()
        {
            if (_disposedValue) throw new ObjectDisposedException(GetType().FullName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _engine.Tree.AddContentChanged -= Tree_AddContentChanged;
                    _engine.Tree.RemoveContentChanged -= Tree_RemoveContentChanged;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        [Conditional("LOCAL_DEBUG")]
        private void Trace(string s, params object[] args)
        {
            Debug.WriteLine($"{this.GetType().Name}: {string.Format(s, args)}");
        }

    }

}
