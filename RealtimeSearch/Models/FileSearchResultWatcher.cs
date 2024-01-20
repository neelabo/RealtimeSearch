#define LOCAL_DEBUG

using NeeLaboratory.Threading.Jobs;
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

        private SlimJobEngine _jobEngine;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="result"></param>
        public FileSearchResultWatcher(FileSearchEngine engine, SearchResult<FileItem> result)
        {
            _engine = engine;
            _result = result;
            _jobEngine = new SlimJobEngine(nameof(FileSearchResultWatcher));

            if (!string.IsNullOrWhiteSpace(result.Keyword))
            {
                _engine.Tree.AddContentChanged += Tree_AddContentChanged;
                _engine.Tree.RemoveContentChanged += Tree_RemoveContentChanged;
                _engine.Tree.ContentChanged += Tree_ContentChanged;
            }
        }


        /// <summary>
        /// 検索結果変更
        /// </summary>
        public event EventHandler<CollectionChangedEventArgs<FileItem>>? CollectionChanged;


        // TODO: 大量のリクエストが同時に気たときにタスクが爆発する。一定間隔でまとめて処理するように！
        // TODO: 投げっぱなし非同期なので例外処理をここで行う
        private void Tree_AddContentChanged(object? sender, FileItemTree.FileTreeContentChangedEventArgs e)
        {
            if (_disposedValue) return;

            AddSearch(e.FileItem);
        }

        private void Tree_RemoveContentChanged(object? sender, FileItemTree.FileTreeContentChangedEventArgs e)
        {
            if (_disposedValue) return;

            RemoveSearch(e.FileItem);
        }

        private void Tree_ContentChanged(object? sender, FileItemTree.FileTreeContentChangedEventArgs e)
        {
            if (_disposedValue) return;

            AddSearch(e.FileItem);
        }

        private void AddSearch(FileItem fileItem)
        {
            _jobEngine.InvokeAsync(() =>
            {
                if (_result.Items.Contains(fileItem)) return;

                var entries = new List<FileItem>() { fileItem };
                var items = _engine.Search(_result.Keyword, entries, CancellationToken.None);

                foreach (var item in items)
                {
                    Trace($"Add: {item.Path}");
                    _result.Items.Add(item);
                    CollectionChanged?.Invoke(this, new CollectionChangedEventArgs<FileItem>(CollectionChangedAction.Add, item));
                }
            });
        }

        private void RemoveSearch(FileItem fileItem)
        {
            _jobEngine.InvokeAsync(() =>
            {
                if (!_result.Items.Contains(fileItem)) return;

                Trace($"Remove: {fileItem.Path}");
                var isRemoved = _result.Items.Remove(fileItem);
                if (isRemoved)
                {
                    CollectionChanged?.Invoke(this, new CollectionChangedEventArgs<FileItem>(CollectionChangedAction.Remove, fileItem));
                }
            });
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
                    _engine.Tree.ContentChanged -= Tree_ContentChanged;
                    _jobEngine.Dispose();
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
